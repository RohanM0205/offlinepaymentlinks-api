using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OfflinePaymentLinks.API.Migrations
{
    public partial class AddProductTypeToPrePayment : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProductType",
                table: "PrePaymentData",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProductType",
                table: "PrePaymentData");
        }
    }
}