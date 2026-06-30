using DropUz.Common.Application.Abstractions;
using DropUz.Common.Application.Clock;
using DropUz.Common.Domain;
using DropUz.Modules.Catalog.Domain.Pricing;
using DropUz.Modules.Orders.Domain.Orders;
using DropUz.Modules.Payments.Application;
using DropUz.Modules.Payments.Application.Payments;
using DropUz.Modules.Payments.Domain.Payments;
using DropUz.Mvp.Tests.Support;
using Xunit;

namespace DropUz.Mvp.Tests.Payments;

public sealed class PaymentProviderAbstractionTests
{
    [Fact]
    public async Task StartPaymentReusesSameIdempotencyKeyWithoutCallingProviderTwice()
    {
        DateTime nowUtc = new(2026, 06, 30, 7, 30, 0, DateTimeKind.Utc);
        Guid userId = Guid.NewGuid();
        Order order = CreateOrder(userId, nowUtc.AddMinutes(-5));
        var repository = new InMemoryMainRepository(order);
        var provider = new TestPaymentProvider
        {
            Name = "mock-pay",
            StartResult = PaymentProviderResult.Success("mock-idempotent-1")
        };
        var handler = new StartPaymentCommandHandler(
            repository,
            new TestCurrentUser(userId),
            new TestDateTimeProvider(nowUtc),
            new PaymentProviderRegistry([provider]));
        var command = new StartPaymentCommand(
            order.Id,
            PaymentType.ProductPayment,
            PaymentMethod.Uzcard,
            "checkout-attempt-1");

        Result<PaymentResponse> first = await handler.Handle(command, CancellationToken.None);
        Result<PaymentResponse> second = await handler.Handle(command, CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(first.Value.Id, second.Value.Id);
        Assert.Equal("checkout-attempt-1", first.Value.IdempotencyKey);
        Assert.Single(provider.StartRequests);
        Assert.Equal("checkout-attempt-1", provider.StartRequests[0].IdempotencyKey);
        Assert.Single(repository.Entities.OfType<Payment>());
    }

    [Fact]
    public async Task StartPaymentRejectsIdempotencyKeyUsedForAnotherOrder()
    {
        DateTime nowUtc = new(2026, 06, 30, 7, 45, 0, DateTimeKind.Utc);
        Guid userId = Guid.NewGuid();
        Order firstOrder = CreateOrder(userId, nowUtc.AddMinutes(-10));
        Order secondOrder = CreateOrder(userId, nowUtc.AddMinutes(-5));
        Payment existingPayment = Payment.Start(
            firstOrder.Id,
            userId,
            PaymentType.ProductPayment,
            PaymentMethod.Uzcard,
            firstOrder.ProductTotal,
            "mock-pay",
            "mock-idempotent-2",
            nowUtc.AddMinutes(-3),
            "checkout-attempt-2");
        var handler = new StartPaymentCommandHandler(
            new InMemoryMainRepository(firstOrder, secondOrder, existingPayment),
            new TestCurrentUser(userId),
            new TestDateTimeProvider(nowUtc),
            new PaymentProviderRegistry([new TestPaymentProvider { Name = "mock-pay" }]));

        Result<PaymentResponse> result = await handler.Handle(
            new StartPaymentCommand(
                secondOrder.Id,
                PaymentType.ProductPayment,
                PaymentMethod.Uzcard,
                "checkout-attempt-2"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(PaymentErrors.IdempotencyKeyConflict, result.Error);
    }

    [Fact]
    public async Task StartPaymentUsesSelectedProviderTransactionSnapshot()
    {
        DateTime nowUtc = new(2026, 06, 30, 8, 0, 0, DateTimeKind.Utc);
        Guid userId = Guid.NewGuid();
        Order order = CreateOrder(userId, nowUtc.AddMinutes(-5));
        var repository = new InMemoryMainRepository(order);
        var provider = new TestPaymentProvider
        {
            Name = "mock-pay",
            StartResult = PaymentProviderResult.Success("mock-start-1")
        };
        var handler = new StartPaymentCommandHandler(
            repository,
            new TestCurrentUser(userId),
            new TestDateTimeProvider(nowUtc),
            new PaymentProviderRegistry([provider]));

        Result<PaymentResponse> result = await handler.Handle(
            new StartPaymentCommand(order.Id, PaymentType.ProductPayment, PaymentMethod.Uzcard),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("mock-pay", result.Value.Provider);
        Assert.Equal("mock-start-1", result.Value.ProviderTransactionId);
        StartPaymentProviderRequest request = Assert.Single(provider.StartRequests);
        Assert.Equal(order.Id, request.OrderId);
        Assert.Equal(userId, request.UserId);
        Assert.Equal(order.ProductTotal, request.Amount);
    }

    [Fact]
    public async Task ConfirmPaymentUsesVerifiedProviderTransaction()
    {
        DateTime nowUtc = new(2026, 06, 30, 9, 0, 0, DateTimeKind.Utc);
        Guid userId = Guid.NewGuid();
        Order order = CreateOrder(userId, nowUtc.AddMinutes(-10));
        Payment payment = Payment.Start(
            order.Id,
            userId,
            PaymentType.ProductPayment,
            PaymentMethod.Uzcard,
            order.ProductTotal,
            provider: "mock-pay",
            providerTransactionId: "mock-start-2",
            nowUtc.AddMinutes(-5));
        var repository = new InMemoryMainRepository(order, payment);
        var provider = new TestPaymentProvider
        {
            Name = "mock-pay",
            ConfirmResult = PaymentProviderResult.Success("mock-verified-2")
        };
        var handler = new ConfirmPaymentCommandHandler(
            repository,
            new TestCurrentUser(userId),
            new TestDateTimeProvider(nowUtc),
            new PaymentProviderRegistry([provider]));

        Result<PaymentResponse> result = await handler.Handle(
            new ConfirmPaymentCommand(payment.Id, "callback-reference"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentStatus.Paid, payment.Status);
        Assert.Equal("mock-verified-2", payment.ProviderTransactionId);
        Assert.Single(provider.ConfirmRequests);
    }

    [Fact]
    public async Task ProviderRejectionLeavesPaymentPending()
    {
        DateTime nowUtc = new(2026, 06, 30, 10, 0, 0, DateTimeKind.Utc);
        Guid userId = Guid.NewGuid();
        Order order = CreateOrder(userId, nowUtc.AddMinutes(-10));
        Payment payment = Payment.Start(
            order.Id,
            userId,
            PaymentType.ProductPayment,
            PaymentMethod.Uzcard,
            order.ProductTotal,
            provider: "mock-pay",
            providerTransactionId: "mock-start-3",
            nowUtc.AddMinutes(-5));
        var repository = new InMemoryMainRepository(order, payment);
        var provider = new TestPaymentProvider
        {
            Name = "mock-pay",
            ConfirmResult = PaymentProviderResult.Failure("Provider rejected confirmation.")
        };
        var handler = new ConfirmPaymentCommandHandler(
            repository,
            new TestCurrentUser(userId),
            new TestDateTimeProvider(nowUtc),
            new PaymentProviderRegistry([provider]));

        Result<PaymentResponse> result = await handler.Handle(
            new ConfirmPaymentCommand(payment.Id, "callback-reference"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(PaymentErrors.ProviderRejected, result.Error);
        Assert.Equal(PaymentStatus.Pending, payment.Status);
        Assert.Empty(payment.DomainEvents);
    }

    private static Order CreateOrder(Guid userId, DateTime createdAtUtc)
    {
        return Order.Create(
            userId,
            sellerId: null,
            [
                new OrderItemSnapshot(
                    ProductId: Guid.NewGuid(),
                    ProductName: "Bag",
                    ProductImageUrl: null,
                    VariantName: null,
                    SourcePlatform: "taobao",
                    SourceProductId: "TB-PROVIDER-1",
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
    }

    private sealed class TestCurrentUser(Guid userId) : ICurrentUser
    {
        public Guid? UserId { get; } = userId;

        public string? UserName => "user";

        public bool IsAuthenticated => true;

        public IReadOnlyCollection<string> Roles => ["user"];
    }

    private sealed class TestDateTimeProvider(DateTime utcNow) : IDateTimeProvider
    {
        public DateTime UtcNow => utcNow;

        public DateTimeOffset OffsetUtcNow => new(utcNow);
    }
}
