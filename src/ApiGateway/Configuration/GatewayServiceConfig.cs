namespace ApiGateway.Configuration;

public sealed class GatewayServiceConfig
{
    public const string Name = "Services";

    public string Identity { get; set; } = "__IDENTITY_SERVICE_BASE_URL__/api/v1/identity/";
    public string Institute { get; set; } = "__INSTITUTE_SERVICE_BASE_URL__/api/v1/institute/";
    public string Billing { get; set; } = "__BILLING_SERVICE_BASE_URL__/api/v1/billing/";
}
