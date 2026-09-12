using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Metrics.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewerDailyMetrics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "reviewer_daily_metrics",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    pr_reviewer_id = table.Column<int>(type: "INTEGER", nullable: false),
                    repository = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    metrics_date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    reviews_requested_on = table.Column<int>(type: "INTEGER", nullable: false),
                    reviews_submitted = table.Column<int>(type: "INTEGER", nullable: false),
                    requested_on = table.Column<int>(type: "INTEGER", nullable: false),
                    reviewed = table.Column<int>(type: "INTEGER", nullable: false),
                    approval_rate = table.Column<float>(type: "REAL", nullable: true),
                    average_response_time_hours = table.Column<float>(type: "REAL", nullable: true),
                    comment_count = table.Column<int>(type: "INTEGER", nullable: false),
                    commented_on = table.Column<int>(type: "INTEGER", nullable: false),
                    comments_count_on_reviewed_p_rs = table.Column<int>(type: "INTEGER", nullable: false),
                    average_comment_count_on_reviewed_p_rs = table.Column<float>(type: "REAL", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reviewer_daily_metrics", x => x.id);
                    table.ForeignKey(
                        name: "fk_reviewer_daily_metrics_reviewer_pr_reviewer_id",
                        column: x => x.pr_reviewer_id,
                        principalTable: "reviewer",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_reviewer_daily_metrics_pr_reviewer_id_metrics_date_repository",
                table: "reviewer_daily_metrics",
                columns: new[] { "pr_reviewer_id", "metrics_date", "repository" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "reviewer_daily_metrics");
        }
    }
}
