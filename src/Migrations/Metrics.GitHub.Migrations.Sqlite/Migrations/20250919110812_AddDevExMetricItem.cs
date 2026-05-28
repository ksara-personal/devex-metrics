using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Metrics.Migrations
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
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    approve_time = table.Column<TimeSpan>(type: "TEXT", nullable: true),
                    total_review_changes_requested = table.Column<int>(type: "INTEGER", nullable: true),
                    code_excellence_requested_changes = table.Column<int>(type: "INTEGER", nullable: true),
                    team_requested_changes = table.Column<int>(type: "INTEGER", nullable: true),
                    others_requested_changes = table.Column<int>(type: "INTEGER", nullable: true),
                    coding_time = table.Column<TimeSpan>(type: "TEXT", nullable: true),
                    total_small_commits = table.Column<int>(type: "INTEGER", nullable: true),
                    total_small_commit_comments = table.Column<int>(type: "INTEGER", nullable: true),
                    total_medium_commits = table.Column<int>(type: "INTEGER", nullable: true),
                    total_medium_commit_comments = table.Column<int>(type: "INTEGER", nullable: true),
                    total_large_commits = table.Column<int>(type: "INTEGER", nullable: true),
                    total_large_commit_comments = table.Column<int>(type: "INTEGER", nullable: true),
                    cycle_time = table.Column<TimeSpan>(type: "TEXT", nullable: true),
                    lead_time = table.Column<TimeSpan>(type: "TEXT", nullable: true),
                    maturity_percentage = table.Column<float>(type: "REAL", nullable: true),
                    initial_lines_changed = table.Column<int>(type: "INTEGER", nullable: true),
                    subsequent_lines_changed = table.Column<int>(type: "INTEGER", nullable: true),
                    merge_time = table.Column<TimeSpan>(type: "TEXT", nullable: true),
                    pickup_time = table.Column<TimeSpan>(type: "TEXT", nullable: true),
                    total_review_comments_after_final_approval = table.Column<int>(type: "INTEGER", nullable: true),
                    review_time = table.Column<TimeSpan>(type: "TEXT", nullable: true),
                    pr_size = table.Column<string>(type: "TEXT", nullable: true),
                    avg_review_comments_per_commit = table.Column<float>(type: "REAL", nullable: true),
                    total_review_comments = table.Column<int>(type: "INTEGER", nullable: true),
                    pr_metric_id = table.Column<int>(type: "INTEGER", nullable: false)
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
