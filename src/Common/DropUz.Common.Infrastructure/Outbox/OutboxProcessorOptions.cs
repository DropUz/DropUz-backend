namespace DropUz.Common.Infrastructure.Outbox;

public sealed class OutboxProcessorOptions
{
    public const string SectionName = "Outbox";

    public bool Enabled { get; set; } = true;

    public int BatchSize { get; set; } = 20;

    public int PollIntervalSeconds { get; set; } = 15;
}
