using InstituteService.Services;
using SharedKernel;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCoachSharedKernel(builder.Configuration);
builder.Services.AddScoped<IInstituteAppService, InstituteAppService>();
builder.Services.AddControllers();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseCoachPlatform();
app.MapControllers();
app.MapHealthChecks("/health");
app.Run();
