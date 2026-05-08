using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OfflinePaymentLinks.API.Migrations
{
    /// <inheritdoc />
    public partial class AddLinkSharedBy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LinkSharedBy",
                table: "PrePaymentData",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LinkSharedBy",
                table: "PrePaymentData");
        }
    }
}
