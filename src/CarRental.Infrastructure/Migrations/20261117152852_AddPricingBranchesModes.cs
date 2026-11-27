using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarRental.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPricingBranchesModes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "Cars",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PricePerHour",
                table: "Cars",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryAddress",
                table: "Bookings",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Mode",
                table: "Bookings",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "PickupBranchId",
                table: "Bookings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReturnAddress",
                table: "Bookings",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReturnBranchId",
                table: "Bookings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "WithDriver",
                table: "Bookings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Branches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Branches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Coupons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MaxDiscount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    MinOrderAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidTo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsageLimit = table.Column<int>(type: "int", nullable: true),
                    UsedCount = table.Column<int>(type: "int", nullable: false),
                    PerCustomerLimit = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Coupons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Surcharges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AutoApply = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Surcharges", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BookingCharges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BookingId = table.Column<int>(type: "int", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CouponId = table.Column<int>(type: "int", nullable: true),
                    SurchargeId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingCharges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookingCharges_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BookingCharges_Coupons_CouponId",
                        column: x => x.CouponId,
                        principalTable: "Coupons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BookingCharges_Surcharges_SurchargeId",
                        column: x => x.SurchargeId,
                        principalTable: "Surcharges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cars_BranchId",
                table: "Cars",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_PickupBranchId",
                table: "Bookings",
                column: "PickupBranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_ReturnBranchId",
                table: "Bookings",
                column: "ReturnBranchId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingCharges_BookingId",
                table: "BookingCharges",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingCharges_CouponId",
                table: "BookingCharges",
                column: "CouponId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingCharges_SurchargeId",
                table: "BookingCharges",
                column: "SurchargeId");

            migrationBuilder.CreateIndex(
                name: "IX_Coupons_Code",
                table: "Coupons",
                column: "Code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Branches_PickupBranchId",
                table: "Bookings",
                column: "PickupBranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Branches_ReturnBranchId",
                table: "Bookings",
                column: "ReturnBranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Cars_Branches_BranchId",
                table: "Cars",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Branches_PickupBranchId",
                table: "Bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Branches_ReturnBranchId",
                table: "Bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_Cars_Branches_BranchId",
                table: "Cars");

            migrationBuilder.DropTable(
                name: "BookingCharges");

            migrationBuilder.DropTable(
                name: "Branches");

            migrationBuilder.DropTable(
                name: "Coupons");

            migrationBuilder.DropTable(
                name: "Surcharges");

            migrationBuilder.DropIndex(
                name: "IX_Cars_BranchId",
                table: "Cars");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_PickupBranchId",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_ReturnBranchId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "Cars");

            migrationBuilder.DropColumn(
                name: "PricePerHour",
                table: "Cars");

            migrationBuilder.DropColumn(
                name: "DeliveryAddress",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "Mode",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "PickupBranchId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "ReturnAddress",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "ReturnBranchId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "WithDriver",
                table: "Bookings");
        }
    }
}
