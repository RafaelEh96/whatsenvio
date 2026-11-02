using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WhatsEnvio.Modules.Identity.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class IdentityAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ocurred_at_utc",
                schema: "audit",
                table: "identity_events",
                newName: "occurred_at_utc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "occurred_at_utc",
                schema: "audit",
                table: "identity_events",
                newName: "ocurred_at_utc");
        }
    }
}
