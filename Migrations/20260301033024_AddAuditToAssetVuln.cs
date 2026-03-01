using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API_SVsharp.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditToAssetVuln : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "AssetsVulnerabilidades",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "AssetsVulnerabilidades",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "AssetsVulnerabilidades",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "AssetsVulnerabilidades",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "AssetsVulnerabilidades");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "AssetsVulnerabilidades");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "AssetsVulnerabilidades");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "AssetsVulnerabilidades");
        }
    }
}
