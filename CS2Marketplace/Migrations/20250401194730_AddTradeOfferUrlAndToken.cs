using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CS2Marketplace.Migrations
{
    /// <inheritdoc />
    public partial class AddTradeOfferUrlAndToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TradeToken",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TradeOfferUrl",
                table: "Trades",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TradeToken",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TradeOfferUrl",
                table: "Trades");
        }
    }
}
