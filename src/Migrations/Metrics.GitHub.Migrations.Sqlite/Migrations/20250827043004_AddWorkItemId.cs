using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Metrics.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkItemId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "changed_files",
                table: "metrics",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "work_item_id",
                table: "metrics",
                type: "TEXT",
                maxLength: 20,
                nullable: true,
                collation: "NOCASE");

            migrationBuilder.AddColumn<string>(
                name: "work_item_id2",
                table: "metrics",
                type: "TEXT",
                maxLength: 20,
                nullable: true,
                collation: "NOCASE");

            migrationBuilder.CreateIndex(
                name: "ix_metrics_work_item_id",
                table: "metrics",
                column: "work_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_metrics_work_item_id2",
                table: "metrics",
                column: "work_item_id2");

            migrationBuilder.CreateTable(
                name: "datamigrationhistory",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    migration_id = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    migration_type = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    applied_on = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_datamigrationhistory", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_datamigrationhistory_migration_id_migration_type",
                table: "datamigrationhistory",
                columns: new[] { "migration_id", "migration_type" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
             migrationBuilder.DropIndex(
                name: "ix_metrics_work_item_id",
                table: "metrics");

            migrationBuilder.DropIndex(
                name: "ix_metrics_work_item_id2",
                table: "metrics");

            migrationBuilder.DropColumn(
                name: "changed_files",
                table: "metrics");

            migrationBuilder.DropColumn(
                name: "work_item_id",
                table: "metrics");

            migrationBuilder.DropColumn(
                name: "work_item_id2",
                table: "metrics");
                
            migrationBuilder.DropTable(
                name: "datamigrationhistory");
        }
    }
}
