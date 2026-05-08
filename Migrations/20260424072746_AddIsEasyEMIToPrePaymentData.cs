using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OfflinePaymentLinks.API.Migrations
{
    /// <inheritdoc />
    public partial class AddIsEasyEMIToPrePaymentData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsEasyEMI",
                table: "PrePaymentData",
                type: "bit",
                nullable: true,
                defaultValue: null);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsEasyEMI",
                table: "PrePaymentData");
        }
    }
}