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
    sampleTenantHosts = new[] { "brightclasses.coachapp.local", "apexacademy.coachapp.local" }
}));

app.MapHealthChecks("/health");

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
