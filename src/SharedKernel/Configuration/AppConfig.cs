namespace SharedKernel.Configuration;

public sealed class AppConfig
{
    public const string Name = "App";

    public string Environment { get; set; } = "__ENVIRONMENT__";
    public string ApplicationName { get; set; } = "Coaching Institute Management System";
}
