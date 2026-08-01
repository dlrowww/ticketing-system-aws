using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TicketingSystem.Api.Migrations
{
    /// <inheritdoc />
    public partial class TicketFiles_Add : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TicketFiles",
                columns: table => new
                {
                    TicketFileId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TicketId = table.Column<int>(type: "integer", nullable: false),
                    OriginalName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    StoredName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UploaderUserId = table.Column<int>(type: "integer", nullable: false),
                    StoragePath = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ChecksumSha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketFiles", x => x.TicketFileId);
                    table.ForeignKey(
                        name: "FK_TicketFiles_Tickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "Tickets",
                        principalColumn: "TicketId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TicketFiles_Users_UploaderUserId",
                        column: x => x.UploaderUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TicketFileContents",
                columns: table => new
                {
                    TicketFileId = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketFileContents", x => x.TicketFileId);
                    table.ForeignKey(
                        name: "FK_TicketFileContents_TicketFiles_TicketFileId",
                        column: x => x.TicketFileId,
                        principalTable: "TicketFiles",
                        principalColumn: "TicketFileId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TicketFiles_TicketId",
                table: "TicketFiles",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketFiles_UploaderUserId",
                table: "TicketFiles",
                column: "UploaderUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TicketFileContents");

            migrationBuilder.DropTable(
                name: "TicketFiles");
        }
    }
}
