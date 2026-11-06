using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using ExamPrep.Api.Admin;
using ExamPrep.Domain.Enums;
using ExamPrep.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ExamPrep.Tests.Api.Admin;

/// <summary>
/// Review of 2 November (B1, B2, A12, B16): a session moves Draft ↔ Scheduled → Active → Closed in order, goes Active
/// only when it has candidates and an invigilator, cannot close while someone is writing, and freezes its seats once
/// the first attempt exists.
/// </summary>
public sealed class ExamLifecycleTests(ApiFixture api) : IClassFixture<ApiFixture>
{
    private const string Password = "life-4x9q";
    private readonly CancellationToken ct = TestContext.Current.CancellationToken;

    private async Task<HttpResponseMessage> SetStatusAsync(HttpClient client, int examId, string status) =>
        await client.PostAsJsonAsync($"/api/admin/exams/{examId}/status", new { status }, ct);

    private async Task AssignInvigilatorAsync(HttpClient admin, int examId)
    {
        var invigilator = await api.CreateStaffAsync(UserRole.Invigilator, Password);
        (await admin.PutAsJsonAsync($"/api/admin/exams/{examId}/invigilators", new { userIds = new[] { invigilator.Id } }, ct)).EnsureSuccessStatusCode();
    }

    /// <summary>A Scheduled session with a candidate and an invigilator: ready to go Active.</summary>
    private async Task<(ApiFixture.SeededCandidate Exam, HttpClient Admin)> ReadyExamAsync()
    {
        var exam = await api.CreateCandidateAsync(Password, ExamStatus.Draft, withSamplePapers: true);
        var admin = await api.AdminClientAsync();
        await AssignInvigilatorAsync(admin, exam.ExamId);
        (await SetStatusAsync(admin, exam.ExamId, "Scheduled")).EnsureSuccessStatusCode();
        return (exam, admin);
    }

    private async Task<string> StatusOfAsync(int examId)
    {
        using var scope = api.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExamPrepDbContext>();
        return (await db.Exams.AsNoTracking().SingleAsync(e => e.Id == examId, ct)).Status.ToString();
    }

    [Fact]
    public async Task Draft_CannotJumpToActive_ButCanBeScheduled_AndBack()
    {
        api.SkipIfUnavailable();
        var exam = await api.CreateCandidateAsync(Password, ExamStatus.Draft);
        using var admin = await api.AdminClientAsync();

        var jump = await SetStatusAsync(admin, exam.ExamId, "Active");
        var scheduled = await SetStatusAsync(admin, exam.ExamId, "Scheduled");
        var back = await SetStatusAsync(admin, exam.ExamId, "Draft");
        var same = await SetStatusAsync(admin, exam.ExamId, "Draft");

        Assert.Equal(HttpStatusCode.Conflict, jump.StatusCode);
        Assert.Contains("Draft", await jump.Content.ReadAsStringAsync(ct));
        Assert.Equal(HttpStatusCode.OK, scheduled.StatusCode);
        Assert.Equal(HttpStatusCode.OK, back.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, same.StatusCode);
    }

    [Fact]
    public async Task GoingActive_NeedsACandidateAndAnInvigilator_AndSaysWhichIsMissing()
    {
        api.SkipIfUnavailable();
        var papers = await api.CreatePublishedPapersAsync();
        using var admin = await api.AdminClientAsync();
        var create = await admin.PostAsJsonAsync("/api/admin/exams", new
        {
            name = $"Empty {Guid.NewGuid():N}"[..20],
            examTypeId = 1,
            scheduledDate = "2026-11-20",
            papers = new[] { new { sectionTemplateId = 1, testPaperId = papers.ReadingWritingId }, new { sectionTemplateId = 2, testPaperId = papers.ListeningId } },
        }, ct);
        create.EnsureSuccessStatusCode();
        var examId = (int)(await create.Content.ReadFromJsonAsync<JsonObject>(ct))!["id"]!;
        (await SetStatusAsync(admin, examId, "Scheduled")).EnsureSuccessStatusCode();

        var neither = await SetStatusAsync(admin, examId, "Active");
        await AssignInvigilatorAsync(admin, examId);
        var noCandidate = await SetStatusAsync(admin, examId, "Active");
        await api.AddCandidateAsync(examId, Password);
        var ready = await SetStatusAsync(admin, examId, "Active");

        Assert.Equal(HttpStatusCode.BadRequest, neither.StatusCode);
        var both = (await neither.Content.ReadFromJsonAsync<JsonObject>(ct))!["errors"]!.AsArray().Select(e => (string)e!).ToList();
        Assert.Equal(2, both.Count);
        Assert.Contains(both, e => e.Contains("candidate"));
        Assert.Contains(both, e => e.Contains("invigilator"));
        Assert.Equal(HttpStatusCode.BadRequest, noCandidate.StatusCode);
        Assert.Single((await noCandidate.Content.ReadFromJsonAsync<JsonObject>(ct))!["errors"]!.AsArray());
        Assert.Equal(HttpStatusCode.OK, ready.StatusCode);
    }

    [Fact]
    public async Task ActiveGoesBackToScheduled_OnlyWhileNoAttemptExists()
    {
        api.SkipIfUnavailable();
        var (exam, admin) = await ReadyExamAsync();
        using var _ = admin;
        (await SetStatusAsync(admin, exam.ExamId, "Active")).EnsureSuccessStatusCode();

        var back = await SetStatusAsync(admin, exam.ExamId, "Scheduled");
        (await SetStatusAsync(admin, exam.ExamId, "Active")).EnsureSuccessStatusCode();
        await api.SubmitSectionAsync(exam.CandidateId, exam.ReadingWritingSectionId);
        var blocked = await SetStatusAsync(admin, exam.ExamId, "Scheduled");

        Assert.Equal(HttpStatusCode.OK, back.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);
        Assert.Equal("Active", await StatusOfAsync(exam.ExamId));
    }

    [Fact]
    public async Task Close_IsRefusedWhileAnAttemptIsRunningOrPaused_AndSaysHowMany()
    {
        api.SkipIfUnavailable();
        var (exam, admin) = await ReadyExamAsync();
        using var _ = admin;
        (await SetStatusAsync(admin, exam.ExamId, "Active")).EnsureSuccessStatusCode();
        var other = await api.AddCandidateAsync(exam.ExamId, Password);
        await api.StartAttemptAsync(exam.CandidateId, exam.ReadingWritingSectionId);
        await api.StartAttemptAsync(other.CandidateId, exam.ReadingWritingSectionId);
        await api.PauseAttemptAsync(other.CandidateId, exam.ReadingWritingSectionId);

        var refused = await SetStatusAsync(admin, exam.ExamId, "Closed");
        await api.SubmitStartedAttemptAsync(exam.CandidateId, exam.ReadingWritingSectionId);
        var stillPaused = await SetStatusAsync(admin, exam.ExamId, "Closed");
        await api.SubmitStartedAttemptAsync(other.CandidateId, exam.ReadingWritingSectionId);
        var closed = await SetStatusAsync(admin, exam.ExamId, "Closed");

        Assert.Equal(HttpStatusCode.Conflict, refused.StatusCode);
        Assert.Contains("2 candidates", await refused.Content.ReadAsStringAsync(ct));
        Assert.Equal(HttpStatusCode.Conflict, stillPaused.StatusCode);
        Assert.Contains("1 candidate", await stillPaused.Content.ReadAsStringAsync(ct));
        Assert.Equal(HttpStatusCode.OK, closed.StatusCode);
    }

    [Fact]
    public async Task ClosedReopens_ForAdministratorsOnly()
    {
        api.SkipIfUnavailable();
        var (exam, admin) = await ReadyExamAsync();
        using var _ = admin;
        (await SetStatusAsync(admin, exam.ExamId, "Active")).EnsureSuccessStatusCode();
        (await SetStatusAsync(admin, exam.ExamId, "Closed")).EnsureSuccessStatusCode();
        var manager = await api.CreateManagerAsync(Password, ManagerArea.Exams);
        await api.SetExamCreatorAsync(exam.ExamId, manager.Id);
        using var managerClient = await api.StaffClientAsync(manager.Username, Password);

        var byManager = await SetStatusAsync(managerClient, exam.ExamId, "Active");
        var toDraft = await SetStatusAsync(admin, exam.ExamId, "Draft");
        var byAdmin = await SetStatusAsync(admin, exam.ExamId, "Active");

        Assert.Equal(HttpStatusCode.Conflict, byManager.StatusCode);
        Assert.Contains("administrator", await byManager.Content.ReadAsStringAsync(ct));
        Assert.Equal(HttpStatusCode.Conflict, toDraft.StatusCode);
        Assert.Equal(HttpStatusCode.OK, byAdmin.StatusCode);
    }

    [Fact]
    public async Task Start_IsRefused_WhenTheExamIsNotActive()
    {
        api.SkipIfUnavailable();
        var (exam, admin) = await ReadyExamAsync();
        using var _ = admin;
        (await SetStatusAsync(admin, exam.ExamId, "Active")).EnsureSuccessStatusCode();
        using var candidate = api.CreateClient();
        (await candidate.PostAsJsonAsync("/api/candidate/auth/login", new { username = exam.Username, password = Password }, ct)).EnsureSuccessStatusCode();
        (await candidate.PostAsJsonAsync("/api/candidate/auth/pin", new { pin = exam.SessionPin }, ct)).EnsureSuccessStatusCode();
        (await SetStatusAsync(admin, exam.ExamId, "Closed")).EnsureSuccessStatusCode();

        var start = await candidate.PostAsync($"/api/candidate/sections/{exam.ReadingWritingSectionId}/attempt", null, ct);

        Assert.Equal(HttpStatusCode.Conflict, start.StatusCode);
        Assert.Empty(await AttemptsOfAsync(exam.ExamId));
    }

    private async Task<List<int>> AttemptsOfAsync(int examId)
    {
        using var scope = api.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExamPrepDbContext>();
        return await db.Attempts.Where(a => a.Candidate!.ExamId == examId).Select(a => a.Id).ToListAsync(ct);
    }

    [Fact]
    public async Task GoingActive_FillsTheSeatsOfUnseatedCandidates_FromTheirPlaceInTheList()
    {
        api.SkipIfUnavailable();
        var (exam, admin) = await ReadyExamAsync();
        using var _ = admin;
        var second = await api.AddCandidateAsync(exam.ExamId, Password);
        var third = await api.AddCandidateAsync(exam.ExamId, Password);
        await api.SetSeatAsync(third.CandidateId, "7");

        (await SetStatusAsync(admin, exam.ExamId, "Active")).EnsureSuccessStatusCode();

        using var scope = api.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExamPrepDbContext>();
        var seats = await db.Candidates.Where(c => c.ExamId == exam.ExamId).OrderBy(c => c.CandidateNumber).ThenBy(c => c.Id).Select(c => new { c.Id, c.SeatNo }).ToListAsync(ct);
        Assert.All(seats, s => Assert.NotNull(s.SeatNo));
        Assert.Equal("7", seats.Single(s => s.Id == third.CandidateId).SeatNo);
        // Each unseated candidate got the place the list gave them before: nobody's computer moved.
        var untyped = seats.Where(s => s.Id != third.CandidateId).ToList();
        Assert.Equal(untyped.Select(s => s.SeatNo).Distinct().Count(), untyped.Count);
    }

    [Fact]
    public async Task OnceAnAttemptExists_SeatsPerRowIsFrozen_AndANewCandidateNeedsAFreeNumericSeat()
    {
        api.SkipIfUnavailable();
        var (exam, admin) = await ReadyExamAsync();
        using var _ = admin;
        await api.SetSeatAsync(exam.CandidateId, "1");
        (await SetStatusAsync(admin, exam.ExamId, "Active")).EnsureSuccessStatusCode();
        await api.StartAttemptAsync(exam.CandidateId, exam.ReadingWritingSectionId);
        var detail = (await admin.GetFromJsonAsync<JsonObject>($"/api/staff/exams/{exam.ExamId}", ct))!;
        var papers = detail["sections"]!.AsArray().Select(s => new { sectionTemplateId = (int)s!["sectionTemplateId"]!, testPaperId = (int)s["testPaperId"]! }).ToList();

        object Save(int? seatsPerRow) => new { name = (string)detail["name"]!, examTypeId = 1, scheduledDate = "2026-11-20", papers, seatsPerRow };
        var change = await admin.PutAsJsonAsync($"/api/admin/exams/{exam.ExamId}", Save(6), ct);
        var rename = await admin.PutAsJsonAsync($"/api/admin/exams/{exam.ExamId}", Save(null), ct);

        object Candidates(string? seat) => new { candidates = new[] { new NewCandidate(Random.Shared.Next(100000, 999999).ToString(), "An", "Le", SeatNo: seat) } };
        var noSeat = await admin.PostAsJsonAsync($"/api/admin/exams/{exam.ExamId}/candidates", Candidates(null), ct);
        var taken = await admin.PostAsJsonAsync($"/api/admin/exams/{exam.ExamId}/candidates", Candidates("1"), ct);
        var label = await admin.PostAsJsonAsync($"/api/admin/exams/{exam.ExamId}/candidates", Candidates("A3"), ct);
        var free = await admin.PostAsJsonAsync($"/api/admin/exams/{exam.ExamId}/candidates", Candidates("9"), ct);

        Assert.Equal(HttpStatusCode.Conflict, change.StatusCode);
        Assert.Equal(HttpStatusCode.OK, rename.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, noSeat.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, taken.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, label.StatusCode);
        Assert.Equal(HttpStatusCode.Created, free.StatusCode);
    }

    [Fact]
    public async Task ASeatTakenInTheSession_IsRefused_WhenEnteringOrChangingCandidates()
    {
        api.SkipIfUnavailable();
        var exam = await api.CreateCandidateAsync(Password, ExamStatus.Draft);
        using var admin = await api.AdminClientAsync();
        await api.SetSeatAsync(exam.CandidateId, "3");
        var other = await api.AddCandidateAsync(exam.ExamId, Password);

        string Number() => Random.Shared.Next(100000, 999999).ToString();
        var sameAsExisting = await admin.PostAsJsonAsync($"/api/admin/exams/{exam.ExamId}/candidates", new { candidates = new[] { new NewCandidate(Number(), "An", "Le", SeatNo: " 3 ") } }, ct);
        var twiceInList = await admin.PostAsJsonAsync($"/api/admin/exams/{exam.ExamId}/candidates", new { candidates = new[] { new NewCandidate(Number(), "An", "Le", SeatNo: "5"), new NewCandidate(Number(), "Bo", "Le", SeatNo: "5") } }, ct);
        var move = await admin.PutAsJsonAsync($"/api/admin/candidates/{other.CandidateId}", new { firstName = "Binh", lastName = "Tran", plannedExtraPercent = 0, seatNo = "3" }, ct);
        var keepOwn = await admin.PutAsJsonAsync($"/api/admin/candidates/{exam.CandidateId}", new { firstName = "An", lastName = "Nguyen", plannedExtraPercent = 0, seatNo = "3" }, ct);
        var freeSeat = await admin.PutAsJsonAsync($"/api/admin/candidates/{other.CandidateId}", new { firstName = "Binh", lastName = "Tran", plannedExtraPercent = 0, seatNo = "4" }, ct);

        Assert.Equal(HttpStatusCode.BadRequest, sameAsExisting.StatusCode);
        Assert.Contains("seat 3", await sameAsExisting.Content.ReadAsStringAsync(ct));
        Assert.Equal(HttpStatusCode.BadRequest, twiceInList.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, move.StatusCode);
        Assert.Equal(HttpStatusCode.OK, keepOwn.StatusCode);
        Assert.Equal(HttpStatusCode.OK, freeSeat.StatusCode);
    }

    [Fact]
    public async Task OnceTheExamIsActive_AnEditKeepsAFreeNumericSeat()
    {
        api.SkipIfUnavailable();
        var (exam, admin) = await ReadyExamAsync();
        using var _ = admin;
        var other = await api.AddCandidateAsync(exam.ExamId, Password);
        await api.SetSeatAsync(exam.CandidateId, "1");
        await api.SetSeatAsync(other.CandidateId, "2");
        (await SetStatusAsync(admin, exam.ExamId, "Active")).EnsureSuccessStatusCode();

        Task<HttpResponseMessage> Edit(string? seat) =>
            admin.PutAsJsonAsync($"/api/admin/candidates/{other.CandidateId}", new { firstName = "Binh", lastName = "Tran", plannedExtraPercent = 0, seatNo = seat }, ct);
        var blank = await Edit(null);
        var label = await Edit("A3");
        var taken = await Edit("1");
        var keep = await Edit("2");
        var move = await Edit("8");

        Assert.Equal(HttpStatusCode.BadRequest, blank.StatusCode);
        Assert.Contains("computer number", await blank.Content.ReadAsStringAsync(ct));
        Assert.Equal(HttpStatusCode.BadRequest, label.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, taken.StatusCode);
        Assert.Equal(HttpStatusCode.OK, keep.StatusCode);
        Assert.Equal(HttpStatusCode.OK, move.StatusCode);
    }

    [Fact]
    public async Task GoingActive_IsRefused_ForATypedSeatThatIsNotANumber_AndNamesTheCandidate()
    {
        api.SkipIfUnavailable();
        var (exam, admin) = await ReadyExamAsync();
        using var _ = admin;
        await api.SetSeatAsync(exam.CandidateId, "A3");

        var refused = await SetStatusAsync(admin, exam.ExamId, "Active");

        Assert.Equal(HttpStatusCode.BadRequest, refused.StatusCode);
        var errors = (await refused.Content.ReadFromJsonAsync<JsonObject>(ct))!["errors"]!.AsArray().Select(e => (string)e!).ToList();
        var line = Assert.Single(errors);
        using (var scope = api.Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ExamPrepDbContext>();
            var candidate = await db.Candidates.AsNoTracking().SingleAsync(c => c.Id == exam.CandidateId, ct);
            Assert.StartsWith($"Candidate {candidate.FirstName} {candidate.LastName} ({candidate.CandidateNumber}):", line);
        }

        Assert.Contains("seat \"A3\" is not a number", line);
        Assert.Equal("Scheduled", await StatusOfAsync(exam.ExamId));

        await api.SetSeatAsync(exam.CandidateId, "3");
        Assert.Equal(HttpStatusCode.OK, (await SetStatusAsync(admin, exam.ExamId, "Active")).StatusCode);
    }

    [Theory]
    [InlineData("Active", "Draft")]
    [InlineData("Draft", "Closed")]
    [InlineData("Scheduled", "Closed")]
    [InlineData("Closed", "Scheduled")]
    [InlineData("Draft", "Draft")]
    [InlineData("Scheduled", "Scheduled")]
    [InlineData("Active", "Active")]
    [InlineData("Closed", "Closed")]
    public async Task EveryOtherTransition_AndASameStatusChange_IsRefusedWith409(string from, string to)
    {
        api.SkipIfUnavailable();
        var exam = await api.CreateCandidateAsync(Password, ExamStatus.Draft, withSamplePapers: true);
        using var admin = await api.AdminClientAsync();
        await AssignInvigilatorAsync(admin, exam.ExamId);
        foreach (var step in new[] { "Scheduled", "Active", "Closed" }.TakeWhile(_ => from != "Draft").Take(from == "Scheduled" ? 1 : from == "Active" ? 2 : 3))
        {
            (await SetStatusAsync(admin, exam.ExamId, step)).EnsureSuccessStatusCode();
        }

        var refused = await SetStatusAsync(admin, exam.ExamId, to);

        Assert.Equal(HttpStatusCode.Conflict, refused.StatusCode);
        Assert.Equal(from, await StatusOfAsync(exam.ExamId));
    }
}
