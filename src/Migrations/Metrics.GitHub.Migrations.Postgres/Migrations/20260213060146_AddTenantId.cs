using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Metrics.Migrations.DevExMetricPostgresDb
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
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "tenant_id",
                table: "runstatus",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "tenant_id",
                table: "reviewer_sprint_metrics",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "tenant_id",
                table: "reviewer_monthly_metrics",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "tenant_id",
                table: "metrics",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "tenant_id",
                table: "datamigrationhistory",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ix_datamigrationhistory_tenant_id_migration_id_migration_type",
                table: "datamigrationhistory",
                columns: new[] { "tenant_id", "migration_id", "migration_type" },
                unique: true);

            migrationBuilder.Sql(@"UPDATE metrics SET tenant_id = 'learn' WHERE tenant_id IS NULL or tenant_id = '';
                    UPDATE team SET tenant_id = 'learn' WHERE tenant_id IS NULL or tenant_id = '';
                    UPDATE runstatus SET tenant_id = 'learn' WHERE tenant_id IS NULL or tenant_id = '';
                    UPDATE reviewer_monthly_metrics SET tenant_id = 'learn' WHERE tenant_id IS NULL or tenant_id = '';
                    UPDATE reviewer_sprint_metrics SET tenant_id = 'learn' WHERE tenant_id IS NULL or tenant_id = '';
                    UPDATE datamigrationhistory SET tenant_id = 'learn' WHERE tenant_id IS NULL or tenant_id = '';");
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
            
            migrationBuilder.Sql(@"UPDATE metrics SET tenant_id = '' WHERE tenant_id = 'learn';
                    UPDATE team SET tenant_id = '' WHERE tenant_id = 'learn';
                    UPDATE runstatus SET tenant_id = '' WHERE tenant_id = 'learn';
                    UPDATE reviewer_monthly_metrics SET tenant_id = '' WHERE tenant_id = 'learn';
                    UPDATE reviewer_sprint_metrics SET tenant_id = '' WHERE tenant_id = 'learn';
                    UPDATE datamigrationhistory SET tenant_id = '' WHERE tenant_id = 'learn';");
        }
    }
}
