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
                name: "reviewer",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    login = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false, collation: "NOCASE")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reviewer", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "runstatus",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    last_run_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    last_error = table.Column<string>(type: "TEXT", nullable: true)
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
                    value_stream = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true, collation: "NOCASE")
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
                    total_comments = table.Column<int>(type: "INTEGER", nullable: false),
                    total_lines = table.Column<int>(type: "INTEGER", nullable: false),
                    is_feature = table.Column<bool>(type: "INTEGER", nullable: true),
                    draft_transitions = table.Column<int>(type: "INTEGER", nullable: false),
                    total_commits = table.Column<int>(type: "INTEGER", nullable: false)
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
                name: "metricitem",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    value = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    value_type = table.Column<int>(type: "INTEGER", nullable: false),
                    pr_metric_id = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_metricitem", x => x.id);
                    table.ForeignKey(
                        name: "fk_metricitem_metrics_pr_metric_id",
                        column: x => x.pr_metric_id,
                        principalTable: "metrics",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "reviewermetrics",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    pr_reviewer_id = table.Column<int>(type: "INTEGER", nullable: false),
                    changes_requested = table.Column<int>(type: "INTEGER", nullable: false),
                    approved = table.Column<int>(type: "INTEGER", nullable: false),
                    reviews = table.Column<int>(type: "INTEGER", nullable: false),
                    review_comments = table.Column<int>(type: "INTEGER", nullable: false),
                    pr_metric_id = table.Column<int>(type: "INTEGER", nullable: false)
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
                name: "ix_contributor_pr_metric_id",
                table: "contributor",
                column: "pr_metric_id");

            migrationBuilder.CreateIndex(
                name: "ix_copilotreviewmetrics_pr_metric_id",
                table: "copilotreviewmetrics",
                column: "pr_metric_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_metricitem_pr_metric_id",
                table: "metricitem",
                column: "pr_metric_id");

            migrationBuilder.CreateIndex(
                name: "ix_metrics_author",
                table: "metrics",
                column: "author");

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

            migrationBuilder.CreateIndex(
                name: "ix_team_name",
                table: "team",
                column: "name",
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
                name: "metricitem");

            migrationBuilder.DropTable(
                name: "reviewermetrics");

            migrationBuilder.DropTable(
                name: "runstatus");

            migrationBuilder.DropTable(
                name: "metrics");

            migrationBuilder.DropTable(
                name: "reviewer");

            migrationBuilder.DropTable(
                name: "team");
        }
    }
}
