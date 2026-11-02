using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WhatsEnvio.Modules.Tenancy.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TenancyAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "pk_teancy_events",
                schema: "audit",
                table: "teancy_events");

            migrationBuilder.RenameTable(
                name: "teancy_events",
                schema: "audit",
                newName: "tenancy_events",
                newSchema: "audit");

            migrationBuilder.AddPrimaryKey(
                name: "pk_tenancy_events",
                schema: "audit",
                table: "tenancy_events",
                column: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "pk_tenancy_events",
                schema: "audit",
                table: "tenancy_events");

            migrationBuilder.RenameTable(
                name: "tenancy_events",
                schema: "audit",
                newName: "teancy_events",
                newSchema: "audit");

            migrationBuilder.AddPrimaryKey(
                name: "pk_teancy_events",
                schema: "audit",
                table: "teancy_events",
                column: "id");
        }
    }
}
