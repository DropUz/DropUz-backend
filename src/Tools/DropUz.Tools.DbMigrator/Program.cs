using DropUz.Common.Infrastructure;
using DropUz.Common.Infrastructure.Data;
using DropUz.Modules.Admin.Infrastructure;
using DropUz.Modules.Cargo.Infrastructure;
using DropUz.Modules.Cart.Infrastructure;
using DropUz.Modules.Catalog.Infrastructure;
using DropUz.Modules.Identity.Infrastructure;
using DropUz.Modules.Identity.Infrastructure.Database;
using DropUz.Modules.Notifications.Infrastructure;
using DropUz.Modules.Orders.Infrastructure;
using DropUz.Modules.Payments.Infrastructure;
using DropUz.Modules.Sellers.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile("appsettings.Development.json", optional: true)
    .AddEnvironmentVariables();

builder.Services.AddDropUzCommonInfrastructure(builder.Configuration);
builder.Services.AddIdentityModule(builder.Configuration);
builder.Services.AddCatalogModule();
builder.Services.AddSellersModule();
builder.Services.AddCartModule();
builder.Services.AddOrdersModule();
builder.Services.AddNotificationsModule();
builder.Services.AddPaymentsModule();
builder.Services.AddCargoModule(builder.Configuration);
builder.Services.AddAdminModule();

using IHost host = builder.Build();
using IServiceScope scope = host.Services.CreateScope();

var schemaInitializer = scope.ServiceProvider.GetRequiredService<IDatabaseSchemaInitializer>();
await schemaInitializer.InitializeAsync();

var mainDbContext = scope.ServiceProvider.GetRequiredService<MainDbContext>();
await mainDbContext.Database.MigrateAsync();

var identityDbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
await identityDbContext.Database.MigrateAsync();

Console.WriteLine("DropUz database schemas and EF migrations applied.");
