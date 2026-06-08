using System.Net.Http.Headers;
using SharedKernel;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("ocelot.json", optional: true, reloadOnChange: true);
builder.Services.AddCoachSharedKernel(builder.Configuration);
builder.Services.AddHttpClient();
builder.Services.AddHealthChecks();

var app = builder.Build();
app.UseCoachPlatform();

var routes = new Dictionary<string, string>
{
    ["/api/identity/"] = builder.Configuration["Services:Identity"] ?? "http://localhost:5101/api/v1/identity/",
    ["/api/institute/"] = builder.Configuration["Services:Institute"] ?? "http://localhost:5102/api/v1/institute/",
    ["/api/billing/"] = builder.Configuration["Services:Billing"] ?? "http://localhost:5103/api/v1/billing/"
};

app.MapGet("/", () => ApiResponse<object>.Ok(new
{
    name = "Coaching Institute Management System Gateway",
    routes = routes.Keys,
    swagger = "/swagger/v1/swagger.yaml",
    sampleTenantHosts = new[] { "brightclasses.coachapp.local", "apexacademy.coachapp.local" }
}));

app.MapHealthChecks("/health");

app.MapGet("/swagger", () => Results.Content("""
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <title>Coaching Institute Management System API</title>
</head>
<body>
  <h1>Coaching Institute Management System API</h1>
  <p>OpenAPI YAML: <a href="/swagger/v1/swagger.yaml">/swagger/v1/swagger.yaml</a></p>
  <p>Alias: <a href="/openapi.yaml">/openapi.yaml</a></p>
</body>
</html>
""", "text/html"));

app.MapGet("/swagger/v1/swagger.yaml", () => Results.Text(ReadOpenApiYaml(), "application/yaml"));
app.MapGet("/openapi.yaml", () => Results.Text(ReadOpenApiYaml(), "application/yaml"));

app.Map("/{**path}", async (HttpContext context, IHttpClientFactory clientFactory) =>
{
    var path = "/" + (context.Request.RouteValues["path"]?.ToString() ?? string.Empty);
    var match = routes.FirstOrDefault(x => path.StartsWith(x.Key, StringComparison.OrdinalIgnoreCase));
    if (match.Key is null)
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        await context.Response.WriteAsJsonAsync(ApiResponse<object>.Fail("No gateway route matched."));
        return;
    }

    var downstreamPath = path.Replace(match.Key, match.Value, StringComparison.OrdinalIgnoreCase);
    var uri = downstreamPath + context.Request.QueryString;
    using var request = new HttpRequestMessage(new HttpMethod(context.Request.Method), uri);
    foreach (var header in context.Request.Headers)
    {
        if (!request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()))
        {
            request.Content ??= new StreamContent(context.Request.Body);
            request.Content.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
        }
    }

    if (context.Request.ContentLength > 0 && request.Content is null)
    {
        request.Content = new StreamContent(context.Request.Body);
        if (!string.IsNullOrWhiteSpace(context.Request.ContentType))
        {
            request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(context.Request.ContentType);
        }
    }

    using var response = await clientFactory.CreateClient().SendAsync(request);
    context.Response.StatusCode = (int)response.StatusCode;
    foreach (var header in response.Headers) context.Response.Headers[header.Key] = header.Value.ToArray();
    foreach (var header in response.Content.Headers) context.Response.Headers[header.Key] = header.Value.ToArray();
    await response.Content.CopyToAsync(context.Response.Body);
});

app.Run();

static string ReadOpenApiYaml()
{
    var current = new DirectoryInfo(AppContext.BaseDirectory);
    while (current is not null)
    {
        var candidate = Path.Combine(current.FullName, "docs", "openapi.yaml");
        if (File.Exists(candidate))
        {
            return File.ReadAllText(candidate);
        }

        current = current.Parent;
    }

    var workspaceCandidate = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "docs", "openapi.yaml"));
    return File.Exists(workspaceCandidate)
        ? File.ReadAllText(workspaceCandidate)
        : "openapi: 3.0.3\ninfo:\n  title: Coaching Institute Management System API\n  version: 1.0.0\npaths: {}\n";
}
