using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NearGo.Migrations
{
    /// <inheritdoc />
    public partial class ConvertToDealPlatform : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoDiscountApplied",
                table: "Products");

            migrationBuilder.RenameColumn(
                name: "SmartExpiryScore",
                table: "Products",
                newName: "DealScore");

            migrationBuilder.AddColumn<DateTime>(
                name: "DiscountEndDate",
                table: "Products",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_DiscountEndDate",
                table: "Products",
                column: "DiscountEndDate");

            migrationBuilder.Sql("UPDATE Products SET DiscountEndDate = ExpiryDate WHERE DiscountEndDate IS NULL");
            migrationBuilder.Sql("UPDATE Products SET DealScore = DiscountPercentage * 100 WHERE DealScore = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_DiscountEndDate",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "DiscountEndDate",
                table: "Products");

            migrationBuilder.RenameColumn(
                name: "DealScore",
                table: "Products",
                newName: "SmartExpiryScore");

            migrationBuilder.AddColumn<bool>(
                name: "AutoDiscountApplied",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
