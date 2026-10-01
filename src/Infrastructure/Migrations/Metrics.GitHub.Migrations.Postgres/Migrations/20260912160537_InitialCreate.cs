using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Metrics.Migrations.DevExMetricPostgresDb
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:citext", ",,");

            migrationBuilder.CreateTable(
                name: "datamigrationhistory",
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
                    table.PrimaryKey("pk_datamigrationhistory", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "reviewer_monthly_metrics",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    month = table.Column<int>(type: "integer", nullable: false),
                    year = table.Column<int>(type: "integer", nullable: false),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    reviews_requested = table.Column<int>(type: "integer", nullable: true),
                    reviews_submitted = table.Column<int>(type: "integer", nullable: true),
                    average_response_time_hours = table.Column<float>(type: "real", nullable: true),
                    comment_count = table.Column<int>(type: "integer", nullable: true),
                    changes_requested = table.Column<int>(type: "integer", nullable: true),
                    approved = table.Column<int>(type: "integer", nullable: true),
                    review_comments = table.Column<int>(type: "integer", nullable: true),
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
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    reviews_requested = table.Column<int>(type: "integer", nullable: true),
                    reviews_submitted = table.Column<int>(type: "integer", nullable: true),
                    average_response_time_hours = table.Column<float>(type: "real", nullable: true),
                    comment_count = table.Column<int>(type: "integer", nullable: true),
                    changes_requested = table.Column<int>(type: "integer", nullable: true),
                    approved = table.Column<int>(type: "integer", nullable: true),
                    review_comments = table.Column<int>(type: "integer", nullable: true),
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

            migrationBuilder.CreateTable(
                name: "runstatus",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    last_run_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    last_error = table.Column<string>(type: "text", nullable: true),
                    pr_number = table.Column<int>(type: "integer", nullable: true),
                    repository = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    tenant_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_runstatus", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "team",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "citext", maxLength: 50, nullable: false),
                    region = table.Column<string>(type: "citext", maxLength: 50, nullable: true),
                    value_stream = table.Column<string>(type: "citext", maxLength: 50, nullable: true),
                    tenant_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_team", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "metrics",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    pr_number = table.Column<int>(type: "integer", nullable: false),
                    author = table.Column<string>(type: "citext", maxLength: 100, nullable: false),
                    merged_by = table.Column<string>(type: "citext", maxLength: 100, nullable: true),
                    repository = table.Column<string>(type: "citext", maxLength: 100, nullable: false),
                    base_branch = table.Column<string>(type: "citext", maxLength: 150, nullable: false),
                    team_id = table.Column<int>(type: "integer", nullable: false),
                    state = table.Column<string>(type: "citext", maxLength: 20, nullable: false),
                    merged_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    total_comments = table.Column<int>(type: "integer", nullable: false),
                    total_lines = table.Column<int>(type: "integer", nullable: false),
                    changed_files = table.Column<int>(type: "integer", nullable: true),
                    work_item_id = table.Column<string>(type: "citext", maxLength: 20, nullable: true),
                    work_item_id2 = table.Column<string>(type: "citext", maxLength: 20, nullable: true),
                    is_feature = table.Column<bool>(type: "boolean", nullable: true),
                    draft_transitions = table.Column<int>(type: "integer", nullable: false),
                    total_commits = table.Column<int>(type: "integer", nullable: false),
                    tenant_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_metrics", x => x.id);
                    table.ForeignKey(
                        name: "fk_metrics_team_team_id",
                        column: x => x.team_id,
                        principalTable: "team",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "contributor",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    contributor = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    loc = table.Column<int>(type: "integer", nullable: false),
                    commits = table.Column<int>(type: "integer", nullable: false),
                    pr_metric_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contributor", x => x.id);
                    table.ForeignKey(
                        name: "fk_contributor_metrics_pr_metric_id",
                        column: x => x.pr_metric_id,
                        principalTable: "metrics",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "copilotreviewmetrics",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    files_reviewed = table.Column<int>(type: "integer", nullable: false),
                    files_changed = table.Column<int>(type: "integer", nullable: false),
                    comments = table.Column<int>(type: "integer", nullable: false),
                    pr_metric_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_copilotreviewmetrics", x => x.id);
                    table.ForeignKey(
                        name: "fk_copilotreviewmetrics_metrics_pr_metric_id",
                        column: x => x.pr_metric_id,
                        principalTable: "metrics",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

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

            migrationBuilder.CreateTable(
                name: "pr_reviewer_metrics",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    pr_metric_id = table.Column<int>(type: "integer", nullable: false),
                    reviews_requested = table.Column<int>(type: "integer", nullable: true),
                    reviews_submitted = table.Column<int>(type: "integer", nullable: true),
                    average_response_time_hours = table.Column<float>(type: "real", nullable: true),
                    comment_count = table.Column<int>(type: "integer", nullable: true),
                    changes_requested = table.Column<int>(type: "integer", nullable: true),
                    approved = table.Column<int>(type: "integer", nullable: true),
                    review_comments = table.Column<int>(type: "integer", nullable: true),
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
                name: "reviewer_daily_metrics",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    reviews_requested = table.Column<int>(type: "integer", nullable: true),
                    reviews_submitted = table.Column<int>(type: "integer", nullable: true),
                    average_response_time_hours = table.Column<float>(type: "real", nullable: true),
                    comment_count = table.Column<int>(type: "integer", nullable: true),
                    changes_requested = table.Column<int>(type: "integer", nullable: true),
                    approved = table.Column<int>(type: "integer", nullable: true),
                    review_comments = table.Column<int>(type: "integer", nullable: true),
                    reviewer = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    pr_metric_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reviewer_daily_metrics", x => x.id);
                    table.ForeignKey(
                        name: "fk_reviewer_daily_metrics_metrics_pr_metric_id",
                        column: x => x.pr_metric_id,
                        principalTable: "metrics",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_contributor_pr_metric_id",
                table: "contributor",
                column: "pr_metric_id");

            migrationBuilder.CreateIndex(
                name: "ix_copilotreviewmetrics_pr_metric_id",
                table: "copilotreviewmetrics",
                column: "pr_metric_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_datamigrationhistory_tenant_id_migration_id_migration_type",
                table: "datamigrationhistory",
                columns: new[] { "tenant_id", "migration_id", "migration_type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_devexmetricitem_pr_metric_id",
                table: "devexmetricitem",
                column: "pr_metric_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_metrics_author",
                table: "metrics",
                column: "author");

            migrationBuilder.CreateIndex(
                name: "ix_metrics_created_at_author",
                table: "metrics",
                columns: new[] { "created_at", "author" });

            migrationBuilder.CreateIndex(
                name: "ix_metrics_created_at_team_id",
                table: "metrics",
                columns: new[] { "created_at", "team_id" });

            migrationBuilder.CreateIndex(
                name: "ix_metrics_pr_number",
                table: "metrics",
                column: "pr_number");

            migrationBuilder.CreateIndex(
                name: "ix_metrics_repository",
                table: "metrics",
                column: "repository");

            migrationBuilder.CreateIndex(
                name: "ix_metrics_state",
                table: "metrics",
                column: "state");

            migrationBuilder.CreateIndex(
                name: "ix_metrics_team_id",
                table: "metrics",
                column: "team_id");

            migrationBuilder.CreateIndex(
                name: "ix_metrics_work_item_id",
                table: "metrics",
                column: "work_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_metrics_work_item_id2",
                table: "metrics",
                column: "work_item_id2");

            migrationBuilder.CreateIndex(
                name: "ix_pr_reviewer_metrics_pr_metric_id_reviewer",
                table: "pr_reviewer_metrics",
                columns: new[] { "pr_metric_id", "reviewer" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_reviewer_daily_metrics_pr_metric_id_reviewer_date",
                table: "reviewer_daily_metrics",
                columns: new[] { "pr_metric_id", "reviewer", "date" },
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

            migrationBuilder.CreateIndex(
                name: "ix_runstatus_repository_pr_number",
                table: "runstatus",
                columns: new[] { "repository", "pr_number" });

            migrationBuilder.CreateIndex(
                name: "ix_team_name",
                table: "team",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_team_name_value_stream_region",
                table: "team",
                columns: new[] { "name", "value_stream", "region" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "contributor");

            migrationBuilder.DropTable(
                name: "copilotreviewmetrics");

            migrationBuilder.DropTable(
                name: "datamigrationhistory");

            migrationBuilder.DropTable(
                name: "devexmetricitem");

            migrationBuilder.DropTable(
                name: "pr_reviewer_metrics");

            migrationBuilder.DropTable(
                name: "reviewer_daily_metrics");

            migrationBuilder.DropTable(
                name: "reviewer_monthly_metrics");

            migrationBuilder.DropTable(
                name: "reviewer_sprint_metrics");

            migrationBuilder.DropTable(
                name: "runstatus");

            migrationBuilder.DropTable(
                name: "metrics");

            migrationBuilder.DropTable(
                name: "team");
        }
    }
}
