using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CS2Marketplace.Migrations
{
    /// <inheritdoc />
    public partial class AddTradeLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TradeLink",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TradeLink",
                table: "Users");
        }
    }
}
