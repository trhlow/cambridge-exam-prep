using ExamPrep.Api.Auth;
using ExamPrep.Api.Content;
using ExamPrep.Api.Delivery;
using ExamPrep.Api.Demo;
using ExamPrep.Infrastructure;
using ExamPrep.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration, Path.Combine(builder.Environment.ContentRootPath, "App_Data", "assets"));
builder.Services.AddStaffAuthentication(builder.Environment);
builder.Services.AddCandidateAuthentication(builder.Environment);
builder.Services.AddAssetAccess();
builder.Services.AddScoped<CandidateSectionQuery>();
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

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

/// <summary>Entry point marker so integration tests can host the API with WebApplicationFactory.</summary>
public partial class Program;
