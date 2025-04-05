using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CS2Marketplace.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRedundantPropertiesFromMarketplaceListingAndTrade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ItemName",
                table: "Trades");

            migrationBuilder.DropColumn(
                name: "ItemWear",
                table: "Trades");

            migrationBuilder.DropColumn(
                name: "FloatValue",
                table: "MarketplaceListings");

            migrationBuilder.DropColumn(
                name: "PatternIndex",
                table: "MarketplaceListings");

            migrationBuilder.AlterColumn<int>(
                name: "ItemId",
                table: "Trades",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<float>(
                name: "FloatValue",
                table: "Items",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PatternIndex",
                table: "Items",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Trades_ItemId",
                table: "Trades",
                column: "ItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_Trades_Items_ItemId",
                table: "Trades",
                column: "ItemId",
                principalTable: "Items",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Trades_Items_ItemId",
                table: "Trades");

            migrationBuilder.DropIndex(
                name: "IX_Trades_ItemId",
                table: "Trades");

            migrationBuilder.DropColumn(
                name: "FloatValue",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "PatternIndex",
                table: "Items");

            migrationBuilder.AlterColumn<string>(
                name: "ItemId",
                table: "Trades",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "ItemName",
                table: "Trades",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ItemWear",
                table: "Trades",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "FloatValue",
                table: "MarketplaceListings",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PatternIndex",
                table: "MarketplaceListings",
                type: "int",
                nullable: true);
        }
    }
}
