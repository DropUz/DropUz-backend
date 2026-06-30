using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DropUz.Common.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                schema: "payments",
                table: "payments",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.Sql(
                """
                WITH ranked_pending_payments AS (
                    SELECT "Id",
                           ROW_NUMBER() OVER (
                               PARTITION BY "OrderId", "UserId", "Type"
                               ORDER BY "CreatedAtUtc" DESC, "Id" DESC) AS row_number
                    FROM payments.payments
                    WHERE "Status" = 1
                )
                UPDATE payments.payments AS payment
                SET "Status" = 4
                FROM ranked_pending_payments AS ranked
                WHERE payment."Id" = ranked."Id"
                  AND ranked.row_number > 1;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_payments_OrderId_UserId_Type",
                schema: "payments",
                table: "payments",
                columns: new[] { "OrderId", "UserId", "Type" },
                unique: true,
                filter: "\"Status\" = 1");

            migrationBuilder.CreateIndex(
                name: "IX_payments_UserId_IdempotencyKey",
                schema: "payments",
                table: "payments",
                columns: new[] { "UserId", "IdempotencyKey" },
                unique: true,
                filter: "\"IdempotencyKey\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_payments_OrderId_UserId_Type",
                schema: "payments",
                table: "payments");

            migrationBuilder.DropIndex(
                name: "IX_payments_UserId_IdempotencyKey",
                schema: "payments",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                schema: "payments",
                table: "payments");
        }
    }
}
