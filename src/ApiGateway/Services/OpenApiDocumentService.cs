namespace ApiGateway.Services;

public sealed class OpenApiDocumentService : IOpenApiDocumentService
{
    public string ReadYaml()
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
}
