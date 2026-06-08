using System.Net.Http.Headers;
using SharedKernel;

namespace ApiGateway.Services;

public sealed class GatewayProxyService(IConfiguration configuration, IHttpClientFactory clientFactory) : IGatewayProxyService
{
    private readonly Dictionary<string, string> routes = new()
    {
        ["/api/identity/"] = configuration["Services:Identity"] ?? "http://localhost:5101/api/v1/identity/",
        ["/api/institute/"] = configuration["Services:Institute"] ?? "http://localhost:5102/api/v1/institute/",
        ["/api/billing/"] = configuration["Services:Billing"] ?? "http://localhost:5103/api/v1/billing/"
    };

    public IReadOnlyCollection<string> RoutePrefixes => routes.Keys;

    public async Task ProxyAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "/";
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

        foreach (var header in response.Headers)
        {
            context.Response.Headers[header.Key] = header.Value.ToArray();
        }

        foreach (var header in response.Content.Headers)
        {
            context.Response.Headers[header.Key] = header.Value.ToArray();
        }

        await response.Content.CopyToAsync(context.Response.Body);
    }
}
