namespace NbcCms.Api.Security;

public class JwtOptions
{
    public string Issuer { get; set; } = "NbcCms.Api";
    public string Audience { get; set; } = "NbcCms.Clients";
    public string Key { get; set; } = string.Empty;
    public int LifetimeMinutes { get; set; } = 480;
}
