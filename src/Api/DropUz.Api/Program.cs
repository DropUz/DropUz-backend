using DropUz.Api.Extensions;
using DropUz.Api.Infrastructure;
using DropUz.Common.Infrastructure;
using DropUz.Common.Presentation.Endpoints;
using DropUz.Modules.Admin.Infrastructure;
using DropUz.Modules.Cargo.Infrastructure;
using DropUz.Modules.Cart.Infrastructure;
using DropUz.Modules.Catalog.Infrastructure;
using DropUz.Modules.Identity.Infrastructure;
using DropUz.Modules.Notifications.Infrastructure;
using DropUz.Modules.Orders.Infrastructure;
using DropUz.Modules.Payments.Infrastructure;
using DropUz.Modules.Sellers.Infrastructure;

namespace DropUz.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Configuration.AddModuleConfiguration([
                "identity",
                "catalog",
                "sellers",
                "cart",
                "orders",
                "payments",
                "cargo",
                "notifications",
                "admin"]);

            // Add services to the container.

            builder.Services.AddDropUzCommonInfrastructure(builder.Configuration);
            builder.Services.AddDropUzApiHardening(builder.Configuration);
            builder.Services.AddIdentityModule(builder.Configuration);
            builder.Services.AddCatalogModule();
            builder.Services.AddSellersModule();
            builder.Services.AddCartModule();
            builder.Services.AddOrdersModule();
            builder.Services.AddNotificationsModule(builder.Configuration);
            builder.Services.AddPaymentsModule();
            builder.Services.AddCargoModule(builder.Configuration);
            builder.Services.AddAdminModule();
            builder.Services.AddControllers();
            builder.Services.AddHealthChecks();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseDropUzApiHardening();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapGet("/", () => Results.Ok(new
            {
                name = "DropUz API",
                environment = app.Environment.EnvironmentName,
                version = typeof(Program).Assembly.GetName().Version?.ToString() ?? "dev"
            }));

            app.MapHealthChecks("/health/live");
            app.MapHealthChecks("/health/ready");

            app.MapEndpoints();

            app.MapControllers();

            app.Run();
        }
    }
}
