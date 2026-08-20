namespace RepairFlow.Api.Configuration;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "RepairFlow";

    public string Audience { get; set; } = "RepairFlow.Client";

    /// <summary>Секрет подписи. В проде приходит из переменной окружения Jwt__Key.</summary>
    public string Key { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 15;

    public int RefreshTokenDays { get; set; } = 7;

    /// <summary>Имя httpOnly-куки, в которой живёт refresh-токен.</summary>
    public string RefreshCookieName { get; set; } = "rf_refresh";

    /// <summary>SameSite=None нужен, когда фронт и API на разных доменах (Vercel + Railway).</summary>
    public bool CrossSiteCookies { get; set; } = true;
}
