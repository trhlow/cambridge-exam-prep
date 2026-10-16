using System.Net;
using System.Net.Http.Json;
using ExamPrep.Domain.Enums;

namespace ExamPrep.Tests.Api.Admin;

/// <summary>VE-23: administrators create invigilator accounts, reset their passwords and switch them off.</summary>
public sealed class AdminStaffTests(ApiFixture api) : IClassFixture<ApiFixture>
{
    private const string Password = "kq7m-2xpt-9";
    private readonly CancellationToken ct = TestContext.Current.CancellationToken;

    public sealed record StaffProfile(int Id, string Username, string FullName, string? Email, string Role, bool IsActive);

    private sealed record LoginRequest(string Username, string Password);

    private static string NewUsername() => $"inv{Guid.NewGuid():N}"[..12];

    private Task<HttpResponseMessage> LoginAsync(string username, string password) =>
        api.CreateClient().PostAsJsonAsync("/api/staff/auth/login", new LoginRequest(username, password), ct);

    [Fact]
    public async Task Create_MakesAnInvigilatorWhoCanSignIn()
    {
        api.SkipIfUnavailable();
        using var admin = await api.AdminClientAsync();
        var username = NewUsername();

        var create = await admin.PostAsJsonAsync("/api/admin/users", new { username, fullName = "Ms Lan", password = Password, role = "Invigilator" }, ct);
        var duplicate = await admin.PostAsJsonAsync("/api/admin/users", new { username, fullName = "Other", password = Password, role = "Invigilator" }, ct);
        var weak = await admin.PostAsJsonAsync("/api/admin/users", new { username = NewUsername(), fullName = "Weak", password = "short", role = "Invigilator" }, ct);

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var profile = await create.Content.ReadFromJsonAsync<StaffProfile>(ct);
        Assert.Equal("Invigilator", profile!.Role);
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(username, Password)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, weak.StatusCode);
    }

    [Fact]
    public async Task Update_ResetsThePasswordAndDeactivatesTheAccount_OnceAnotherAdminApproves()
    {
        api.SkipIfUnavailable();
        using var admin = await api.AdminClientAsync();
        using var otherAdmin = await api.SecondAdminClientAsync();
        var invigilator = await api.CreateStaffAsync(UserRole.Invigilator, Password);
        using var signedIn = await api.StaffClientAsync(invigilator.Username, Password);

        var reset = await admin.PutAsJsonAsync($"/api/admin/users/{invigilator.Id}", new { fullName = "Renamed", isActive = true, newPassword = "new-pass-2026" }, ct);
        var resetBody = (await reset.Content.ReadFromJsonAsync<System.Text.Json.Nodes.JsonObject>(ct))!;
        await ApiFixture.ApproveAsync(otherAdmin, (int)resetBody["request"]!["id"]!);
        var oldSessionAfterReset = await signedIn.GetAsync("/api/staff/auth/me", ct);
        var newLogin = await LoginAsync(invigilator.Username, "new-pass-2026");
        var deactivate = await admin.PutAsJsonAsync($"/api/admin/users/{invigilator.Id}", new { fullName = "Renamed", isActive = false }, ct);
        await ApiFixture.ApproveAsync(otherAdmin, await ApiFixture.RequestIdOfAsync(deactivate));
        var loginWhenInactive = await LoginAsync(invigilator.Username, "new-pass-2026");

        Assert.Equal("Renamed", (string)resetBody["user"]!["fullName"]!);
        Assert.Equal(HttpStatusCode.Unauthorized, oldSessionAfterReset.StatusCode);
        Assert.Equal(HttpStatusCode.OK, newLogin.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, loginWhenInactive.StatusCode);
    }

    [Fact]
    public async Task Update_DoesNotLetAnAdministratorSwitchThemselvesOff()
    {
        api.SkipIfUnavailable();
        var other = await api.CreateStaffAsync(UserRole.Admin, Password);
        using var admin = await api.StaffClientAsync(other.Username, Password);

        var response = await admin.PutAsJsonAsync($"/api/admin/users/{other.Id}", new { fullName = "Me", isActive = false }, ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
