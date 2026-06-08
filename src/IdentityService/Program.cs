using IdentityService.Services;
using SharedKernel;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCoachSharedKernel(builder.Configuration);
builder.Services.AddScoped<IIdentityAppService, IdentityAppService>();
builder.Services.AddControllers();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseCoachPlatform();
SeedData.Ensure(app.Services);

app.MapControllers();
app.MapHealthChecks("/health");
app.Run();
