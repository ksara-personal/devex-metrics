using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Metrics.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_datamigrationhistory_migration_id_migration_type",
                table: "datamigrationhistory");

            migrationBuilder.AddColumn<string>(
                name: "tenant_id",
                table: "team",
                type: "TEXT",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "tenant_id",
                table: "runstatus",
                type: "TEXT",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "tenant_id",
                table: "reviewer_sprint_metrics",
                type: "TEXT",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "tenant_id",
                table: "reviewer_monthly_metrics",
                type: "TEXT",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "tenant_id",
                table: "metrics",
                type: "TEXT",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "tenant_id",
                table: "datamigrationhistory",
                type: "TEXT",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ix_datamigrationhistory_tenant_id_migration_id_migration_type",
                table: "datamigrationhistory",
                columns: new[] { "tenant_id", "migration_id", "migration_type" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_datamigrationhistory_tenant_id_migration_id_migration_type",
                table: "datamigrationhistory");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "team");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "runstatus");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "reviewer_sprint_metrics");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "reviewer_monthly_metrics");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "metrics");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "datamigrationhistory");

            migrationBuilder.CreateIndex(
                name: "ix_datamigrationhistory_migration_id_migration_type",
                table: "datamigrationhistory",
                columns: new[] { "migration_id", "migration_type" },
                unique: true);
        }
    }
}
