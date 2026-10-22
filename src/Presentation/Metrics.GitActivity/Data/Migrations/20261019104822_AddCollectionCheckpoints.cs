using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Metrics.GitActivity.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCollectionCheckpoints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Resumed",
                table: "CollectionRuns",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "CollectionCheckpoints",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Repository = table.Column<string>(type: "TEXT", nullable: false),
                    Phase = table.Column<string>(type: "TEXT", nullable: false),
                    WindowStart = table.Column<DateTime>(type: "TEXT", nullable: false),
                    WindowEnd = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Branch = table.Column<string>(type: "TEXT", nullable: true),
                    Position = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Cursor = table.Column<string>(type: "TEXT", nullable: true),
                    LastItemAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastItemKey = table.Column<string>(type: "TEXT", nullable: true),
                    ItemsStored = table.Column<int>(type: "INTEGER", nullable: false),
                    IsComplete = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastError = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectionCheckpoints", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CollectionCheckpoints_Repository_Phase_WindowStart_WindowEnd",
                table: "CollectionCheckpoints",
                columns: new[] { "Repository", "Phase", "WindowStart", "WindowEnd" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CollectionCheckpoints");

            migrationBuilder.DropColumn(
                name: "Resumed",
                table: "CollectionRuns");
        }
    }
}
