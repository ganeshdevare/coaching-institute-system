using ApiGateway.Services;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.Controllers;

[ApiController]
public sealed class SwaggerController(IOpenApiDocumentService openApi) : ControllerBase
{
    [HttpGet("/swagger")]
    public ContentResult Index()
        => Content("""
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
""", "text/html");

    [HttpGet("/swagger/v1/swagger.yaml")]
    [HttpGet("/openapi.yaml")]
    public ContentResult Yaml()
        => Content(openApi.ReadYaml(), "application/yaml");
}
