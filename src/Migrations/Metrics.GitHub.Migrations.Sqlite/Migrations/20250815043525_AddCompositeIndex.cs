using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Metrics.Migrations
{
    /// <inheritdoc />
    public partial class AddCompositeIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_team_name",
                table: "team");

            migrationBuilder.CreateIndex(
                name: "ix_team_name",
                table: "team",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_team_name_value_stream_region",
                table: "team",
                columns: new[] { "name", "value_stream", "region" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_metrics_created_at_author",
                table: "metrics",
                columns: new[] { "created_at", "author" });

            migrationBuilder.CreateIndex(
                name: "ix_metrics_created_at_team_id",
                table: "metrics",
                columns: new[] { "created_at", "team_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_team_name",
                table: "team");

            migrationBuilder.DropIndex(
                name: "ix_team_name_value_stream_region",
                table: "team");

            migrationBuilder.DropIndex(
                name: "ix_metrics_created_at_author",
                table: "metrics");

            migrationBuilder.DropIndex(
                name: "ix_metrics_created_at_team_id",
                table: "metrics");

            migrationBuilder.CreateIndex(
                name: "ix_team_name",
                table: "team",
                column: "name",
                unique: true);
        }
    }
}
