using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinCore.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionReversals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ReversalOfTransactionId",
                table: "Transactions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_ReversalOfTransactionId",
                table: "Transactions",
                column: "ReversalOfTransactionId",
                unique: true,
                filter: "[ReversalOfTransactionId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Transactions_ReversalOfTransactionId",
                table: "Transactions",
                column: "ReversalOfTransactionId",
                principalTable: "Transactions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Transactions_ReversalOfTransactionId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_ReversalOfTransactionId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ReversalOfTransactionId",
                table: "Transactions");
        }
    }
}
