using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderItemDatesRemoveCurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Currency",
                table: "SalesOrderItems");

            migrationBuilder.AddColumn<DateOnly>(
                name: "EndDate",
                table: "SalesOrderItems",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "StartDate",
                table: "SalesOrderItems",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "SalesOrderItems");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "SalesOrderItems");

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "SalesOrderItems",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
