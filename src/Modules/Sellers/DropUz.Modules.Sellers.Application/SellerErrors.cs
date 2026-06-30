using DropUz.Common.Domain;

namespace DropUz.Modules.Sellers.Application;

public static class SellerErrors
{
    public static readonly Error UserNotAuthenticated = Error.Unauthorized(
        "Sellers.UserNotAuthenticated",
        "Authenticated seller user is required.");

    public static readonly Error ShopNameRequired = Error.Validation(
        "Sellers.ShopNameRequired",
        "Shop name is required.");

    public static readonly Error SlugRequired = Error.Validation(
        "Sellers.SlugRequired",
        "Shop slug is required.");

    public static readonly Error SellerNotFound = Error.NotFound(
        "Sellers.SellerNotFound",
        "Seller profile was not found.");

    public static readonly Error SellerProductNotFound = Error.NotFound(
        "Sellers.SellerProductNotFound",
        "Seller product was not found.");

    public static readonly Error CatalogProductNotAvailable = Error.Validation(
        "Sellers.CatalogProductNotAvailable",
        "Only approved catalog products can be added to a seller shop.");

    public static readonly Error MarkupInvalid = Error.Validation(
        "Sellers.MarkupInvalid",
        "Markup value cannot be negative.");

    public static readonly Error WithdrawalInvalid = Error.Validation(
        "Sellers.WithdrawalInvalid",
        "Withdrawal amount is invalid or exceeds available balance.");
}
