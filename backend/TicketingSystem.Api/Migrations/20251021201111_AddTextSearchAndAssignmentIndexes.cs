using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketingSystem.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTextSearchAndAssignmentIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Trigram extension for fast ILIKE '%term%'
            migrationBuilder.Sql(@"CREATE EXTENSION IF NOT EXISTS pg_trgm;");

            // Trigram indexes for free-text search on Title & Description
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ix_tickets_title_trgm
                ON ""Tickets"" USING gin (""Title"" gin_trgm_ops);
            ");

            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ix_tickets_description_trgm
                ON ""Tickets"" USING gin (""Description"" gin_trgm_ops);
            ");

            // 3) Assignment helper (TeamLeader by category)
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ix_users_category_role_userid
                ON ""Users"" (""CategoryId"", ""RoleId"", ""UserId"");
            ");

            // 4) Optional (for “least open” strategy later)
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ix_tickets_assigned_status
                ON ""Tickets"" (""AssignedToId"", ""Status"");
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ix_tickets_title_trgm;");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ix_tickets_description_trgm;");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ix_users_category_role_userid;");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ix_tickets_assigned_status;");
        }
    }
}
