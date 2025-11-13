using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Mango.Services.OrderAPI.Migrations.PostgreSQL
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrderHeaders",
                columns: table => new
                {
                    OrderHeaderId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    CouponCode = table.Column<string>(type: "text", nullable: true),
                    Discount = table.Column<double>(type: "double precision", nullable: false),
                    OrderTotal = table.Column<double>(type: "double precision", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: true),
                    DeliveryFee = table.Column<double>(type: "double precision", nullable: false),
                    OrderTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PaymentIntentId = table.Column<string>(type: "text", nullable: true),
                    ClientSecret = table.Column<string>(type: "text", nullable: true),
                    PaymentSummary_Last4 = table.Column<int>(type: "integer", nullable: true),
                    PaymentSummary_Brand = table.Column<string>(type: "text", nullable: true),
                    PaymentSummary_ExpMonth = table.Column<int>(type: "integer", nullable: true),
                    PaymentSummary_ExpYear = table.Column<int>(type: "integer", nullable: true),
                    ShippingAddress_Name = table.Column<string>(type: "text", nullable: true),
                    ShippingAddress_Line1 = table.Column<string>(type: "text", nullable: true),
                    ShippingAddress_Line2 = table.Column<string>(type: "text", nullable: true),
                    ShippingAddress_City = table.Column<string>(type: "text", nullable: true),
                    ShippingAddress_State = table.Column<string>(type: "text", nullable: true),
                    ShippingAddress_PostalCode = table.Column<string>(type: "text", nullable: true),
                    ShippingAddress_Country = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderHeaders", x => x.OrderHeaderId);
                });

            migrationBuilder.CreateTable(
                name: "OrderDetails",
                columns: table => new
                {
                    OrderDetailsId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    ProductName = table.Column<string>(type: "text", nullable: false),
                    Price = table.Column<double>(type: "double precision", nullable: false),
                    OrderHeaderId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderDetails", x => x.OrderDetailsId);
                    table.ForeignKey(
                        name: "FK_OrderDetails_OrderHeaders_OrderHeaderId",
                        column: x => x.OrderHeaderId,
                        principalTable: "OrderHeaders",
                        principalColumn: "OrderHeaderId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderDetails_OrderHeaderId",
                table: "OrderDetails",
                column: "OrderHeaderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderDetails");

            migrationBuilder.DropTable(
                name: "OrderHeaders");
        }
    }
}
