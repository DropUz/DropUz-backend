using DropUz.Common.Domain;

namespace DropUz.Modules.Orders.Application;

public static class OrderErrors
{
    public static readonly Error UserNotAuthenticated = Error.Unauthorized(
        "Orders.UserNotAuthenticated",
        "Authenticated user is required.");

    public static readonly Error OrderNotFound = Error.NotFound(
        "Orders.OrderNotFound",
        "Order was not found.");

    public static readonly Error EmptyCart = Error.Validation(
        "Orders.EmptyCart",
        "Cart is empty.");

    public static readonly Error CargoPriceInvalid = Error.Validation(
        "Orders.CargoPriceInvalid",
        "Cargo price must be greater than zero.");

    public static readonly Error InvalidStatusTransition = Error.Validation(
        "Orders.InvalidStatusTransition",
        "Order status transition is not allowed.");
}
