using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Metrics.Migrations.DevExMetricPostgresDb
{
    /// <inheritdoc />
    public partial class ReviewerMetricChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_reviewer_daily_metrics_reviewer_pr_reviewer_id",
                table: "reviewer_daily_metrics");

            migrationBuilder.DropTable(
                name: "reviewermetrics");

            migrationBuilder.DropTable(
                name: "reviewer");

            migrationBuilder.DropIndex(
                name: "ix_reviewer_daily_metrics_pr_reviewer_id_metrics_date_reposito",
                table: "reviewer_daily_metrics");

            migrationBuilder.DropColumn(
                name: "approval_rate",
                table: "reviewer_daily_metrics");

            migrationBuilder.DropColumn(
                name: "average_comment_count_on_reviewed_p_rs",
                table: "reviewer_daily_metrics");

            migrationBuilder.DropColumn(
                name: "commented_on",
                table: "reviewer_daily_metrics");

            migrationBuilder.RenameColumn(
                name: "reviews_requested_on",
                table: "reviewer_daily_metrics",
                newName: "reviews_requested");

            migrationBuilder.RenameColumn(
                name: "reviewed",
                table: "reviewer_daily_metrics",
                newName: "review_comments");

            migrationBuilder.RenameColumn(
                name: "requested_on",
                table: "reviewer_daily_metrics",
                newName: "pr_metric_id");

            migrationBuilder.RenameColumn(
                name: "repository",
                table: "reviewer_daily_metrics",
                newName: "reviewer");

            migrationBuilder.RenameColumn(
                name: "pr_reviewer_id",
                table: "reviewer_daily_metrics",
                newName: "changes_requested");

            migrationBuilder.RenameColumn(
                name: "metrics_date",
                table: "reviewer_daily_metrics",
                newName: "date");

            migrationBuilder.RenameColumn(
                name: "comments_count_on_reviewed_p_rs",
                table: "reviewer_daily_metrics",
                newName: "approved");

            migrationBuilder.AlterColumn<float>(
                name: "average_response_time_hours",
                table: "reviewer_daily_metrics",
                type: "real",
                nullable: false,
                defaultValue: 0f,
                oldClrType: typeof(float),
                oldType: "real",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_reviewer_daily_metrics_pr_metric_id_reviewer_date",
                table: "reviewer_daily_metrics",
                columns: new[] { "pr_metric_id", "reviewer", "date" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_reviewer_daily_metrics_metrics_pr_metric_id",
                table: "reviewer_daily_metrics",
                column: "pr_metric_id",
                principalTable: "metrics",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_reviewer_daily_metrics_metrics_pr_metric_id",
                table: "reviewer_daily_metrics");

            migrationBuilder.DropIndex(
                name: "ix_reviewer_daily_metrics_pr_metric_id_reviewer_date",
                table: "reviewer_daily_metrics");

            migrationBuilder.RenameColumn(
                name: "reviews_requested",
                table: "reviewer_daily_metrics",
                newName: "reviews_requested_on");

            migrationBuilder.RenameColumn(
                name: "reviewer",
                table: "reviewer_daily_metrics",
                newName: "repository");

            migrationBuilder.RenameColumn(
                name: "review_comments",
                table: "reviewer_daily_metrics",
                newName: "reviewed");

            migrationBuilder.RenameColumn(
                name: "pr_metric_id",
                table: "reviewer_daily_metrics",
                newName: "requested_on");

            migrationBuilder.RenameColumn(
                name: "date",
                table: "reviewer_daily_metrics",
                newName: "metrics_date");

            migrationBuilder.RenameColumn(
                name: "changes_requested",
                table: "reviewer_daily_metrics",
                newName: "pr_reviewer_id");

            migrationBuilder.RenameColumn(
                name: "approved",
                table: "reviewer_daily_metrics",
                newName: "comments_count_on_reviewed_p_rs");

            migrationBuilder.AlterColumn<float>(
                name: "average_response_time_hours",
                table: "reviewer_daily_metrics",
                type: "real",
                nullable: true,
                oldClrType: typeof(float),
                oldType: "real");

            migrationBuilder.AddColumn<float>(
                name: "approval_rate",
                table: "reviewer_daily_metrics",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "average_comment_count_on_reviewed_p_rs",
                table: "reviewer_daily_metrics",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "commented_on",
                table: "reviewer_daily_metrics",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "reviewer",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    login = table.Column<string>(type: "citext", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reviewer", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "reviewermetrics",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    pr_metric_id = table.Column<int>(type: "integer", nullable: false),
                    pr_reviewer_id = table.Column<int>(type: "integer", nullable: false),
                    approved = table.Column<int>(type: "integer", nullable: false),
                    changes_requested = table.Column<int>(type: "integer", nullable: false),
                    review_comments = table.Column<int>(type: "integer", nullable: false),
                    reviews = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reviewermetrics", x => x.id);
                    table.ForeignKey(
                        name: "fk_reviewermetrics_metrics_pr_metric_id",
                        column: x => x.pr_metric_id,
                        principalTable: "metrics",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_reviewermetrics_reviewer_pr_reviewer_id",
                        column: x => x.pr_reviewer_id,
                        principalTable: "reviewer",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_reviewer_daily_metrics_pr_reviewer_id_metrics_date_reposito",
                table: "reviewer_daily_metrics",
                columns: new[] { "pr_reviewer_id", "metrics_date", "repository" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_reviewer_login",
                table: "reviewer",
                column: "login",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_reviewermetrics_pr_metric_id",
                table: "reviewermetrics",
                column: "pr_metric_id");

            migrationBuilder.CreateIndex(
                name: "ix_reviewermetrics_pr_reviewer_id",
                table: "reviewermetrics",
                column: "pr_reviewer_id");

            migrationBuilder.AddForeignKey(
                name: "fk_reviewer_daily_metrics_reviewer_pr_reviewer_id",
                table: "reviewer_daily_metrics",
                column: "pr_reviewer_id",
                principalTable: "reviewer",
                principalColumn: "id");
        }
    }
}
