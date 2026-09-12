using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Metrics.ADO.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "datamigrationhistory_ado",
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
                    table.PrimaryKey("pk_datamigrationhistory_ado", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "workitem_metrics",
                columns: table => new
                {
                    work_item_id = table.Column<int>(type: "INTEGER", nullable: false),
                    total_prs = table.Column<int>(type: "INTEGER", nullable: true),
                    team = table.Column<string>(type: "TEXT", nullable: true),
                    release_version = table.Column<string>(type: "TEXT", nullable: true),
                    shirt_size = table.Column<string>(type: "TEXT", nullable: true),
                    loc = table.Column<int>(type: "INTEGER", nullable: false),
                    contributors = table.Column<int>(type: "INTEGER", nullable: false),
                    commits = table.Column<int>(type: "INTEGER", nullable: false),
                    review_comments = table.Column<int>(type: "INTEGER", nullable: false),
                    changes_requested = table.Column<int>(type: "INTEGER", nullable: false),
                    maturity_percentage = table.Column<float>(type: "REAL", nullable: false),
                    pr_cycle_time = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    created_date = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    closed_date = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_workitem_metrics", x => x.work_item_id);
                });

            migrationBuilder.CreateTable(
                name: "workitem_sync_status",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    work_item_id = table.Column<int>(type: "INTEGER", nullable: true),
                    last_sync_date = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_workitem_sync_status", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_workitem_metrics_team",
                table: "workitem_metrics",
                column: "team");

            migrationBuilder.CreateIndex(
                name: "ix_workitem_metrics_team_created_date_closed_date",
                table: "workitem_metrics",
                columns: new[] { "team", "created_date", "closed_date" });

            migrationBuilder.CreateIndex(
                name: "ix_workitem_sync_status_work_item_id",
                table: "workitem_sync_status",
                column: "work_item_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "datamigrationhistory_ado");

            migrationBuilder.DropTable(
                name: "workitem_metrics");

            migrationBuilder.DropTable(
                name: "workitem_sync_status");
        }
    }
}
