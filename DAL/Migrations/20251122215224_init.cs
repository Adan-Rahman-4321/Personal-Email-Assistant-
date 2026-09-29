using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ColorCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Emails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Subject = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SenderName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SenderEmail = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    ReceivedTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    EmailProvider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ExternalEmailId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Emails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Emails_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "ColorCode", "CreatedAt", "Description", "DisplayName", "Name", "Priority" },
                values: new object[,]
                {
                    { 1, "#dc3545", new DateTime(2025, 11, 22, 21, 52, 21, 802, DateTimeKind.Utc).AddTicks(3289), "Urgent and critical emails requiring immediate attention", "🔥 Most Important", "Most Important", 1 },
                    { 2, "#ffc107", new DateTime(2025, 11, 22, 21, 52, 21, 802, DateTimeKind.Utc).AddTicks(3292), "Important emails that need attention but are not urgent", "⭐ Important", "Important", 2 },
                    { 3, "#0d6efd", new DateTime(2025, 11, 22, 21, 52, 21, 802, DateTimeKind.Utc).AddTicks(3293), "Regular casual emails", "🙂 Casual", "Casual", 3 },
                    { 4, "#6c757d", new DateTime(2025, 11, 22, 21, 52, 21, 802, DateTimeKind.Utc).AddTicks(3295), "Promotional and marketing emails", "🏷️ Promotional", "Promotional", 4 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Name",
                table: "Categories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Priority",
                table: "Categories",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_Emails_CategoryId",
                table: "Emails",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Emails_ExternalEmailId",
                table: "Emails",
                column: "ExternalEmailId",
                unique: true,
                filter: "[ExternalEmailId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Emails_ReceivedTime",
                table: "Emails",
                column: "ReceivedTime");

            migrationBuilder.CreateIndex(
                name: "IX_Emails_SenderEmail",
                table: "Emails",
                column: "SenderEmail");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Emails");

            migrationBuilder.DropTable(
                name: "Categories");
        }
    }
}
