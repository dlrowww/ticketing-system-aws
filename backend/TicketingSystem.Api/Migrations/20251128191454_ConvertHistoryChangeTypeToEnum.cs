using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketingSystem.Api.Migrations
{
    /// <inheritdoc />
    public partial class ConvertHistoryChangeTypeToEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // PostgreSQL requires explicit conversion from string to smallint
            // Map string enum names to their numeric values
            migrationBuilder.Sql(@"
                ALTER TABLE ""TicketHistories"" 
                ALTER COLUMN ""ChangeType"" TYPE smallint 
                USING CASE ""ChangeType""
                    WHEN 'TicketCreated' THEN 1
                    WHEN 'StatusChanged' THEN 2
                    WHEN 'PriorityChanged' THEN 3
                    WHEN 'CategoryChanged' THEN 4
                    WHEN 'AssignmentChanged' THEN 5
                    WHEN 'TitleChanged' THEN 6
                    WHEN 'DescriptionChanged' THEN 7
                    ELSE 0
                END;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ChangeType",
                table: "TicketHistories",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(byte),
                oldType: "smallint",
                oldMaxLength: 50);
        }
    }
}
