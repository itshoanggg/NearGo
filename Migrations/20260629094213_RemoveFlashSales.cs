using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NearGo.Migrations
{
    /// <inheritdoc />
    public partial class RemoveFlashSales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_FlashSaleEnd",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "FlashSaleEnd",
                table: "Products");

            migrationBuilder.RenameColumn(
                name: "FlashSaleStart",
                table: "Products",
                newName: "DiscountEndDate");

            migrationBuilder.CreateIndex(
                name: "IX_Products_DiscountEndDate",
                table: "Products",
                column: "DiscountEndDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_DiscountEndDate",
                table: "Products");

            migrationBuilder.RenameColumn(
                name: "DiscountEndDate",
                table: "Products",
                newName: "FlashSaleStart");

            migrationBuilder.AddColumn<DateTime>(
                name: "FlashSaleEnd",
                table: "Products",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_FlashSaleEnd",
                table: "Products",
                column: "FlashSaleEnd");
        }
    }
}
