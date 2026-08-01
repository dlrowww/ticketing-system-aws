using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TicketingSystem.Api.Migrations
{
    /// <inheritdoc />
    public partial class Add_TicketComments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TicketComments",
                columns: table => new
                {
                    CommentId   = table.Column<int>(type: "integer", nullable: false)
                                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TicketId    = table.Column<int>(type: "integer", nullable: false),
                    Content     = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    // add DB-side default for CreatedAt
                    CreatedAt   = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    CreatedById = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketComments", x => x.CommentId);

                    // FK -> Tickets, cascade on ticket delete
                    table.ForeignKey(
                        name: "FK_TicketComments_Tickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "Tickets",
                        principalColumn: "TicketId",
                        onDelete: ReferentialAction.Cascade);

                    // FK -> Users, keep comments even if user is removed (Restrict)
                    table.ForeignKey(
                        name: "FK_TicketComments_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TicketComments_TicketId",
                table: "TicketComments",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketComments_TicketId_CreatedAt",
                table: "TicketComments",
                columns: new[] { "TicketId", "CreatedAt" });

            // (optional but useful) index for querying by author
            migrationBuilder.CreateIndex(
                name: "IX_TicketComments_CreatedById",
                table: "TicketComments",
                column: "CreatedById");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "TicketComments");
        }
    }
}
