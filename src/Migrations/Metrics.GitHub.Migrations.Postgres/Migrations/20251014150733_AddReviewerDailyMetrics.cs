using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Metrics.Migrations.DevExMetricPostgresDb
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
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    pr_reviewer_id = table.Column<int>(type: "integer", nullable: false),
                    repository = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    metrics_date = table.Column<DateOnly>(type: "date", nullable: false),
                    reviews_requested_on = table.Column<int>(type: "integer", nullable: false),
                    reviews_submitted = table.Column<int>(type: "integer", nullable: false),
                    requested_on = table.Column<int>(type: "integer", nullable: false),
                    reviewed = table.Column<int>(type: "integer", nullable: false),
                    approval_rate = table.Column<float>(type: "real", nullable: true),
                    average_response_time_hours = table.Column<float>(type: "real", nullable: true),
                    comment_count = table.Column<int>(type: "integer", nullable: false),
                    commented_on = table.Column<int>(type: "integer", nullable: false),
                    comments_count_on_reviewed_p_rs = table.Column<int>(type: "integer", nullable: false),
                    average_comment_count_on_reviewed_p_rs = table.Column<float>(type: "real", nullable: true)
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
                name: "ix_reviewer_daily_metrics_pr_reviewer_id_metrics_date_reposito",
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
