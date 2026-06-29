using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NearGo.Migrations
{
    /// <inheritdoc />
    public partial class FixFlashSaleColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatMessages");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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

            migrationBuilder.CreateTable(
                name: "ChatMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AiResponse = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserMessage = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatMessages_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_UserId",
                table: "ChatMessages",
                column: "UserId");
        }
    }
}
