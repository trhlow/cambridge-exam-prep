using ExamPrep.Api.Auth;
using ExamPrep.Api.Content;
using ExamPrep.Infrastructure;
using ExamPrep.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration, Path.Combine(builder.Environment.ContentRootPath, "App_Data", "assets"));
builder.Services.AddStaffAuthentication(builder.Environment);
builder.Services.AddCandidateAuthentication(builder.Environment);
builder.Services.AddAssetAccess();
builder.Services.AddProblemDetails();
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks().AddDbContextCheck<ExamPrepDbContext>("database");

var app = builder.Build();

await AdminBootstrapper.EnsureAdminAsync(app.Services, app.Configuration);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

/// <summary>Entry point marker so integration tests can host the API with WebApplicationFactory.</summary>
public partial class Program;
