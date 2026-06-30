using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DropUz.Common.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogImportLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "import_logs",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourcePlatform = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SourceProductId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ProviderName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CatalogProductId = table.Column<Guid>(type: "uuid", nullable: true),
                    Operation = table.Column<int>(type: "integer", nullable: true),
                    ErrorCode = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_import_logs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_import_logs_CatalogProductId",
                schema: "catalog",
                table: "import_logs",
                column: "CatalogProductId");

            migrationBuilder.CreateIndex(
                name: "IX_import_logs_SourcePlatform_SourceProductId_CompletedAtUtc",
                schema: "catalog",
                table: "import_logs",
                columns: new[] { "SourcePlatform", "SourceProductId", "CompletedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_import_logs_Status_CompletedAtUtc",
                schema: "catalog",
                table: "import_logs",
                columns: new[] { "Status", "CompletedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "import_logs",
                schema: "catalog");
        }
    }
}
