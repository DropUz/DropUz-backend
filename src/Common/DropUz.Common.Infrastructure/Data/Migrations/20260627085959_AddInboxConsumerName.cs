using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DropUz.Common.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInboxConsumerName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_inbox_messages",
                schema: "common",
                table: "inbox_messages");

            migrationBuilder.AddColumn<string>(
                name: "ConsumerName",
                schema: "common",
                table: "inbox_messages",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "legacy");

            migrationBuilder.Sql(
                "ALTER TABLE common.inbox_messages ALTER COLUMN \"ConsumerName\" DROP DEFAULT;");

            migrationBuilder.AddPrimaryKey(
                name: "PK_inbox_messages",
                schema: "common",
                table: "inbox_messages",
                columns: new[] { "Id", "ConsumerName" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_inbox_messages",
                schema: "common",
                table: "inbox_messages");

            migrationBuilder.Sql(
                """
                DELETE FROM common.inbox_messages AS candidate
                USING common.inbox_messages AS keeper
                WHERE candidate."Id" = keeper."Id"
                  AND candidate."ConsumerName" > keeper."ConsumerName";
                """);

            migrationBuilder.DropColumn(
                name: "ConsumerName",
                schema: "common",
                table: "inbox_messages");

            migrationBuilder.AddPrimaryKey(
                name: "PK_inbox_messages",
                schema: "common",
                table: "inbox_messages",
                column: "Id");
        }
    }
}
