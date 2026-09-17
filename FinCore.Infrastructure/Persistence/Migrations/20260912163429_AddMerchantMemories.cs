using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinCore.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMerchantMemories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MerchantMemories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NormalizedMerchant = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MerchantMemories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MerchantMemories_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MerchantMemories_CategoryId",
                table: "MerchantMemories",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_MerchantMemories_NormalizedMerchant",
                table: "MerchantMemories",
                column: "NormalizedMerchant",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MerchantMemories");
        }
    }
}
