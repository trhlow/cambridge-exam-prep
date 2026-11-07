using System.Net;
using System.Net.Http.Json;
using ExamPrep.Domain.Entities;
using ExamPrep.Domain.Enums;
using ExamPrep.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace ExamPrep.Tests.Api.Delivery;

/// <summary>
/// B8 of the 2 November review: when an invigilator, the sweep or another tab changes an attempt between the moment a
/// candidate's request reads it and the moment it saves, the request is read again and tried once more; if it loses
/// again the candidate gets a 409 with a sentence, never a 500.
/// </summary>
public sealed class ConcurrencyConflictTests(ApiFixture api) : IClassFixture<ApiFixture>
{
    private const string Password = "kq7m-2xpt";
    private readonly CancellationToken ct = TestContext.Current.CancellationToken;

    private sealed record LoginRequest(string Username, string Password);

    private sealed record PinRequest(string Pin);

    private sealed record AttemptState(string Status);

    private sealed record Problem(string? Title);

    /// <summary>Changes the attempt's concurrency stamp behind the request's back, a set number of times.</summary>
    private sealed class ConflictMaker : SaveChangesInterceptor
    {
        public int Remaining;

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            var context = eventData.Context!;
            var attempt = context.ChangeTracker.Entries<Attempt>().FirstOrDefault(e => e.State == EntityState.Modified);
            if (attempt is not null && Remaining > 0)
            {
                Remaining--;
                var id = attempt.Entity.Id;
                await context.Database.ExecuteSqlInterpolatedAsync($"UPDATE attempts SET concurrency_stamp = UUID() WHERE id = {id}", cancellationToken);
            }

            return result;
        }
    }

    /// <summary>A candidate whose Reading &amp; Writing time ran out a moment ago but who is still marked as working.</summary>
    private async Task<(HttpClient Client, ApiFixture.SeededCandidate Candidate, ConflictMaker Conflicts)> ExpiredCandidateAsync()
    {
        var candidate = await api.CreateCandidateAsync(Password, withSamplePapers: true);
        var conflicts = new ConflictMaker();
        var factory = api.Factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.ConfigureDbContext<ExamPrepDbContext>(options => options.AddInterceptors(conflicts))));
        var client = factory.CreateClient();
        (await client.PostAsJsonAsync("/api/candidate/auth/login", new LoginRequest(candidate.Username, Password), ct)).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync("/api/candidate/auth/pin", new PinRequest(candidate.SessionPin), ct)).EnsureSuccessStatusCode();
        await api.StartAttemptAsync(candidate.CandidateId, candidate.ReadingWritingSectionId);
        await api.MoveAttemptStartBackAsync(candidate.CandidateId, candidate.ReadingWritingSectionId, TimeSpan.FromMinutes(61));
        return (client, candidate, conflicts);
    }

    [Fact]
    public async Task ALostRace_IsReadAgainAndRetriedOnce()
    {
        api.SkipIfUnavailable();
        var (client, candidate, conflicts) = await ExpiredCandidateAsync();
        using var _ = client;
        conflicts.Remaining = 1;

        var response = await client.GetAsync($"/api/candidate/sections/{candidate.ReadingWritingSectionId}/attempt", ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Submitted", (await response.Content.ReadFromJsonAsync<AttemptState>(ct))!.Status);
    }

    [Theory]
    [InlineData("GET", "attempt")]
    [InlineData("POST", "heartbeat")]
    [InlineData("PUT", "responses")]
    public async Task ARaceLostTwice_IsA409WithASentenceNeverA500(string method, string path)
    {
        api.SkipIfUnavailable();
        var (client, candidate, conflicts) = await ExpiredCandidateAsync();
        using var _ = client;
        conflicts.Remaining = 5;
        var body = method == "PUT" ? JsonContent.Create(new { responses = new[] { new { questionNumber = 1, value = "A" } } }) : null;

        var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), $"/api/candidate/sections/{candidate.ReadingWritingSectionId}/{path}") { Content = body }, ct);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("try again", (await response.Content.ReadFromJsonAsync<Problem>(ct))!.Title);
    }

    /// <summary>
    /// Final review 8: a heartbeat saves one of the expired attempts between the sweep's read and its save. The sweep
    /// used to drop the whole batch until its next run; now it goes on one attempt at a time from fresh data.
    /// </summary>
    [Fact]
    public async Task TheSweep_WhenOneAttemptWasChangedMeanwhile_StillSubmitsTheOthers()
    {
        api.SkipIfUnavailable();
        var first = await api.CreateCandidateAsync(Password);
        var ids = new List<int> { first.CandidateId };
        for (var i = 0; i < 2; i++)
        {
            ids.Add((await api.AddCandidateAsync(first.ExamId, Password, "Mai", $"Zz{Guid.NewGuid():N}"[..10])).CandidateId);
        }

        using var scope = api.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExamPrepDbContext>();
        var bumped = false;
        db.SavingChanges += (_, _) =>
        {
            if (bumped || db.ChangeTracker.Entries<Attempt>().FirstOrDefault(e => e.State == EntityState.Modified) is not { } entry)
            {
                return;
            }

            bumped = true;
            db.Database.ExecuteSqlInterpolated($"UPDATE attempts SET concurrency_stamp = UUID() WHERE id = {entry.Entity.Id}");
        };
        var sweep = ActivatorUtilities.CreateInstance<ExamPrep.Api.Delivery.AttemptExpiry>(scope.ServiceProvider, db);
        foreach (var id in ids)
        {
            await api.StartAttemptAsync(id, first.ReadingWritingSectionId);
            await api.MoveAttemptStartBackAsync(id, first.ReadingWritingSectionId, TimeSpan.FromMinutes(61));
        }

        await sweep.SubmitExpiredAsync(ct);

        Assert.True(bumped);
        using var check = api.Factory.Services.CreateScope();
        var statuses = await check.ServiceProvider.GetRequiredService<ExamPrepDbContext>().Attempts
            .Where(a => ids.Contains(a.CandidateId) && a.ExamSectionId == first.ReadingWritingSectionId)
            .Select(a => a.Status).ToListAsync(ct);
        Assert.Equal(3, statuses.Count);
        Assert.All(statuses, s => Assert.Equal(AttemptStatus.Submitted, s));
    }
}
