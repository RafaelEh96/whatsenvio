using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WhatsEnvio.Modules.Tenancy.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarIsActive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                schema: "iam",
                table: "tenants",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_active",
                schema: "iam",
                table: "tenants");
        }
    }
}
