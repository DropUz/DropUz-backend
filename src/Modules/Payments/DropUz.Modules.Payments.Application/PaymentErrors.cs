using DropUz.Common.Domain;

namespace DropUz.Modules.Payments.Application;

public static class PaymentErrors
{
    public static readonly Error UserNotAuthenticated = Error.Unauthorized(
        "Payments.UserNotAuthenticated",
        "Authenticated user is required.");

    public static readonly Error PaymentNotFound = Error.NotFound(
        "Payments.PaymentNotFound",
        "Payment was not found.");

    public static readonly Error OrderNotFound = Error.NotFound(
        "Payments.OrderNotFound",
        "Order was not found.");

    public static readonly Error PaymentNotAllowed = Error.Validation(
        "Payments.PaymentNotAllowed",
        "Payment is not allowed for the current order status.");

    public static readonly Error ProviderUnavailable = Error.Failure(
        "Payments.ProviderUnavailable",
        "No payment provider is available for this payment method.");

    public static readonly Error ProviderRejected = Error.Failure(
        "Payments.ProviderRejected",
        "The payment provider did not confirm the operation.");

    public static readonly Error IdempotencyKeyInvalid = Error.Validation(
        "Payments.IdempotencyKeyInvalid",
        "Idempotency key cannot exceed 200 characters.");

    public static readonly Error IdempotencyKeyConflict = Error.Conflict(
        "Payments.IdempotencyKeyConflict",
        "Idempotency key was already used for another payment request.");
}
