using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CS2Marketplace.Migrations
{
    /// <inheritdoc />
    public partial class AddSellerEligibilityFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsEligibleForTrading",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastVerificationCheck",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VerificationMessage",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsEligibleForTrading",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastVerificationCheck",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "VerificationMessage",
                table: "Users");
        }
    }
}
