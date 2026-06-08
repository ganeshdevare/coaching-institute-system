using ApiGateway.Services;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;

namespace ApiGateway.Controllers;

[ApiController]
public sealed class GatewayController(IGatewayProxyService proxyService) : ControllerBase
{
    [HttpGet("/")]
    public IActionResult Index()
        => Ok(ApiResponse<object>.Ok(new
        {
            name = "Coaching Institute Management System Gateway",
            routes = proxyService.RoutePrefixes,
            swagger = "/swagger/v1/swagger.yaml",
            sampleTenantHosts = new[] { "brightclasses.coachapp.local", "apexacademy.coachapp.local" }
        }));

    [Route("/{**path}")]
    [AcceptVerbs("GET", "POST", "PUT", "PATCH", "DELETE")]
    public Task Proxy()
        => proxyService.ProxyAsync(HttpContext);
}
