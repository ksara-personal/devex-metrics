using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Metrics.Migrations.DevExMetricPostgresDb
{
    /// <inheritdoc />
    public partial class AggregatedReviewerMetrics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sprint");

            migrationBuilder.CreateTable(
                name: "pr_reviewer_metrics",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    pr_metric_id = table.Column<int>(type: "integer", nullable: false),
                    reviews_requested = table.Column<int>(type: "integer", nullable: false),
                    reviews_submitted = table.Column<int>(type: "integer", nullable: false),
                    average_response_time_hours = table.Column<float>(type: "real", nullable: true),
                    comment_count = table.Column<int>(type: "integer", nullable: false),
                    changes_requested = table.Column<int>(type: "integer", nullable: false),
                    approved = table.Column<int>(type: "integer", nullable: false),
                    review_comments = table.Column<int>(type: "integer", nullable: false),
                    reviewer = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pr_reviewer_metrics", x => x.id);
                    table.ForeignKey(
                        name: "fk_pr_reviewer_metrics_metrics_pr_metric_id",
                        column: x => x.pr_metric_id,
                        principalTable: "metrics",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "reviewer_monthly_metrics",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    month = table.Column<int>(type: "integer", nullable: false),
                    year = table.Column<int>(type: "integer", nullable: false),
                    reviews_requested = table.Column<int>(type: "integer", nullable: false),
                    reviews_submitted = table.Column<int>(type: "integer", nullable: false),
                    average_response_time_hours = table.Column<float>(type: "real", nullable: true),
                    comment_count = table.Column<int>(type: "integer", nullable: false),
                    changes_requested = table.Column<int>(type: "integer", nullable: false),
                    approved = table.Column<int>(type: "integer", nullable: false),
                    review_comments = table.Column<int>(type: "integer", nullable: false),
                    reviewer = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    repository = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    total_review_days = table.Column<int>(type: "integer", nullable: false),
                    prs_requested = table.Column<int>(type: "integer", nullable: false),
                    prs_reviewed = table.Column<int>(type: "integer", nullable: false),
                    prs_for_approval_rate = table.Column<int>(type: "integer", nullable: false),
                    prs_reviewed_with_comments = table.Column<int>(type: "integer", nullable: false),
                    prs_commented = table.Column<int>(type: "integer", nullable: false),
                    comment_count_when_reviewed = table.Column<int>(type: "integer", nullable: false),
                    approval_rate_percentage = table.Column<int>(type: "integer", nullable: false),
                    average_comment_count = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reviewer_monthly_metrics", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "reviewer_sprint_metrics",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    sprint_number = table.Column<int>(type: "integer", nullable: false),
                    year = table.Column<int>(type: "integer", nullable: false),
                    reviews_requested = table.Column<int>(type: "integer", nullable: false),
                    reviews_submitted = table.Column<int>(type: "integer", nullable: false),
                    average_response_time_hours = table.Column<float>(type: "real", nullable: true),
                    comment_count = table.Column<int>(type: "integer", nullable: false),
                    changes_requested = table.Column<int>(type: "integer", nullable: false),
                    approved = table.Column<int>(type: "integer", nullable: false),
                    review_comments = table.Column<int>(type: "integer", nullable: false),
                    reviewer = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    repository = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    total_review_days = table.Column<int>(type: "integer", nullable: false),
                    prs_requested = table.Column<int>(type: "integer", nullable: false),
                    prs_reviewed = table.Column<int>(type: "integer", nullable: false),
                    prs_for_approval_rate = table.Column<int>(type: "integer", nullable: false),
                    prs_reviewed_with_comments = table.Column<int>(type: "integer", nullable: false),
                    prs_commented = table.Column<int>(type: "integer", nullable: false),
                    comment_count_when_reviewed = table.Column<int>(type: "integer", nullable: false),
                    approval_rate_percentage = table.Column<int>(type: "integer", nullable: false),
                    average_comment_count = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reviewer_sprint_metrics", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_pr_reviewer_metrics_pr_metric_id_reviewer",
                table: "pr_reviewer_metrics",
                columns: new[] { "pr_metric_id", "reviewer" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_reviewer_monthly_metrics_reviewer_repository_month_year",
                table: "reviewer_monthly_metrics",
                columns: new[] { "reviewer", "repository", "month", "year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_reviewer_sprint_metrics_reviewer_repository_sprint_number_y",
                table: "reviewer_sprint_metrics",
                columns: new[] { "reviewer", "repository", "sprint_number", "year" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pr_reviewer_metrics");

            migrationBuilder.DropTable(
                name: "reviewer_monthly_metrics");

            migrationBuilder.DropTable(
                name: "reviewer_sprint_metrics");

            migrationBuilder.CreateTable(
                name: "sprint",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    end_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    release_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    sprint_number = table.Column<int>(type: "integer", nullable: false),
                    start_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    year = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sprint", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_sprint_release_number",
                table: "sprint",
                column: "release_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sprint_sprint_number_year",
                table: "sprint",
                columns: new[] { "sprint_number", "year" },
                unique: true);
        }
    }
}
