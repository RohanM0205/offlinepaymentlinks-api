using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OfflinePaymentLinks.API.Migrations
{
    /// <inheritdoc />
    public partial class AddRolePermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(nullable: false),
                    CanSendPaymentLink = table.Column<bool>(nullable: false),
                    CanViewReports = table.Column<bool>(nullable: false),
                    CanManageUsers = table.Column<bool>(nullable: false),
                    CanApproveUsers = table.Column<bool>(nullable: false),
                    CanManageRoles = table.Column<bool>(nullable: false),
                    CanManageAdmins = table.Column<bool>(nullable: false),
                    AllowedTransactionTypes = table.Column<string>(nullable: false),
                    UpdatedAt = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_RoleId",
                table: "RolePermissions",
                column: "RoleId",
                unique: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RolePermissions");
        }
    }
}
