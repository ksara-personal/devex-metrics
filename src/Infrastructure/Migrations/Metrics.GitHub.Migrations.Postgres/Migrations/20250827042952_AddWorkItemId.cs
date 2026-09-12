using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Metrics.Migrations.DevExMetricPostgresDb
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
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "work_item_id",
                table: "metrics",
                type: "citext",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "work_item_id2",
                table: "metrics",
                type: "citext",
                maxLength: 20,
                nullable: true);

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
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    migration_id = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    migration_type = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    applied_on = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
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
