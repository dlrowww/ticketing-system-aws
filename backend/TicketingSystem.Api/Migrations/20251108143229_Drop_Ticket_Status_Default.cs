using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketingSystem.Api.Migrations
{
    /// <inheritdoc />
    public partial class Drop_Ticket_Status_Default : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE ""Tickets"" ALTER COLUMN ""Status"" DROP DEFAULT;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE ""Tickets"" ALTER COLUMN ""Status"" SET DEFAULT 0;");
        }
    }
}
