using ApiGateway.Services;
using SharedKernel;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("ocelot.json", optional: true, reloadOnChange: true);
builder.Services.AddCoachSharedKernel(builder.Configuration);
builder.Services.AddHttpClient();
builder.Services.AddScoped<IGatewayProxyService, GatewayProxyService>();
builder.Services.AddScoped<IOpenApiDocumentService, OpenApiDocumentService>();
builder.Services.AddControllers();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseCoachPlatform();
app.MapControllers();
app.MapHealthChecks("/health");
app.Run();
