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

    /// <summary>When the exam is saved with the given status for the first time, runs <see cref="Other"/> and gives it two seconds.</summary>
    private sealed class RaceHook(ExamStatus saving) : SaveChangesInterceptor
    {
        private int fired;

        public Func<Task>? Other;

        public Task? Running;

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            var exam = eventData.Context!.ChangeTracker.Entries<Exam>().FirstOrDefault(e => e.State == EntityState.Modified && e.Entity.Status == saving);
            if (exam is not null && Other is not null && Interlocked.Exchange(ref fired, 1) == 0)
            {
                Running = Task.Run(Other, CancellationToken.None);
                await Task.Delay(2000, cancellationToken);
            }

            return result;
        }
    }

    private async Task<(ApiFixture.SeededCandidate Exam, HttpClient Admin, HttpClient Candidate, RaceHook Hook)> ActiveExamAsync(ExamStatus saving)
    {
        var exam = await api.CreateCandidateAsync(Password, ExamStatus.Draft, withSamplePapers: true);
        var hook = new RaceHook(saving);
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
        var (exam, admin, candidate, hook) = await ActiveExamAsync(Enum.Parse<ExamStatus>(target));
        using var _ = admin;
        using var __ = candidate;
        HttpResponseMessage? start = null;
        hook.Other = async () => start = await candidate.PostAsync($"/api/candidate/sections/{exam.ReadingWritingSectionId}/attempt", null, ct);

        var change = await admin.PostAsJsonAsync($"/api/admin/exams/{exam.ExamId}/status", new { status = target }, ct);
        await hook.Running!;

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
        var (exam, admin, candidate, hook) = await ActiveExamAsync(ExamStatus.Closed);
        using var _ = admin;
        using var __ = candidate;
        HttpResponseMessage? second = null;
        hook.Other = async () => second = await admin.PostAsJsonAsync($"/api/admin/exams/{exam.ExamId}/status", new { status = "Scheduled" }, ct);

        var first = await admin.PostAsJsonAsync($"/api/admin/exams/{exam.ExamId}/status", new { status = "Closed" }, ct);
        await hook.Running!;

        // Active -> Closed first; Closed -> Scheduled is not a move, so the second must be refused, not overwrite it.
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second!.StatusCode);
        using var scope = api.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExamPrepDbContext>();
        Assert.Equal(ExamStatus.Closed, (await db.Exams.AsNoTracking().SingleAsync(e => e.Id == exam.ExamId, ct)).Status);
    }
}
