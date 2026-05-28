using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Metrics.Migrations.DevExMetricPostgresDb
{
    /// <inheritdoc />
    public partial class AddDevExMetricItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "devexmetricitem",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    approve_time = table.Column<TimeSpan>(type: "interval", nullable: true),
                    total_review_changes_requested = table.Column<int>(type: "integer", nullable: true),
                    code_excellence_requested_changes = table.Column<int>(type: "integer", nullable: true),
                    team_requested_changes = table.Column<int>(type: "integer", nullable: true),
                    others_requested_changes = table.Column<int>(type: "integer", nullable: true),
                    coding_time = table.Column<TimeSpan>(type: "interval", nullable: true),
                    total_small_commits = table.Column<int>(type: "integer", nullable: true),
                    total_small_commit_comments = table.Column<int>(type: "integer", nullable: true),
                    total_medium_commits = table.Column<int>(type: "integer", nullable: true),
                    total_medium_commit_comments = table.Column<int>(type: "integer", nullable: true),
                    total_large_commits = table.Column<int>(type: "integer", nullable: true),
                    total_large_commit_comments = table.Column<int>(type: "integer", nullable: true),
                    cycle_time = table.Column<TimeSpan>(type: "interval", nullable: true),
                    lead_time = table.Column<TimeSpan>(type: "interval", nullable: true),
                    maturity_percentage = table.Column<float>(type: "real", nullable: true),
                    initial_lines_changed = table.Column<int>(type: "integer", nullable: true),
                    subsequent_lines_changed = table.Column<int>(type: "integer", nullable: true),
                    merge_time = table.Column<TimeSpan>(type: "interval", nullable: true),
                    pickup_time = table.Column<TimeSpan>(type: "interval", nullable: true),
                    total_review_comments_after_final_approval = table.Column<int>(type: "integer", nullable: true),
                    review_time = table.Column<TimeSpan>(type: "interval", nullable: true),
                    pr_size = table.Column<string>(type: "text", nullable: true),
                    avg_review_comments_per_commit = table.Column<float>(type: "real", nullable: true),
                    total_review_comments = table.Column<int>(type: "integer", nullable: true),
                    pr_metric_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_devexmetricitem", x => x.id);
                    table.ForeignKey(
                        name: "fk_devexmetricitem_metrics_pr_metric_id",
                        column: x => x.pr_metric_id,
                        principalTable: "metrics",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_devexmetricitem_pr_metric_id",
                table: "devexmetricitem",
                column: "pr_metric_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "devexmetricitem");
        }
    }
}
