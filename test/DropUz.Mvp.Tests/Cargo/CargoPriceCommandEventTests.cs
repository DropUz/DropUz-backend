using DropUz.Common.Application.Clock;
using DropUz.Common.Application.Data;
using DropUz.Common.Domain;
using DropUz.Modules.Admin.Application.Audit;
using DropUz.Modules.Cargo.Application.Cargo;
using DropUz.Modules.Cargo.Domain.Cargo;
using DropUz.Modules.Catalog.Domain.Pricing;
using DropUz.Modules.Notifications.Application.Notifications;
using DropUz.Modules.Notifications.Domain.Notifications;
using DropUz.Modules.Orders.Domain.Orders;
using DropUz.Mvp.Tests.Support;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DropUz.Mvp.Tests.Cargo;

public sealed class CargoPriceCommandEventTests
{
    [Fact]
    public async Task CargoRecordCommandRaisesEventWithoutDirectNotification()
    {
        DateTime nowUtc = new(2026, 06, 27, 17, 0, 0, DateTimeKind.Utc);
        Order order = CreateProductPaidOrder(Guid.NewGuid(), nowUtc.AddHours(-1));
        var repository = new InMemoryMainRepository(order, CargoSettings.CreateDefault(nowUtc));
        await using ServiceProvider provider = CreateProvider(nowUtc, repository);

        Result<CargoPriceResponse> result = await provider.GetRequiredService<ISender>().Send(
            new RecordCargoPriceCommand(order.Id, CargoPrice: 28m, DeadlineDays: 4),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        CargoPriceRecord record = Assert.Single(repository.Entities.OfType<CargoPriceRecord>());
        CargoPriceAddedDomainEvent domainEvent = Assert.Single(
            order.DomainEvents.OfType<CargoPriceAddedDomainEvent>());
        Assert.Equal(28m, record.Amount);
        Assert.Equal(nowUtc.AddDays(4), record.DeadlineAtUtc);
        Assert.Equal(28m, domainEvent.CargoPrice);
        Assert.Empty(repository.Entities.OfType<NotificationMessage>());
    }

    private static ServiceProvider CreateProvider(DateTime nowUtc, InMemoryMainRepository repository)
    {
        var services = new ServiceCollection();

        services.AddSingleton<IDateTimeProvider>(new TestDateTimeProvider(nowUtc));
        services.AddSingleton<IMainRepository>(repository);
        services.AddSingleton<INotificationService, InMemoryNotificationService>();
        services.AddSingleton<IAdminAuditService, NoOpAdminAuditService>();
        services.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssembly(DropUz.Modules.Cargo.Application.AssemblyReference.Assembly));

        return services.BuildServiceProvider(validateScopes: true);
    }

    private static Order CreateProductPaidOrder(Guid userId, DateTime createdAtUtc)
    {
        Order order = Order.Create(
            userId,
            sellerId: null,
            [
                new OrderItemSnapshot(
                    ProductId: Guid.NewGuid(),
                    ProductName: "Bag",
                    ProductImageUrl: null,
                    VariantName: null,
                    SourcePlatform: "taobao",
                    SourceProductId: "TB-CARGO-COMMAND-1",
                    SourceUrl: null,
                    ApiPrice: 100m,
                    CurrencyRate: 1m,
                    DropUzMarkup: new Markup(MarkupType.Percent, 10m),
                    DropUzMarkupAmount: 10m,
                    DropUzFinalPrice: 110m,
                    SellerId: null,
                    SellerMarkup: null,
                    SellerProfit: 0m,
                    FinalProductPrice: 110m,
                    CargoPrice: 0m,
                    Quantity: 1)
            ],
            createdAtUtc);

        order.MarkProductPaid(createdAtUtc.AddMinutes(10));

        return order;
    }

    private sealed class TestDateTimeProvider(DateTime utcNow) : IDateTimeProvider
    {
        public DateTime UtcNow => utcNow;

        public DateTimeOffset OffsetUtcNow => new(utcNow);
    }

    private sealed class InMemoryNotificationService(
        IMainRepository repository,
        IDateTimeProvider dateTimeProvider) : INotificationService
    {
        public async Task EnqueueAsync(
            Guid userId,
            Guid? orderId,
            NotificationType type,
            string subject,
            string body,
            CancellationToken cancellationToken = default)
        {
            await repository.AddAsync(NotificationMessage.Create(
                userId,
                orderId,
                type,
                NotificationChannel.Email,
                userId.ToString(),
                subject,
                body,
                dateTimeProvider.UtcNow));
        }
    }

    private sealed class NoOpAdminAuditService : IAdminAuditService
    {
        public Task RecordAsync(
            string action,
            string entityType,
            Guid? entityId = null,
            string? details = null,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
