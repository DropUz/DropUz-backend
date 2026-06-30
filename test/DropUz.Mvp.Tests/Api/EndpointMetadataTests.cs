using DropUz.Common.Presentation.Authorization;
using DropUz.Common.Presentation.Endpoints;
using DropUz.Modules.Admin.Presentation;
using DropUz.Modules.Cargo.Presentation;
using DropUz.Modules.Cart.Presentation;
using DropUz.Modules.Catalog.Presentation;
using DropUz.Modules.Notifications.Presentation;
using DropUz.Modules.Orders.Presentation;
using DropUz.Modules.Payments.Presentation;
using DropUz.Modules.Sellers.Presentation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using MediatR;
using Xunit;

namespace DropUz.Mvp.Tests.Api;

public sealed class EndpointMetadataTests
{
    [Theory]
    [InlineData("GetCatalogCategories", "Public: Catalog")]
    [InlineData("GetCatalogProducts", "Public: Products")]
    [InlineData("GetCatalogProducts", "User: Catalog")]
    [InlineData("CreateCatalogCategory", "Admin: Categories")]
    [InlineData("UpdateCatalogCategory", "Admin: Categories")]
    [InlineData("DeleteCatalogCategory", "Admin: Categories")]
    [InlineData("ImportCatalogProduct", "Admin: Products")]
    [InlineData("GetCatalogImportLogs", "Admin: Products")]
    [InlineData("ActivateCatalogProduct", "Admin: Products")]
    [InlineData("DeactivateCatalogProduct", "Admin: Products")]
    [InlineData("DeleteCatalogProduct", "Admin: Products")]
    [InlineData("GetAdminOrders", "Admin: Orders")]
    [InlineData("RecordCargoPrice", "Admin: Cargo")]
    [InlineData("GetSellerBalances", "Admin: Sellers")]
    [InlineData("GetAdminPayments", "Admin: Payments")]
    [InlineData("GetAdminSupportTelegramUrl", "Admin: Settings")]
    [InlineData("GetAdminDashboard", "Admin: Dashboard")]
    [InlineData("CreateSellerShop", "Seller: Shop")]
    [InlineData("AddSellerProduct", "Seller: Products")]
    [InlineData("RemoveSellerProduct", "Seller: Products")]
    [InlineData("GetSellerOrders", "Seller: Orders")]
    [InlineData("GetSellerBalance", "Seller: Balance")]
    [InlineData("GetMyCart", "User: Cart")]
    [InlineData("CreateOrderFromCart", "User: Orders")]
    [InlineData("StartProductPayment", "User: Payments")]
    [InlineData("LinkTelegram", "User: Profile")]
    [InlineData("GetSellerShopProducts", "Public: Seller Shops")]
    public void EndpointUsesRequiredSwaggerTag(string endpointName, string expectedTag)
    {
        Endpoint endpoint = GetEndpoints()[endpointName];

        string[] tags = endpoint.Metadata.GetMetadata<ITagsMetadata>()?.Tags.ToArray() ?? [];

        Assert.Contains(expectedTag, tags);
    }

    [Theory]
    [InlineData("GetAdminStatus")]
    [InlineData("GetCatalogStatus")]
    [InlineData("GetSellersStatus")]
    [InlineData("GetCartStatus")]
    [InlineData("GetOrdersStatus")]
    [InlineData("GetPaymentsStatus")]
    [InlineData("GetCargoStatus")]
    [InlineData("GetNotificationsStatus")]
    public void OperationalStatusEndpointRequiresAdmin(string endpointName)
    {
        Endpoint endpoint = GetEndpoints()[endpointName];

        IReadOnlyList<IAuthorizeData> authorization = endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>();

        Assert.Contains(authorization, metadata => metadata.Policy == AuthorizationPolicies.Admin);
    }

    private static IReadOnlyDictionary<string, Endpoint> GetEndpoints()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddAuthorization();
        builder.Services.AddSingleton<ISender>(_ => throw new NotSupportedException());
        WebApplication app = builder.Build();

        IEndpoint[] modules =
        [
            new AdminEndpoints(),
            new CargoEndpoints(),
            new CartEndpoints(),
            new CatalogEndpoints(),
            new NotificationsEndpoints(),
            new OrdersEndpoints(),
            new PaymentsEndpoints(),
            new SellersEndpoints()
        ];

        foreach (IEndpoint module in modules)
        {
            module.MapEndpoint(app);
        }

        return ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(dataSource => dataSource.Endpoints)
            .Select(endpoint => new
            {
                Endpoint = endpoint,
                Name = endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName
            })
            .Where(item => item.Name is not null)
            .ToDictionary(item => item.Name!, item => item.Endpoint);
    }
}
