using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LittleHelper.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCycleLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CycleLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    LoggedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CycleLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CycleLogs_UserId_StartDate",
                table: "CycleLogs",
                columns: new[] { "UserId", "StartDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CycleLogs");
        }
    }
}
