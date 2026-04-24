using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Online_Mobile_Recharge.Migrations
{
    /// <inheritdoc />
    public partial class DailyLoginLockout : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FailedLoginAttempts",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "FailedLoginDate",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LoginLockoutUntil",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email_FailedLoginDate",
                table: "Users",
                columns: new[] { "Email", "FailedLoginDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_Email_FailedLoginDate",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "FailedLoginAttempts",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "FailedLoginDate",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LoginLockoutUntil",
                table: "Users");
        }
    }
}
