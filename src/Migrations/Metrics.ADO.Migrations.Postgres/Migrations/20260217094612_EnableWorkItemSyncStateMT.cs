using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Metrics.Extensions.ADO.Migrations
{
    /// <inheritdoc />
    public partial class EnableWorkItemSyncStateMT : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "tenant_id",
                table: "workitem_sync_status",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(@"UPDATE workitem_sync_status SET tenant_id = 'tenant-1' WHERE tenant_id IS NULL or tenant_id = ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "workitem_sync_status");
            
            migrationBuilder.Sql(@"UPDATE workitem_sync_status SET tenant_id = '' WHERE tenant_id = 'tenant-1'");
        }
    }
}
