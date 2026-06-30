namespace DropUz.Modules.Notifications.Infrastructure.Delivery;

public sealed class NotificationDeliveryOptions
{
    public const string SectionName = "Notifications:Delivery";

    public bool Enabled { get; set; } = true;

    public int BatchSize { get; set; } = 20;

    public int PollIntervalSeconds { get; set; } = 15;
}
