using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketingSystem.Api.Migrations
{
    /// <inheritdoc />
    public partial class Sync_Model_After_Comments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<byte>(
                name: "Status",
                table: "Tickets",
                type: "smallint",
                nullable: false,
                oldClrType: typeof(byte),
                oldType: "smallint",
                oldDefaultValue: (byte)1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<byte>(
                name: "Status",
                table: "Tickets",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)1,
                oldClrType: typeof(byte),
                oldType: "smallint");
        }
    }
}
