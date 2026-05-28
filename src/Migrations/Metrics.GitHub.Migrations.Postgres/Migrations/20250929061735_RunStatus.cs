using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Metrics.Migrations.DevExMetricPostgresDb
{
    /// <inheritdoc />
    public partial class RunStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "pr_number",
                table: "runstatus",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "repository",
                table: "runstatus",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_runstatus_repository_pr_number",
                table: "runstatus",
                columns: new[] { "repository", "pr_number" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_runstatus_repository_pr_number",
                table: "runstatus");

            migrationBuilder.DropColumn(
                name: "pr_number",
                table: "runstatus");

            migrationBuilder.DropColumn(
                name: "repository",
                table: "runstatus");
        }
    }
}
