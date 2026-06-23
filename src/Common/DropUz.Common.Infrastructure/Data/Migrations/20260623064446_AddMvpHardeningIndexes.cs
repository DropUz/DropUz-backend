using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DropUz.Common.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMvpHardeningIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OrderNumber",
                schema: "orders",
                table: "orders",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE orders.orders
                SET "OrderNumber" = 'DUZ-' ||
                    to_char("CreatedAtUtc", 'YYYYMMDD') ||
                    '-' ||
                    upper(substring(replace("Id"::text, '-', ''), 1, 12))
                WHERE "OrderNumber" IS NULL OR "OrderNumber" = '';
                """);

            migrationBuilder.AlterColumn<string>(
                name: "OrderNumber",
                schema: "orders",
                table: "orders",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_seller_profiles_CreatedAtUtc",
                schema: "sellers",
                table: "seller_profiles",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_seller_products_CreatedAtUtc",
                schema: "sellers",
                table: "seller_products",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_seller_balance_transactions_CreatedAtUtc",
                schema: "sellers",
                table: "seller_balance_transactions",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_seller_balance_transactions_SellerId_OrderId_Type",
                schema: "sellers",
                table: "seller_balance_transactions",
                columns: new[] { "SellerId", "OrderId", "Type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_products_CreatedAtUtc",
                schema: "catalog",
                table: "products",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_payments_CreatedAtUtc",
                schema: "payments",
                table: "payments",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_orders_CreatedAtUtc",
                schema: "orders",
                table: "orders",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_orders_OrderNumber",
                schema: "orders",
                table: "orders",
                column: "OrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_messages_CreatedAtUtc",
                schema: "notifications",
                table: "messages",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_categories_CreatedAtUtc",
                schema: "catalog",
                table: "categories",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_carts_CreatedAtUtc",
                schema: "cart",
                table: "carts",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_cart_items_CreatedAtUtc",
                schema: "cart",
                table: "cart_items",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_cargo_price_records_CreatedAtUtc",
                schema: "cargo",
                table: "cargo_price_records",
                column: "CreatedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_seller_profiles_CreatedAtUtc",
                schema: "sellers",
                table: "seller_profiles");

            migrationBuilder.DropIndex(
                name: "IX_seller_products_CreatedAtUtc",
                schema: "sellers",
                table: "seller_products");

            migrationBuilder.DropIndex(
                name: "IX_seller_balance_transactions_CreatedAtUtc",
                schema: "sellers",
                table: "seller_balance_transactions");

            migrationBuilder.DropIndex(
                name: "IX_seller_balance_transactions_SellerId_OrderId_Type",
                schema: "sellers",
                table: "seller_balance_transactions");

            migrationBuilder.DropIndex(
                name: "IX_products_CreatedAtUtc",
                schema: "catalog",
                table: "products");

            migrationBuilder.DropIndex(
                name: "IX_payments_CreatedAtUtc",
                schema: "payments",
                table: "payments");

            migrationBuilder.DropIndex(
                name: "IX_orders_CreatedAtUtc",
                schema: "orders",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "IX_orders_OrderNumber",
                schema: "orders",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "IX_messages_CreatedAtUtc",
                schema: "notifications",
                table: "messages");

            migrationBuilder.DropIndex(
                name: "IX_categories_CreatedAtUtc",
                schema: "catalog",
                table: "categories");

            migrationBuilder.DropIndex(
                name: "IX_carts_CreatedAtUtc",
                schema: "cart",
                table: "carts");

            migrationBuilder.DropIndex(
                name: "IX_cart_items_CreatedAtUtc",
                schema: "cart",
                table: "cart_items");

            migrationBuilder.DropIndex(
                name: "IX_cargo_price_records_CreatedAtUtc",
                schema: "cargo",
                table: "cargo_price_records");

            migrationBuilder.DropColumn(
                name: "OrderNumber",
                schema: "orders",
                table: "orders");
        }
    }
}
