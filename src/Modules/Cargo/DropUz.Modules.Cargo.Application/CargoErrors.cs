using DropUz.Common.Domain;

namespace DropUz.Modules.Cargo.Application;

public static class CargoErrors
{
    public static readonly Error OrderNotFound = Error.NotFound(
        "Cargo.OrderNotFound",
        "Order was not found.");

    public static readonly Error CargoPriceInvalid = Error.Validation(
        "Cargo.CargoPriceInvalid",
        "Cargo price must be greater than zero.");

    public static readonly Error CargoPriceNotAllowed = Error.Validation(
        "Cargo.CargoPriceNotAllowed",
        "Cargo price cannot be entered for the current order status.");
}
