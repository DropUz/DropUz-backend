using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DropUz.Common.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationDeliveryMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AttemptCount",
                schema: "notifications",
                table: "messages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastAttemptAtUtc",
                schema: "notifications",
                table: "messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderMessageId",
                schema: "notifications",
                table: "messages",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderName",
                schema: "notifications",
                table: "messages",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_messages_Status_CreatedAtUtc",
                schema: "notifications",
                table: "messages",
                columns: new[] { "Status", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_messages_Status_CreatedAtUtc",
                schema: "notifications",
                table: "messages");

            migrationBuilder.DropColumn(
                name: "AttemptCount",
                schema: "notifications",
                table: "messages");

            migrationBuilder.DropColumn(
                name: "LastAttemptAtUtc",
                schema: "notifications",
                table: "messages");

            migrationBuilder.DropColumn(
                name: "ProviderMessageId",
                schema: "notifications",
                table: "messages");

            migrationBuilder.DropColumn(
                name: "ProviderName",
                schema: "notifications",
                table: "messages");
        }
    }
}
