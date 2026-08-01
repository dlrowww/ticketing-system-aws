using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketingSystem.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Note: IX_Tickets_Status and IX_Tickets_CreatedAt already exist from Initial migration, skipping

            // Add composite index on Tickets (Status, CreatedAt) for dashboard queries
            migrationBuilder.CreateIndex(
                name: "IX_Tickets_Status_CreatedAt",
                table: "Tickets",
                columns: new[] { "Status", "CreatedAt" });

            // Add index on TicketComments.CreatedAt for comment sorting
            migrationBuilder.CreateIndex(
                name: "IX_TicketComments_CreatedAt",
                table: "TicketComments",
                column: "CreatedAt");

            // Add index on Users.IsActive for active user filtering
            migrationBuilder.CreateIndex(
                name: "IX_Users_IsActive",
                table: "Users",
                column: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_IsActive",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_TicketComments_CreatedAt",
                table: "TicketComments");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_Status_CreatedAt",
                table: "Tickets");

            // Note: IX_Tickets_Status and IX_Tickets_CreatedAt from Initial migration, not dropping here
        }
    }
}
