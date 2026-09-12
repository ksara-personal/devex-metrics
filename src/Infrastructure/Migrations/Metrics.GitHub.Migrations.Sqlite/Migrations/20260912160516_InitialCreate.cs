using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Metrics.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "datamigrationhistory",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    tenant_id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    migration_id = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    migration_type = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    applied_on = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_datamigrationhistory", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "reviewer_monthly_metrics",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    month = table.Column<int>(type: "INTEGER", nullable: false),
                    year = table.Column<int>(type: "INTEGER", nullable: false),
                    tenant_id = table.Column<string>(type: "TEXT", nullable: false),
                    reviews_requested = table.Column<int>(type: "INTEGER", nullable: true),
                    reviews_submitted = table.Column<int>(type: "INTEGER", nullable: true),
                    average_response_time_hours = table.Column<float>(type: "REAL", nullable: true),
                    comment_count = table.Column<int>(type: "INTEGER", nullable: true),
                    changes_requested = table.Column<int>(type: "INTEGER", nullable: true),
                    approved = table.Column<int>(type: "INTEGER", nullable: true),
                    review_comments = table.Column<int>(type: "INTEGER", nullable: true),
                    reviewer = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    repository = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    total_review_days = table.Column<int>(type: "INTEGER", nullable: false),
                    prs_requested = table.Column<int>(type: "INTEGER", nullable: false),
                    prs_reviewed = table.Column<int>(type: "INTEGER", nullable: false),
                    prs_for_approval_rate = table.Column<int>(type: "INTEGER", nullable: false),
                    prs_reviewed_with_comments = table.Column<int>(type: "INTEGER", nullable: false),
                    prs_commented = table.Column<int>(type: "INTEGER", nullable: false),
                    comment_count_when_reviewed = table.Column<int>(type: "INTEGER", nullable: false),
                    approval_rate_percentage = table.Column<int>(type: "INTEGER", nullable: false),
                    average_comment_count = table.Column<float>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reviewer_monthly_metrics", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "reviewer_sprint_metrics",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    sprint_number = table.Column<int>(type: "INTEGER", nullable: false),
                    year = table.Column<int>(type: "INTEGER", nullable: false),
                    tenant_id = table.Column<string>(type: "TEXT", nullable: false),
                    reviews_requested = table.Column<int>(type: "INTEGER", nullable: true),
                    reviews_submitted = table.Column<int>(type: "INTEGER", nullable: true),
                    average_response_time_hours = table.Column<float>(type: "REAL", nullable: true),
                    comment_count = table.Column<int>(type: "INTEGER", nullable: true),
                    changes_requested = table.Column<int>(type: "INTEGER", nullable: true),
                    approved = table.Column<int>(type: "INTEGER", nullable: true),
                    review_comments = table.Column<int>(type: "INTEGER", nullable: true),
                    reviewer = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    repository = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    total_review_days = table.Column<int>(type: "INTEGER", nullable: false),
                    prs_requested = table.Column<int>(type: "INTEGER", nullable: false),
                    prs_reviewed = table.Column<int>(type: "INTEGER", nullable: false),
                    prs_for_approval_rate = table.Column<int>(type: "INTEGER", nullable: false),
                    prs_reviewed_with_comments = table.Column<int>(type: "INTEGER", nullable: false),
                    prs_commented = table.Column<int>(type: "INTEGER", nullable: false),
                    comment_count_when_reviewed = table.Column<int>(type: "INTEGER", nullable: false),
                    approval_rate_percentage = table.Column<int>(type: "INTEGER", nullable: false),
                    average_comment_count = table.Column<float>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reviewer_sprint_metrics", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "runstatus",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    last_run_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    last_error = table.Column<string>(type: "TEXT", nullable: true),
                    pr_number = table.Column<int>(type: "INTEGER", nullable: true),
                    repository = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    tenant_id = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_runstatus", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "team",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false, collation: "NOCASE"),
                    region = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true, collation: "NOCASE"),
                    value_stream = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true, collation: "NOCASE"),
                    tenant_id = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_team", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "metrics",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    pr_number = table.Column<int>(type: "INTEGER", nullable: false),
                    author = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false, collation: "NOCASE"),
                    merged_by = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true, collation: "NOCASE"),
                    repository = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false, collation: "NOCASE"),
                    base_branch = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false, collation: "NOCASE"),
                    team_id = table.Column<int>(type: "INTEGER", nullable: false),
                    state = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false, collation: "NOCASE"),
                    merged_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    total_comments = table.Column<int>(type: "INTEGER", nullable: false),
                    total_lines = table.Column<int>(type: "INTEGER", nullable: false),
                    changed_files = table.Column<int>(type: "INTEGER", nullable: true),
                    work_item_id = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true, collation: "NOCASE"),
                    work_item_id2 = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true, collation: "NOCASE"),
                    is_feature = table.Column<bool>(type: "INTEGER", nullable: true),
                    draft_transitions = table.Column<int>(type: "INTEGER", nullable: false),
                    total_commits = table.Column<int>(type: "INTEGER", nullable: false),
                    tenant_id = table.Column<string>(type: "TEXT", nullable: false)
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
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    contributor = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    loc = table.Column<int>(type: "INTEGER", nullable: false),
                    commits = table.Column<int>(type: "INTEGER", nullable: false),
                    pr_metric_id = table.Column<int>(type: "INTEGER", nullable: false)
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
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    files_reviewed = table.Column<int>(type: "INTEGER", nullable: false),
                    files_changed = table.Column<int>(type: "INTEGER", nullable: false),
                    comments = table.Column<int>(type: "INTEGER", nullable: false),
                    pr_metric_id = table.Column<int>(type: "INTEGER", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "pr_reviewer_metrics",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    pr_metric_id = table.Column<int>(type: "INTEGER", nullable: false),
                    reviews_requested = table.Column<int>(type: "INTEGER", nullable: true),
                    reviews_submitted = table.Column<int>(type: "INTEGER", nullable: true),
                    average_response_time_hours = table.Column<float>(type: "REAL", nullable: true),
                    comment_count = table.Column<int>(type: "INTEGER", nullable: true),
                    changes_requested = table.Column<int>(type: "INTEGER", nullable: true),
                    approved = table.Column<int>(type: "INTEGER", nullable: true),
                    review_comments = table.Column<int>(type: "INTEGER", nullable: true),
                    reviewer = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
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
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    reviews_requested = table.Column<int>(type: "INTEGER", nullable: true),
                    reviews_submitted = table.Column<int>(type: "INTEGER", nullable: true),
                    average_response_time_hours = table.Column<float>(type: "REAL", nullable: true),
                    comment_count = table.Column<int>(type: "INTEGER", nullable: true),
                    changes_requested = table.Column<int>(type: "INTEGER", nullable: true),
                    approved = table.Column<int>(type: "INTEGER", nullable: true),
                    review_comments = table.Column<int>(type: "INTEGER", nullable: true),
                    reviewer = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    pr_metric_id = table.Column<int>(type: "INTEGER", nullable: false)
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
                name: "ix_reviewer_sprint_metrics_reviewer_repository_sprint_number_year",
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
