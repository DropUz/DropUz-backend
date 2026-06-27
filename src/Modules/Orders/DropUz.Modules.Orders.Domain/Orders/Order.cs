using DropUz.Common.Domain;

namespace DropUz.Modules.Orders.Domain.Orders;

public sealed class Order : Entity
{
    private readonly List<OrderItem> _items = [];
    private readonly List<OrderStatusHistory> _statusHistory = [];

    private Order()
    {
    }

    private Order(Guid id, Guid userId, Guid? sellerId, DateTime createdAtUtc)
        : base(id)
    {
        UserId = userId;
        SellerId = sellerId;
        OrderNumber = GenerateOrderNumber(id, createdAtUtc);
        Status = OrderStatus.PendingProductPayment;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public string OrderNumber { get; private set; } = string.Empty;

    public Guid UserId { get; private set; }

    public Guid? SellerId { get; private set; }

    public OrderStatus Status { get; private set; }

    public decimal ProductTotal { get; private set; }

    public decimal CargoTotal { get; private set; }

    public decimal Total { get; private set; }

    public decimal SellerProfitTotal { get; private set; }

    public DateTime? ProductPaidAtUtc { get; private set; }

    public DateTime? CargoPaidAtUtc { get; private set; }

    public DateTime? CargoPaymentDeadlineAt { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    public IReadOnlyCollection<OrderStatusHistory> StatusHistory => _statusHistory.AsReadOnly();

    public static Order Create(
        Guid userId,
        Guid? sellerId,
        IReadOnlyCollection<OrderItemSnapshot> snapshots,
        DateTime createdAtUtc)
    {
        if (snapshots.Count == 0)
        {
            throw new ArgumentException("Order requires at least one item.", nameof(snapshots));
        }

        var order = new Order(Guid.NewGuid(), userId, sellerId, createdAtUtc);

        foreach (OrderItemSnapshot snapshot in snapshots)
        {
            order._items.Add(new OrderItem(Guid.NewGuid(), order.Id, snapshot));
        }

        order.RecalculateTotals();
        order.AddHistory(OrderStatus.PendingProductPayment, "Order created.", createdAtUtc);

        return order;
    }

    public bool MarkProductPaid(DateTime nowUtc)
    {
        if (Status != OrderStatus.PendingProductPayment)
        {
            return false;
        }

        ProductPaidAtUtc = nowUtc;
        ChangeStatus(OrderStatus.ProductPaid, "Product payment received.", nowUtc);

        return true;
    }

    public bool SetCargoPrice(decimal cargoPrice, int deadlineDays, DateTime nowUtc)
    {
        if (cargoPrice <= 0m || !CanSetCargoPrice())
        {
            return false;
        }

        decimal perItemCargo = _items.Count == 0 ? cargoPrice : decimal.Round(cargoPrice / _items.Count, 2, MidpointRounding.AwayFromZero);
        foreach (OrderItem item in _items)
        {
            item.SetCargoPrice(perItemCargo);
        }

        CargoTotal = cargoPrice;
        Total = ProductTotal + CargoTotal;
        CargoPaymentDeadlineAt = nowUtc.AddDays(deadlineDays <= 0 ? 7 : deadlineDays);
        ChangeStatus(OrderStatus.PendingCargoPayment, "Cargo price added.", nowUtc);

        return true;
    }

    public bool MarkCargoPaid(DateTime nowUtc)
    {
        if (Status != OrderStatus.PendingCargoPayment)
        {
            return false;
        }

        CargoPaidAtUtc = nowUtc;
        ChangeStatus(OrderStatus.CargoPaid, "Cargo payment received.", nowUtc);

        return true;
    }

    public bool UpdateStatus(OrderStatus status, string? note, DateTime nowUtc)
    {
        if (!CanUpdateStatus(status))
        {
            return false;
        }

        ChangeStatus(status, note, nowUtc);
        if (status == OrderStatus.Delivered && SellerId.HasValue)
        {
            RaiseDomainEvent(new OrderDeliveredDomainEvent(
                Id,
                UserId,
                SellerId.Value,
                SellerProfitTotal,
                nowUtc));
        }
        else if (status == OrderStatus.CargoPaymentExpired)
        {
            RaiseCargoPaymentExpiredDomainEvent(nowUtc);
        }

        return true;
    }

    public void ExpireCargoPayment(DateTime nowUtc)
    {
        if (Status == OrderStatus.PendingCargoPayment &&
            CargoPaymentDeadlineAt.HasValue &&
            CargoPaymentDeadlineAt.Value < nowUtc)
        {
            ChangeStatus(OrderStatus.CargoPaymentExpired, "Cargo payment deadline expired.", nowUtc);
            RaiseCargoPaymentExpiredDomainEvent(nowUtc);
        }
    }

    private void RaiseCargoPaymentExpiredDomainEvent(DateTime expiredAtUtc)
    {
        RaiseDomainEvent(new CargoPaymentExpiredDomainEvent(
            Id,
            UserId,
            SellerId,
            SellerProfitTotal,
            expiredAtUtc));
    }

    private void ChangeStatus(OrderStatus status, string? note, DateTime nowUtc)
    {
        Status = status;
        UpdatedAtUtc = nowUtc;
        AddHistory(status, note, nowUtc);
    }

    private void AddHistory(OrderStatus status, string? note, DateTime changedAtUtc)
    {
        _statusHistory.Add(new OrderStatusHistory(Guid.NewGuid(), Id, status, note, changedAtUtc));
    }

    private void RecalculateTotals()
    {
        ProductTotal = _items.Sum(item => item.ProductLineTotal);
        CargoTotal = _items.Sum(item => item.CargoPrice);
        Total = ProductTotal + CargoTotal;
        SellerProfitTotal = _items.Sum(item => item.SellerProfitTotal);
    }

    private bool CanSetCargoPrice()
    {
        return Status is OrderStatus.ProductPaid
            or OrderStatus.Purchasing
            or OrderStatus.Purchased
            or OrderStatus.InForeignWarehouse
            or OrderStatus.CargoCalculating
            or OrderStatus.PendingCargoPayment;
    }

    private bool CanUpdateStatus(OrderStatus nextStatus)
    {
        if (Status == nextStatus || IsTerminal(Status))
        {
            return false;
        }

        if (nextStatus is OrderStatus.Cancelled or OrderStatus.Refunded)
        {
            return true;
        }

        if (nextStatus == OrderStatus.CargoPaymentExpired)
        {
            return Status == OrderStatus.PendingCargoPayment;
        }

        return Status switch
        {
            OrderStatus.PendingProductPayment => nextStatus == OrderStatus.ProductPaid,
            OrderStatus.ProductPaid => nextStatus is OrderStatus.Purchasing
                or OrderStatus.Purchased
                or OrderStatus.InForeignWarehouse
                or OrderStatus.CargoCalculating,
            OrderStatus.Purchasing => nextStatus is OrderStatus.Purchased
                or OrderStatus.InForeignWarehouse
                or OrderStatus.CargoCalculating,
            OrderStatus.Purchased => nextStatus is OrderStatus.InForeignWarehouse
                or OrderStatus.CargoCalculating,
            OrderStatus.InForeignWarehouse => nextStatus == OrderStatus.CargoCalculating,
            OrderStatus.CargoCalculating => false,
            OrderStatus.PendingCargoPayment => false,
            OrderStatus.CargoPaid => nextStatus is OrderStatus.InTransit
                or OrderStatus.ArrivedUzbekistan
                or OrderStatus.Delivered,
            OrderStatus.InTransit => nextStatus is OrderStatus.ArrivedUzbekistan
                or OrderStatus.Delivered,
            OrderStatus.ArrivedUzbekistan => nextStatus == OrderStatus.Delivered,
            _ => false
        };
    }

    private static bool IsTerminal(OrderStatus status)
    {
        return status is OrderStatus.Delivered
            or OrderStatus.Cancelled
            or OrderStatus.Refunded
            or OrderStatus.CargoPaymentExpired;
    }

    private static string GenerateOrderNumber(Guid id, DateTime createdAtUtc)
    {
        return $"DUZ-{createdAtUtc:yyyyMMdd}-{id:N}"[..25].ToUpperInvariant();
    }
}
