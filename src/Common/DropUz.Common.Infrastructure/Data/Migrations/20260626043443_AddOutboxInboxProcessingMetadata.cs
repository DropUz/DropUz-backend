using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DropUz.Common.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboxInboxProcessingMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastAttemptedOnUtc",
                schema: "common",
                table: "outbox_messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RetryCount",
                schema: "common",
                table: "outbox_messages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastAttemptedOnUtc",
                schema: "common",
                table: "inbox_messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RetryCount",
                schema: "common",
                table: "inbox_messages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_ProcessedOnUtc_OccurredOnUtc",
                schema: "common",
                table: "outbox_messages",
                columns: new[] { "ProcessedOnUtc", "OccurredOnUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_inbox_messages_ProcessedOnUtc_OccurredOnUtc",
                schema: "common",
                table: "inbox_messages",
                columns: new[] { "ProcessedOnUtc", "OccurredOnUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_outbox_messages_ProcessedOnUtc_OccurredOnUtc",
                schema: "common",
                table: "outbox_messages");

            migrationBuilder.DropIndex(
                name: "IX_inbox_messages_ProcessedOnUtc_OccurredOnUtc",
                schema: "common",
                table: "inbox_messages");

            migrationBuilder.DropColumn(
                name: "LastAttemptedOnUtc",
                schema: "common",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "RetryCount",
                schema: "common",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "LastAttemptedOnUtc",
                schema: "common",
                table: "inbox_messages");

            migrationBuilder.DropColumn(
                name: "RetryCount",
                schema: "common",
                table: "inbox_messages");
        }
    }
}
