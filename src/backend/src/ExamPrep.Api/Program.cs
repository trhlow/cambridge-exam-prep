using ExamPrep.Api.Admin;
using ExamPrep.Api.Auth;
using ExamPrep.Api.Content;
using ExamPrep.Api.Delivery;
using ExamPrep.Api.Demo;
using ExamPrep.Api.Realtime;
using ExamPrep.Infrastructure;
using ExamPrep.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration, Path.Combine(builder.Environment.ContentRootPath, "App_Data", "assets"));
builder.Services.AddStaffAuthentication(builder.Environment);
builder.Services.AddCandidateAuthentication(builder.Environment);
builder.Services.AddAssetAccess();
builder.Services.AddScoped<CandidateSectionQuery>();
builder.Services.AddScoped<CandidateCredentials>();
builder.Services.AddSingleton<LoginThrottle>();
builder.Services.AddScoped<ExamPrep.Api.Approvals.ApprovalService>();
builder.Services.Configure<ConverterOptions>(builder.Configuration.GetSection("Converter"));
builder.Services.AddSingleton<IPaperConverter, PythonPaperConverter>();

// Sign-in cookies and the re-printable candidate passwords are encrypted with this key ring; it must outlive a
// restart or a new container, so it lives in App_Data (git-ignored) unless DataProtection:KeysPath says otherwise.
var keysPath = builder.Configuration["DataProtection:KeysPath"];
builder.Services.AddDataProtection()
    .SetApplicationName("ExamPrep")
    .PersistKeysToFileSystem(new DirectoryInfo(string.IsNullOrWhiteSpace(keysPath)
        ? Path.Combine(builder.Environment.ContentRootPath, "App_Data", "keys")
        : Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, keysPath))));
builder.Services.AddSignalR(options =>
{
    // VE-13: a candidate whose network drops is seen offline within seconds, not after the default 30 s.
    options.KeepAliveInterval = TimeSpan.FromSeconds(5);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(12);
});
builder.Services.AddSingleton<RoomNotifier>();
builder.Services.AddHostedService<RoomBroadcaster>();
builder.Services.AddScoped<AttemptExpiry>();
builder.Services.AddHostedService<AttemptExpiryWorker>();
builder.Services.AddProblemDetails();
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks().AddDbContextCheck<ExamPrepDbContext>("database");

var app = builder.Build();

if (app.Configuration.GetValue<bool>("Database:MigrateOnStart"))
{
    // Docker (docker/docker-compose.yml) brings the schema up to date before anything reads it.
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<ExamPrepDbContext>().Database.MigrateAsync();
}

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
        ListeningFolder = string.IsNullOrWhiteSpace(demo.ListeningFolder)
            ? null
            : Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, demo.ListeningFolder)),
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
app.MapHub<StaffRoomHub>(StaffRoomHub.Path);
app.MapHub<CandidateRoomHub>(CandidateRoomHub.Path);
app.MapHealthChecks("/health");

app.Run();

/// <summary>Entry point marker so integration tests can host the API with WebApplicationFactory.</summary>
public partial class Program;
