using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MVCSampleApp.Migrations
{
    /// <inheritdoc />
    public partial class AddPhotoVaultName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PhotoVaultName",
                table: "Clients",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhotoVaultName",
                table: "Clients");
        }
    }
}
