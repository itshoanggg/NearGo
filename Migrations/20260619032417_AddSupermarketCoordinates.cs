using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NearGo.Migrations
{
    /// <inheritdoc />
    public partial class AddSupermarketCoordinates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Supermarkets",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Supermarkets",
                type: "float",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Supermarkets");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Supermarkets");
        }
    }
}
