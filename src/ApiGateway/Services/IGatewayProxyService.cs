namespace ApiGateway.Services;

public interface IGatewayProxyService
{
    Task ProxyAsync(HttpContext context);
    IReadOnlyCollection<string> RoutePrefixes { get; }
}
