using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Metrics.GitActivity.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CollectionRuns",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Repository = table.Column<string>(type: "TEXT", nullable: false),
                    WindowStart = table.Column<DateTime>(type: "TEXT", nullable: false),
                    WindowEnd = table.Column<DateTime>(type: "TEXT", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PullRequestCount = table.Column<int>(type: "INTEGER", nullable: false),
                    CommitCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Error = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectionRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Commits",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Repository = table.Column<string>(type: "TEXT", nullable: false),
                    Branch = table.Column<string>(type: "TEXT", nullable: false),
                    Hash = table.Column<string>(type: "TEXT", nullable: false),
                    AuthorName = table.Column<string>(type: "TEXT", nullable: true),
                    AuthorEmail = table.Column<string>(type: "TEXT", nullable: true),
                    AuthorDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CommitterName = table.Column<string>(type: "TEXT", nullable: true),
                    CommitterEmail = table.Column<string>(type: "TEXT", nullable: true),
                    CommitterDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Subject = table.Column<string>(type: "TEXT", nullable: false),
                    Body = table.Column<string>(type: "TEXT", nullable: true),
                    ParentHashes = table.Column<string>(type: "TEXT", nullable: false),
                    ParentCount = table.Column<int>(type: "INTEGER", nullable: false),
                    CollectedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Commits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PullRequests",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Repository = table.Column<string>(type: "TEXT", nullable: false),
                    Number = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Url = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    State = table.Column<string>(type: "TEXT", nullable: false),
                    IsDraft = table.Column<bool>(type: "INTEGER", nullable: false),
                    AuthorLogin = table.Column<string>(type: "TEXT", nullable: true),
                    AuthorName = table.Column<string>(type: "TEXT", nullable: true),
                    AuthorEmail = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    MergedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SourceBranch = table.Column<string>(type: "TEXT", nullable: false),
                    TargetBranch = table.Column<string>(type: "TEXT", nullable: false),
                    ReviewDecision = table.Column<string>(type: "TEXT", nullable: true),
                    CommentsCount = table.Column<int>(type: "INTEGER", nullable: false),
                    LinesAdded = table.Column<int>(type: "INTEGER", nullable: false),
                    LinesDeleted = table.Column<int>(type: "INTEGER", nullable: false),
                    FilesChanged = table.Column<int>(type: "INTEGER", nullable: false),
                    CommitsCount = table.Column<int>(type: "INTEGER", nullable: false),
                    CiStatus = table.Column<string>(type: "TEXT", nullable: true),
                    ReviewSubmittedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FirstReviewAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ReviewRounds = table.Column<int>(type: "INTEGER", nullable: false),
                    CollectedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PullRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PullRequestComments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PullRequestId = table.Column<long>(type: "INTEGER", nullable: false),
                    AuthorLogin = table.Column<string>(type: "TEXT", nullable: true),
                    AuthorEmail = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PullRequestComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PullRequestComments_PullRequests_PullRequestId",
                        column: x => x.PullRequestId,
                        principalTable: "PullRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PullRequestLabels",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PullRequestId = table.Column<long>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PullRequestLabels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PullRequestLabels_PullRequests_PullRequestId",
                        column: x => x.PullRequestId,
                        principalTable: "PullRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PullRequestLinkedIssues",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PullRequestId = table.Column<long>(type: "INTEGER", nullable: false),
                    Reference = table.Column<string>(type: "TEXT", nullable: false),
                    Url = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PullRequestLinkedIssues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PullRequestLinkedIssues_PullRequests_PullRequestId",
                        column: x => x.PullRequestId,
                        principalTable: "PullRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PullRequestReviewRequests",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PullRequestId = table.Column<long>(type: "INTEGER", nullable: false),
                    Reviewer = table.Column<string>(type: "TEXT", nullable: false),
                    ReviewerEmail = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PullRequestReviewRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PullRequestReviewRequests_PullRequests_PullRequestId",
                        column: x => x.PullRequestId,
                        principalTable: "PullRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PullRequestReviews",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PullRequestId = table.Column<long>(type: "INTEGER", nullable: false),
                    ReviewerLogin = table.Column<string>(type: "TEXT", nullable: true),
                    ReviewerEmail = table.Column<string>(type: "TEXT", nullable: true),
                    State = table.Column<string>(type: "TEXT", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CommentsCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PullRequestReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PullRequestReviews_PullRequests_PullRequestId",
                        column: x => x.PullRequestId,
                        principalTable: "PullRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CollectionRuns_Repository",
                table: "CollectionRuns",
                column: "Repository");

            migrationBuilder.CreateIndex(
                name: "IX_Commits_Repository_AuthorDate",
                table: "Commits",
                columns: new[] { "Repository", "AuthorDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Commits_Repository_Hash",
                table: "Commits",
                columns: new[] { "Repository", "Hash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PullRequestComments_PullRequestId",
                table: "PullRequestComments",
                column: "PullRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_PullRequestLabels_PullRequestId",
                table: "PullRequestLabels",
                column: "PullRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_PullRequestLinkedIssues_PullRequestId",
                table: "PullRequestLinkedIssues",
                column: "PullRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_PullRequestReviewRequests_PullRequestId",
                table: "PullRequestReviewRequests",
                column: "PullRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_PullRequestReviews_PullRequestId",
                table: "PullRequestReviews",
                column: "PullRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_PullRequests_Repository_CreatedAt",
                table: "PullRequests",
                columns: new[] { "Repository", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PullRequests_Repository_Number",
                table: "PullRequests",
                columns: new[] { "Repository", "Number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CollectionRuns");

            migrationBuilder.DropTable(
                name: "Commits");

            migrationBuilder.DropTable(
                name: "PullRequestComments");

            migrationBuilder.DropTable(
                name: "PullRequestLabels");

            migrationBuilder.DropTable(
                name: "PullRequestLinkedIssues");

            migrationBuilder.DropTable(
                name: "PullRequestReviewRequests");

            migrationBuilder.DropTable(
                name: "PullRequestReviews");

            migrationBuilder.DropTable(
                name: "PullRequests");
        }
    }
}
