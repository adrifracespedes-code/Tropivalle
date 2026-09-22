using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using EcommerceApp.Data;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EcommerceApp.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260921120000_AddSales")]
public class AddSales : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Sales",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                ProductId = table.Column<int>(type: "integer", nullable: false),
                ProductName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                Quantity = table.Column<int>(type: "integer", nullable: false),
                UnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                TotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                SoldAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Notes = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Sales", x => x.Id);
                table.ForeignKey(
                    name: "FK_Sales_Products_ProductId",
                    column: x => x.ProductId,
                    principalTable: "Products",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Sales_Category",
            table: "Sales",
            column: "Category");

        migrationBuilder.CreateIndex(
            name: "IX_Sales_ProductId",
            table: "Sales",
            column: "ProductId");

        migrationBuilder.CreateIndex(
            name: "IX_Sales_SoldAt",
            table: "Sales",
            column: "SoldAt");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Sales");
    }
}
