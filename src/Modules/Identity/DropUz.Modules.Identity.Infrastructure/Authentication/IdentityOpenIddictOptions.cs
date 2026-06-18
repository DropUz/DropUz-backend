namespace DropUz.Modules.Identity.Infrastructure.Authentication;

internal sealed class IdentityOpenIddictOptions
{
    public const string SectionName = "Identity:OpenIddict";

    public int AccessTokenLifetimeMinutes { get; set; } = 60;

    public int RefreshTokenLifetimeDays { get; set; } = 14;
}
