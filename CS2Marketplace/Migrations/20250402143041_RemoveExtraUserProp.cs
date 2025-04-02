using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CS2Marketplace.Migrations
{
    /// <inheritdoc />
    public partial class RemoveExtraUserProp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "APIKey",
                table: "Users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "APIKey",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
