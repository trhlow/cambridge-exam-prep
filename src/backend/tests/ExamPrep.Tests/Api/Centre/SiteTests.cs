using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using ExamPrep.Domain.Enums;

namespace ExamPrep.Tests.Api.Centre;

/// <summary>
/// VE-24: the centre's name and logo. Everyone reads them (the sign-in screens come first); administrators and
/// managers of the Centre area change them; only PNG logos are accepted.
/// </summary>
public sealed class SiteTests(ApiFixture api) : IClassFixture<ApiFixture>
{
    private const string Password = "Centre#2026-pass";
    private readonly CancellationToken ct = TestContext.Current.CancellationToken;

    private sealed record SiteView(string CentreName, string? LogoUrl);

    /// <summary>A tiny but complete 1×1 PNG.</summary>
    private static readonly byte[] Png = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

    private static MultipartFormDataContent Logo(byte[] content, string name = "logo.png", string type = "image/png")
    {
        var file = new ByteArrayContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue(type);
        return new MultipartFormDataContent { { file, "logo", name } };
    }

    [Fact]
    public async Task Everyone_ReadsTheCentre_AndAManagerOfTheCentreChangesIt()
    {
        api.SkipIfUnavailable();
        var manager = await api.CreateManagerAsync(Password, ManagerArea.Centre);
        using var staff = await api.StaffClientAsync(manager.Username, Password);
        using var anonymous = api.CreateClient();
        var before = await anonymous.GetFromJsonAsync<SiteView>("/api/site", ct);
        var noLogo = await anonymous.GetAsync("/api/site/logo", ct);

        (await staff.PutAsJsonAsync("/api/admin/site", new { centreName = "  Victory English Centre  " }, ct)).EnsureSuccessStatusCode();
        (await staff.PostAsync("/api/admin/site/logo", Logo(Png), ct)).EnsureSuccessStatusCode();
        var after = await anonymous.GetFromJsonAsync<SiteView>("/api/site", ct);
        var logo = await anonymous.GetAsync(after!.LogoUrl, ct);

        Assert.Null(before!.LogoUrl);
        Assert.Equal(HttpStatusCode.NotFound, noLogo.StatusCode);
        Assert.Equal("Victory English Centre", after.CentreName);
        Assert.Equal("image/png", logo.Content.Headers.ContentType?.MediaType);
        Assert.Equal(Png, await logo.Content.ReadAsByteArrayAsync(ct));
    }

    [Fact]
    public async Task OnlyPngLogosUpToOneMegabyte_AreAccepted()
    {
        api.SkipIfUnavailable();
        using var admin = await api.AdminClientAsync();
        var svg = Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\"><script>alert(1)</script></svg>");
        var huge = new byte[2 * 1024 * 1024];
        Png.CopyTo(huge, 0);

        var asSvg = await admin.PostAsync("/api/admin/site/logo", Logo(svg, "logo.svg", "image/svg+xml"), ct);
        var renamedSvg = await admin.PostAsync("/api/admin/site/logo", Logo(svg), ct);
        var tooBig = await admin.PostAsync("/api/admin/site/logo", Logo(huge), ct);
        var blankName = await admin.PutAsJsonAsync("/api/admin/site", new { centreName = "" }, ct);

        Assert.Equal(HttpStatusCode.BadRequest, asSvg.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, renamedSvg.StatusCode);
        Assert.True(tooBig.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.RequestEntityTooLarge, tooBig.StatusCode.ToString());
        Assert.Equal(HttpStatusCode.BadRequest, blankName.StatusCode);
    }

    [Fact]
    public async Task OtherStaff_CannotChangeTheCentre()
    {
        api.SkipIfUnavailable();
        var papersManager = await api.CreateManagerAsync(Password, ManagerArea.Papers);
        var invigilator = await api.CreateStaffAsync(UserRole.Invigilator, Password);
        using var papers = await api.StaffClientAsync(papersManager.Username, Password);
        using var room = await api.StaffClientAsync(invigilator.Username, Password);
        using var anonymous = api.CreateClient();

        Assert.Equal(HttpStatusCode.Forbidden, (await papers.PutAsJsonAsync("/api/admin/site", new { centreName = "X" }, ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await room.PostAsync("/api/admin/site/logo", Logo(Png), ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PutAsJsonAsync("/api/admin/site", new { centreName = "X" }, ct)).StatusCode);
    }
}
