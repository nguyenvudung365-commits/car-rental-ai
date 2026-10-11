using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarRental.Infrastructure.Migrations;

public partial class FixPaymentBookingOneToMany : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Payment_BookingId",
            table: "Payment");

        migrationBuilder.CreateIndex(
            name: "IX_Payment_BookingId",
            table: "Payment",
            column: "BookingId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Payment_BookingId",
            table: "Payment");

        migrationBuilder.CreateIndex(
            name: "IX_Payment_BookingId",
            table: "Payment",
            column: "BookingId",
            unique: true);
    }
}
