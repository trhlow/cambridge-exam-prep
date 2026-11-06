using System.Net;
using System.Net.Http.Json;
using ExamPrep.Domain.Entities;
using ExamPrep.Domain.Enums;
using ExamPrep.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace ExamPrep.Tests.Api.Admin;

/// <summary>
/// Review of Task 5: a status change and a candidate's Start (or a second status change) that meet at the same moment
/// must wait for each other. The hook below runs the other request while the first one is between its checks and its
/// save, which is exactly where the old code let both through.
/// </summary>
public sealed class ExamStatusRaceTests(ApiFixture api) : IClassFixture<ApiFixture>
{
    private const string Password = "race-7m2k";
    private readonly CancellationToken ct = TestContext.Current.CancellationToken;

    /// <summary>
    /// When the matching save happens for the first time, runs <see cref="Other"/>, gives it two seconds and records
    /// whether it was still waiting (on a lock or a key) when they were up: if it had already finished, the two requests
    /// never overlapped and the test would pass without testing anything.
    /// </summary>
    private sealed class RaceHook(Func<DbContext, bool> matches) : SaveChangesInterceptor
    {
        private int fired;

        public Func<Task>? Other;

        public Task? Running;

        /// <summary>True when the other request had not finished after the two seconds, i.e. it really was held up.</summary>
        public bool OtherWasHeldUp;

        public static RaceHook ExamSavedAs(ExamStatus saving) => new(context =>
            context.ChangeTracker.Entries<Exam>().Any(e => e.State == EntityState.Modified && e.Entity.Status == saving));

        public static RaceHook CandidateEdited() => new(context => context.ChangeTracker.Entries<Candidate>().Any(e => e.State == EntityState.Modified));

        public static RaceHook AttemptAdded() => new(context => context.ChangeTracker.Entries<Attempt>().Any(e => e.State == EntityState.Added));

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (Other is not null && matches(eventData.Context!) && Interlocked.Exchange(ref fired, 1) == 0)
            {
                Running = Task.Run(Other, CancellationToken.None);
                await Task.Delay(2000, cancellationToken);
                OtherWasHeldUp = !Running.IsCompleted;
            }

            return result;
        }
    }

    private async Task<(ApiFixture.SeededCandidate Exam, HttpClient Admin, HttpClient Candidate, RaceHook Hook)> ActiveExamAsync(RaceHook hook)
    {
        var exam = await api.CreateCandidateAsync(Password, ExamStatus.Draft, withSamplePapers: true);
        var factory = api.Factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.ConfigureDbContext<ExamPrepDbContext>(options => options.AddInterceptors(hook))));
        var admin = factory.CreateClient();
        (await admin.PostAsJsonAsync("/api/staff/auth/login", new { username = ApiFixture.AdminUsername, password = ApiFixture.AdminPassword }, ct)).EnsureSuccessStatusCode();
        var invigilator = await api.CreateStaffAsync(UserRole.Invigilator, Password);
        (await admin.PutAsJsonAsync($"/api/admin/exams/{exam.ExamId}/invigilators", new { userIds = new[] { invigilator.Id } }, ct)).EnsureSuccessStatusCode();
        (await admin.PostAsJsonAsync($"/api/admin/exams/{exam.ExamId}/status", new { status = "Scheduled" }, ct)).EnsureSuccessStatusCode();
        (await admin.PostAsJsonAsync($"/api/admin/exams/{exam.ExamId}/status", new { status = "Active" }, ct)).EnsureSuccessStatusCode();
        var candidate = factory.CreateClient();
        (await candidate.PostAsJsonAsync("/api/candidate/auth/login", new { username = exam.Username, password = Password }, ct)).EnsureSuccessStatusCode();
        (await candidate.PostAsJsonAsync("/api/candidate/auth/pin", new { pin = exam.SessionPin }, ct)).EnsureSuccessStatusCode();
        return (exam, admin, candidate, hook);
    }

    [Theory]
    [InlineData("Closed")]
    [InlineData("Scheduled")]
    public async Task AStartThatMeetsTheStatusChange_NeverLeavesAnAttemptInANotActiveExam(string target)
    {
        api.SkipIfUnavailable();
        var (exam, admin, candidate, hook) = await ActiveExamAsync(RaceHook.ExamSavedAs(Enum.Parse<ExamStatus>(target)));
        using var _ = admin;
        using var __ = candidate;
        HttpResponseMessage? start = null;
        hook.Other = async () => start = await candidate.PostAsync($"/api/candidate/sections/{exam.ReadingWritingSectionId}/attempt", null, ct);

        var change = await admin.PostAsJsonAsync($"/api/admin/exams/{exam.ExamId}/status", new { status = target }, ct);
        await hook.Running!;
        Assert.True(hook.OtherWasHeldUp, "The other request finished within the window, so the two never overlapped.");

        using var scope = api.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExamPrepDbContext>();
        var status = (await db.Exams.AsNoTracking().SingleAsync(e => e.Id == exam.ExamId, ct)).Status;
        var attempts = await db.Attempts.CountAsync(a => a.Candidate!.ExamId == exam.ExamId, ct);
        Assert.True(status == ExamStatus.Active || attempts == 0, $"exam is {status} with {attempts} attempt(s); change {change.StatusCode}, start {start!.StatusCode}");
        // Whoever came second saw the other's result: exactly one of the two went through.
        Assert.True(change.IsSuccessStatusCode != start!.IsSuccessStatusCode);
    }

    [Fact]
    public async Task TwoStatusChangesAtOnce_AreDecidedOneAfterTheOther()
    {
        api.SkipIfUnavailable();
        var (exam, admin, candidate, hook) = await ActiveExamAsync(RaceHook.ExamSavedAs(ExamStatus.Closed));
        using var _ = admin;
        using var __ = candidate;
        HttpResponseMessage? second = null;
        hook.Other = async () => second = await admin.PostAsJsonAsync($"/api/admin/exams/{exam.ExamId}/status", new { status = "Scheduled" }, ct);

        var first = await admin.PostAsJsonAsync($"/api/admin/exams/{exam.ExamId}/status", new { status = "Closed" }, ct);
        await hook.Running!;
        Assert.True(hook.OtherWasHeldUp, "The other request finished within the window, so the two never overlapped.");

        // Active -> Closed first; Closed -> Scheduled is not a move, so the second must be refused, not overwrite it.
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second!.StatusCode);
        using var scope = api.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExamPrepDbContext>();
        Assert.Equal(ExamStatus.Closed, (await db.Exams.AsNoTracking().SingleAsync(e => e.Id == exam.ExamId, ct)).Status);
    }

    /// <summary>
    /// A double click or a second tab: two Starts of the same section at once. The first has read "no attempt" and is held
    /// just before its insert; the second runs through and commits the attempt. The first then hits the unique key and
    /// must return the second's attempt, not fail (its transaction must not keep a snapshot that hides the winner's row).
    /// </summary>
    [Fact]
    public async Task TwoStartsAtOnce_BothReturnTheSameAttempt()
    {
        api.SkipIfUnavailable();
        var (exam, admin, candidate, hook) = await ActiveExamAsync(RaceHook.AttemptAdded());
        using var _ = admin;
        using var __ = candidate;
        var url = $"/api/candidate/sections/{exam.ReadingWritingSectionId}/attempt";
        HttpResponseMessage? second = null;
        Exception? secondFailure = null;
        hook.Other = async () =>
        {
            try
            {
                second = await candidate.PostAsync(url, null, ct);
            }
            catch (Exception e)
            {
                secondFailure = e;
            }
        };

        var first = await candidate.PostAsync(url, null, ct);
        await hook.Running!;

        Assert.False(hook.OtherWasHeldUp, "The second Start was still running after the window, so it did not commit before the first one inserted.");
        Assert.Null(secondFailure);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second!.StatusCode);
        var firstState = await first.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>(ct);
        var secondState = await second.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>(ct);
        Assert.Equal(firstState.GetProperty("endsAt").GetDateTime(), secondState.GetProperty("endsAt").GetDateTime());
        using var scope = api.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExamPrepDbContext>();
        Assert.Equal(1, await db.Attempts.CountAsync(a => a.Candidate!.ExamId == exam.ExamId, ct));
    }

    /// <summary>
    /// A seat edit that is held between its frozen-seat check and its save while the exam goes Active: the exam must not
    /// end up open with a typed label for a seat. Either the edit lands first and going Active refuses the label, or the
    /// exam opens first and the edit is refused.
    /// </summary>
    [Fact]
    public async Task AnEditThatMeetsGoingActive_NeverLeavesATypedSeatInAnOpenExam()
    {
        api.SkipIfUnavailable();
        var exam = await api.CreateCandidateAsync(Password, ExamStatus.Draft, withSamplePapers: true);
        var hook = RaceHook.CandidateEdited();
        var factory = api.Factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.ConfigureDbContext<ExamPrepDbContext>(options => options.AddInterceptors(hook))));
        using var admin = factory.CreateClient();
        (await admin.PostAsJsonAsync("/api/staff/auth/login", new { username = ApiFixture.AdminUsername, password = ApiFixture.AdminPassword }, ct)).EnsureSuccessStatusCode();
        var invigilator = await api.CreateStaffAsync(UserRole.Invigilator, Password);
        (await admin.PutAsJsonAsync($"/api/admin/exams/{exam.ExamId}/invigilators", new { userIds = new[] { invigilator.Id } }, ct)).EnsureSuccessStatusCode();
        (await admin.PostAsJsonAsync($"/api/admin/exams/{exam.ExamId}/status", new { status = "Scheduled" }, ct)).EnsureSuccessStatusCode();
        HttpResponseMessage? activate = null;
        hook.Other = async () => activate = await admin.PostAsJsonAsync($"/api/admin/exams/{exam.ExamId}/status", new { status = "Active" }, ct);

        var edit = await admin.PutAsJsonAsync($"/api/admin/candidates/{exam.CandidateId}", new { firstName = "Race", lastName = "Seat", plannedExtraPercent = 0, seatNo = "A3" }, ct);
        await hook.Running!;

        Assert.True(hook.OtherWasHeldUp, "Going Active finished within the window, so the two never overlapped.");
        using var scope = api.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExamPrepDbContext>();
        var status = (await db.Exams.AsNoTracking().SingleAsync(e => e.Id == exam.ExamId, ct)).Status;
        var seat = (await db.Candidates.AsNoTracking().SingleAsync(c => c.Id == exam.CandidateId, ct)).SeatNo;
        Assert.True(status != ExamStatus.Active || ExamPrep.Domain.Papers.SeatPattern.Parse(seat) is not null,
            $"exam is Active with seat \"{seat}\"; edit {edit.StatusCode}, activate {activate!.StatusCode}");
    }
}
