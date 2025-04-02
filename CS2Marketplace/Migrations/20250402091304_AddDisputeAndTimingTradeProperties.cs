using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CS2Marketplace.Migrations
{
    /// <inheritdoc />
    public partial class AddDisputeAndTimingTradeProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPaid",
                table: "Trades");

            migrationBuilder.RenameColumn(
                name: "TradeOfferExpiresAt",
                table: "Trades",
                newName: "ResolvedAt");

            migrationBuilder.RenameColumn(
                name: "RefundId",
                table: "Trades",
                newName: "DisputeResolution");

            migrationBuilder.RenameColumn(
                name: "PaymentIntentId",
                table: "Trades",
                newName: "DisputeReason");

            migrationBuilder.AlterColumn<string>(
                name: "ReferenceId",
                table: "WalletTransactions",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "AdminNotes",
                table: "Trades",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "BuyerResponseDeadline",
                table: "Trades",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DisputedAt",
                table: "Trades",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OfferSentAt",
                table: "Trades",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdminNotes",
                table: "Trades");

            migrationBuilder.DropColumn(
                name: "BuyerResponseDeadline",
                table: "Trades");

            migrationBuilder.DropColumn(
                name: "DisputedAt",
                table: "Trades");

            migrationBuilder.DropColumn(
                name: "OfferSentAt",
                table: "Trades");

            migrationBuilder.RenameColumn(
                name: "ResolvedAt",
                table: "Trades",
                newName: "TradeOfferExpiresAt");

            migrationBuilder.RenameColumn(
                name: "DisputeResolution",
                table: "Trades",
                newName: "RefundId");

            migrationBuilder.RenameColumn(
                name: "DisputeReason",
                table: "Trades",
                newName: "PaymentIntentId");

            migrationBuilder.AlterColumn<string>(
                name: "ReferenceId",
                table: "WalletTransactions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPaid",
                table: "Trades",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
