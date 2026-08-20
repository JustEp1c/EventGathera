using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventGathera.Events.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ProcessedMesssage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "processed_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    message_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    message_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_processed_messages", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_processed_messages_message_id_message_type",
                table: "processed_messages",
                columns: new[] { "message_id", "message_type" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "processed_messages");
        }
    }
}
