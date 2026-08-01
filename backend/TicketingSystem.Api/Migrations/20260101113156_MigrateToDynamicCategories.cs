using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TicketingSystem.Api.Migrations
{
    /// <inheritdoc />
    public partial class MigrateToDynamicCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tickets_Category_Priority",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Tickets");

            migrationBuilder.AlterColumn<int>(
                name: "CategoryId",
                table: "Users",
                type: "integer",
                nullable: true,
                oldClrType: typeof(byte),
                oldType: "smallint",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "Tickets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    CategoryId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NamePl = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NameEn = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.CategoryId);
                });

            // Seed initial categories with specific IDs matching old enum values
            // Old enum: IT = 1, Logistics = 2, Administration = 3
            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "CategoryId", "NamePl", "NameEn", "IsActive", "CreatedAt" },
                values: new object[,]
                {
                    { 1, "IT", "IT", true, DateTime.UtcNow },
                    { 2, "Logistyka", "Logistics", true, DateTime.UtcNow },
                    { 3, "Administracja", "Administration", true, DateTime.UtcNow }
                });

            // Set sequence to start after seeded values
            migrationBuilder.Sql("SELECT setval(pg_get_serial_sequence('\"Categories\"', 'CategoryId'), 3, true);");

            // Update existing tickets to have valid CategoryId (default to IT category)
            // This is required before adding FK constraint
            migrationBuilder.Sql("UPDATE \"Tickets\" SET \"CategoryId\" = 1 WHERE \"CategoryId\" = 0;");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_CategoryId_Priority",
                table: "Tickets",
                columns: new[] { "CategoryId", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_IsActive",
                table: "Categories",
                column: "IsActive");

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Categories_CategoryId",
                table: "Tickets",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "CategoryId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Categories_CategoryId",
                table: "Users",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "CategoryId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_Categories_CategoryId",
                table: "Tickets");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Categories_CategoryId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_CategoryId_Priority",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Tickets");

            migrationBuilder.AlterColumn<byte>(
                name: "CategoryId",
                table: "Users",
                type: "smallint",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "Category",
                table: "Tickets",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_Category_Priority",
                table: "Tickets",
                columns: new[] { "Category", "Priority" });
        }
    }
}
