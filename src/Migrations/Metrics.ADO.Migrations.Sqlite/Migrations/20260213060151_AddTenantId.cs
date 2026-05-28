using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Metrics.ADO.Migrations
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
                type: "TEXT",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "tenant_id",
                table: "datamigrationhistory_ado",
                type: "TEXT",
                maxLength: 64,
                nullable: false,
                defaultValue: "");
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
        }
    }
}
