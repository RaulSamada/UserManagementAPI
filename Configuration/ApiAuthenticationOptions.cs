namespace UserManagementAPI.Configuration;

public sealed class ApiAuthenticationOptions
{
    public const string SectionName = "ApiAuthentication";

    public string Token { get; init; } = string.Empty;
}
