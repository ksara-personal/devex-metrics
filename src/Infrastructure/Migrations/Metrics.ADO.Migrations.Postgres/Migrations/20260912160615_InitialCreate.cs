using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Metrics.Extensions.ADO.Migrations
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
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    migration_id = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    migration_type = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    applied_on = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_datamigrationhistory_ado", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "workitem_metrics",
                columns: table => new
                {
                    work_item_id = table.Column<int>(type: "integer", nullable: false),
                    total_prs = table.Column<int>(type: "integer", nullable: true),
                    team = table.Column<string>(type: "text", nullable: true),
                    release_version = table.Column<string>(type: "text", nullable: true),
                    shirt_size = table.Column<string>(type: "text", nullable: true),
                    loc = table.Column<int>(type: "integer", nullable: false),
                    contributors = table.Column<int>(type: "integer", nullable: false),
                    commits = table.Column<int>(type: "integer", nullable: false),
                    review_comments = table.Column<int>(type: "integer", nullable: false),
                    changes_requested = table.Column<int>(type: "integer", nullable: false),
                    maturity_percentage = table.Column<float>(type: "real", nullable: false),
                    pr_cycle_time = table.Column<TimeSpan>(type: "interval", nullable: false),
                    created_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    closed_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    tenant_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_workitem_metrics", x => x.work_item_id);
                });

            migrationBuilder.CreateTable(
                name: "workitem_sync_status",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    work_item_id = table.Column<int>(type: "integer", nullable: true),
                    last_sync_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    tenant_id = table.Column<string>(type: "text", nullable: false)
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
