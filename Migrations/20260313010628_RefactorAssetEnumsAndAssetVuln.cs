using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rapsodia.Migrations
{
    /// <inheritdoc />
    public partial class RefactorAssetEnumsAndAssetVuln : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "assets_vulnerabilidades");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "assets_vulnerabilidades");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "assets_vulnerabilidades");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "assets_vulnerabilidades",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "assets_vulnerabilidades",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "NOW()");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "assets_vulnerabilidades",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "assets_vulnerabilidades",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "assets_vulnerabilidades",
                type: "timestamp with time zone",
                nullable: true);
        }
    }
}
