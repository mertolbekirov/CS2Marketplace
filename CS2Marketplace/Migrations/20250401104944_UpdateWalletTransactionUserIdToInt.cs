using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CS2Marketplace.Migrations
{
    /// <inheritdoc />
    public partial class UpdateWalletTransactionUserIdToInt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TransactionType",
                table: "WalletTransactions",
                newName: "Type");

            migrationBuilder.RenameColumn(
                name: "Timestamp",
                table: "WalletTransactions",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "PaymentId",
                table: "WalletTransactions",
                newName: "ReferenceId");

            migrationBuilder.RenameColumn(
                name: "TradeStatus",
                table: "Trades",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "TradeInitiatedAt",
                table: "Trades",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "TradeCompletedAt",
                table: "Trades",
                newName: "TradeOfferExpiresAt");

            migrationBuilder.RenameColumn(
                name: "PaymentId",
                table: "Trades",
                newName: "StatusMessage");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "WalletTransactions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SteamApiKey",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "Trades",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPaid",
                table: "Trades",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsRefunded",
                table: "Trades",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ItemId",
                table: "Trades",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

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
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastChecked",
                table: "Trades",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentIntentId",
                table: "Trades",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefundId",
                table: "Trades",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RefundedAt",
                table: "Trades",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TradeOfferId",
                table: "Trades",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "WalletTransactions");

            migrationBuilder.DropColumn(
                name: "SteamApiKey",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "Trades");

            migrationBuilder.DropColumn(
                name: "IsPaid",
                table: "Trades");

            migrationBuilder.DropColumn(
                name: "IsRefunded",
                table: "Trades");

            migrationBuilder.DropColumn(
                name: "ItemId",
                table: "Trades");

            migrationBuilder.DropColumn(
                name: "ItemName",
                table: "Trades");

            migrationBuilder.DropColumn(
                name: "ItemWear",
                table: "Trades");

            migrationBuilder.DropColumn(
                name: "LastChecked",
                table: "Trades");

            migrationBuilder.DropColumn(
                name: "PaymentIntentId",
                table: "Trades");

            migrationBuilder.DropColumn(
                name: "RefundId",
                table: "Trades");

            migrationBuilder.DropColumn(
                name: "RefundedAt",
                table: "Trades");

            migrationBuilder.DropColumn(
                name: "TradeOfferId",
                table: "Trades");

            migrationBuilder.RenameColumn(
                name: "Type",
                table: "WalletTransactions",
                newName: "TransactionType");

            migrationBuilder.RenameColumn(
                name: "ReferenceId",
                table: "WalletTransactions",
                newName: "PaymentId");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "WalletTransactions",
                newName: "Timestamp");

            migrationBuilder.RenameColumn(
                name: "TradeOfferExpiresAt",
                table: "Trades",
                newName: "TradeCompletedAt");

            migrationBuilder.RenameColumn(
                name: "StatusMessage",
                table: "Trades",
                newName: "PaymentId");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "Trades",
                newName: "TradeStatus");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Trades",
                newName: "TradeInitiatedAt");
        }
    }
}
