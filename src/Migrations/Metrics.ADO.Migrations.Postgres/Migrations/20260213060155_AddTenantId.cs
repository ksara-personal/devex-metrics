using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Metrics.Extensions.ADO.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "tenant_id",
                table: "workitem_metrics",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "tenant_id",
                table: "datamigrationhistory_ado",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(@"UPDATE workitem_metrics SET tenant_id = 'learn' WHERE tenant_id IS NULL or tenant_id = '';
                UPDATE datamigrationhistory_ado SET tenant_id = 'learn' WHERE tenant_id IS NULL or tenant_id = '';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "workitem_metrics");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "datamigrationhistory_ado");
                
            migrationBuilder.Sql(@"UPDATE workitem_metrics SET tenant_id = '' WHERE tenant_id = 'learn';
                UPDATE datamigrationhistory_ado SET tenant_id = '' WHERE tenant_id = 'learn';");
        }
    }
}
