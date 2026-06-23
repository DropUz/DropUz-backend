namespace DropUz.Modules.Cargo.Infrastructure.BackgroundJobs;

internal sealed class CargoPaymentExpirationOptions
{
    public const string SectionName = "Cargo:ExpirationJob";

    public bool Enabled { get; set; } = true;

    public int PollIntervalMinutes { get; set; } = 15;
}
