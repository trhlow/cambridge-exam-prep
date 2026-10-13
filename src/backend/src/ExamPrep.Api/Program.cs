using ExamPrep.Api.Admin;
using ExamPrep.Api.Auth;
using ExamPrep.Api.Content;
using ExamPrep.Api.Delivery;
using ExamPrep.Api.Demo;
using ExamPrep.Infrastructure;
using ExamPrep.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration, Path.Combine(builder.Environment.ContentRootPath, "App_Data", "assets"));
builder.Services.AddStaffAuthentication(builder.Environment);
builder.Services.AddCandidateAuthentication(builder.Environment);
builder.Services.AddAssetAccess();
builder.Services.AddScoped<CandidateSectionQuery>();
builder.Services.AddScoped<CandidateCredentials>();
builder.Services.AddSingleton<LoginThrottle>();

// Sign-in cookies and the re-printable candidate passwords are encrypted with this key ring; it must outlive a
// restart or a new container, so it lives in App_Data (git-ignored) unless DataProtection:KeysPath says otherwise.
var keysPath = builder.Configuration["DataProtection:KeysPath"];
builder.Services.AddDataProtection()
    .SetApplicationName("ExamPrep")
    .PersistKeysToFileSystem(new DirectoryInfo(string.IsNullOrWhiteSpace(keysPath)
        ? Path.Combine(builder.Environment.ContentRootPath, "App_Data", "keys")
        : Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, keysPath))));
builder.Services.AddScoped<AttemptExpiry>();
builder.Services.AddHostedService<AttemptExpiryWorker>();
builder.Services.AddProblemDetails();
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks().AddDbContextCheck<ExamPrepDbContext>("database");

var app = builder.Build();

await AdminBootstrapper.EnsureAdminAsync(app.Services, app.Configuration);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    var demo = app.Configuration.GetSection("Demo").Get<DemoOptions>() ?? new DemoOptions();
    demo = demo with
    {
        SamplesRoot = Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, demo.SamplesRoot)),
        ReadingWritingFolder = string.IsNullOrWhiteSpace(demo.ReadingWritingFolder)
            ? null
            : Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, demo.ReadingWritingFolder)),
    };
    await DemoSeeder.SeedAsync(app.Services, demo, CancellationToken.None);
}

if (!app.Environment.IsDevelopment())
{
    // Unhandled errors become a plain ProblemDetails 500: no stack trace, SQL or path reaches the browser.
    app.UseExceptionHandler();
    app.UseHsts();
}

app.Use((context, next) =>
{
    // The API only returns JSON and exam files: nothing here may be framed, sniffed or leak a referrer.
    var headers = context.Response.Headers;
    headers.XContentTypeOptions = "nosniff";
    headers.XFrameOptions = "DENY";
    headers["Referrer-Policy"] = "no-referrer";
    headers.ContentSecurityPolicy = "default-src 'none'; frame-ancestors 'none'";
    return next(context);
});

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

/// <summary>Entry point marker so integration tests can host the API with WebApplicationFactory.</summary>
public partial class Program;
