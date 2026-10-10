using System.Net;
using System.Net.Http.Json;
using ExamPrep.Domain.Enums;
using ExamPrep.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ExamPrep.Tests.Api.Auth;

/// <summary>VE-3: staff (admin, invigilator) sign-in with cookies and role-based access.</summary>
public sealed class StaffAuthTests(ApiFixture api) : IClassFixture<ApiFixture>
{
    private const string Password = "Correct#Horse42";
    private readonly CancellationToken ct = TestContext.Current.CancellationToken;

    private sealed record LoginRequest(string Username, string Password);

    private sealed record StaffProfile(int Id, string Username, string FullName, string Role);

    private async Task<HttpResponseMessage> LoginAsync(HttpClient client, string username, string password) =>
        await client.PostAsJsonAsync("/api/staff/auth/login", new LoginRequest(username, password), ct);

    [Fact]
    public async Task Login_ReturnsProfile_WhenCredentialsAreValid()
    {
        api.SkipIfUnavailable();
        using var client = api.CreateClient();

        var response = await LoginAsync(client, ApiFixture.AdminUsername, ApiFixture.AdminPassword);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = await response.Content.ReadFromJsonAsync<StaffProfile>(ct);
        Assert.Equal(ApiFixture.AdminUsername, profile!.Username);
        Assert.Equal("Admin", profile.Role);
    }

    [Fact]
    public async Task Login_Returns401_WhenPasswordIsWrong()
    {
        api.SkipIfUnavailable();
        using var client = api.CreateClient();

        var response = await LoginAsync(client, ApiFixture.AdminUsername, "wrong-password");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_Returns401_WhenAccountIsDeactivated()
    {
        api.SkipIfUnavailable();
        var user = await api.CreateStaffAsync(UserRole.Invigilator, Password, isActive: false);
        using var client = api.CreateClient();

        var response = await LoginAsync(client, user.Username, Password);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("", "x")]
    [InlineData("admin", "")]
    public async Task Login_Returns400_WhenFieldsAreMissing(string username, string password)
    {
        api.SkipIfUnavailable();
        using var client = api.CreateClient();

        var response = await LoginAsync(client, username, password);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Me_Returns401_WhenNotSignedIn()
    {
        api.SkipIfUnavailable();
        using var client = api.CreateClient();

        var response = await client.GetAsync("/api/staff/auth/me", ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_ReturnsProfile_AfterLogin()
    {
        api.SkipIfUnavailable();
        var user = await api.CreateStaffAsync(UserRole.Invigilator, Password);
        using var client = api.CreateClient();
        await LoginAsync(client, user.Username, Password);

        var profile = await client.GetFromJsonAsync<StaffProfile>("/api/staff/auth/me", ct);

        Assert.Equal(user.Id, profile!.Id);
        Assert.Equal("Invigilator", profile.Role);
    }

    [Fact]
    public async Task Me_Returns401_AfterLogout()
    {
        api.SkipIfUnavailable();
        using var client = api.CreateClient();
        await LoginAsync(client, ApiFixture.AdminUsername, ApiFixture.AdminPassword);

        var logout = await client.PostAsync("/api/staff/auth/logout", null, ct);
        var me = await client.GetAsync("/api/staff/auth/me", ct);

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
    }

    [Fact]
    public async Task Me_Returns401_WhenSecurityStampChangedAfterLogin()
    {
        api.SkipIfUnavailable();
        var user = await api.CreateStaffAsync(UserRole.Invigilator, Password);
        using var client = api.CreateClient();
        await LoginAsync(client, user.Username, Password);

        await api.RotateSecurityStampAsync(user.Id);
        var response = await client.GetAsync("/api/staff/auth/me", ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AdminUsers_Returns401_WhenNotSignedIn()
    {
        api.SkipIfUnavailable();
        using var client = api.CreateClient();

        var response = await client.GetAsync("/api/admin/users", ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AdminUsers_Returns403_ForInvigilator()
    {
        api.SkipIfUnavailable();
        var user = await api.CreateStaffAsync(UserRole.Invigilator, Password);
        using var client = api.CreateClient();
        await LoginAsync(client, user.Username, Password);

        var response = await client.GetAsync("/api/admin/users", ct);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminUsers_ListsStaffWithoutPasswordHashes_ForAdmin()
    {
        api.SkipIfUnavailable();
        using var client = api.CreateClient();
        await LoginAsync(client, ApiFixture.AdminUsername, ApiFixture.AdminPassword);

        var response = await client.GetAsync("/api/admin/users", ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(ApiFixture.AdminUsername, body);
        Assert.DoesNotContain("password", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Startup_CreatesBootstrapAdminOnlyOnce()
    {
        api.SkipIfUnavailable();
        await using (var secondHost = api.CreateFactory())
        {
            _ = secondHost.Server;
        }

        using var scope = api.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExamPrepDbContext>();
        var admins = await db.Users.CountAsync(u => u.Username == ApiFixture.AdminUsername, ct);

        Assert.Equal(1, admins);
    }
}
