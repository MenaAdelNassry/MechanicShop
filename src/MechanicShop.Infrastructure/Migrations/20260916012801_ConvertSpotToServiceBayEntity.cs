using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MechanicShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ConvertSpotToServiceBayEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Spot",
                table: "WorkOrders");

            migrationBuilder.AddColumn<Guid>(
                name: "SpotId",
                table: "WorkOrders",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "ServiceBays",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceBays", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_SpotId",
                table: "WorkOrders",
                column: "SpotId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceBays_Name",
                table: "ServiceBays",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkOrders_ServiceBays_SpotId",
                table: "WorkOrders",
                column: "SpotId",
                principalTable: "ServiceBays",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkOrders_ServiceBays_SpotId",
                table: "WorkOrders");

            migrationBuilder.DropTable(
                name: "ServiceBays");

            migrationBuilder.DropIndex(
                name: "IX_WorkOrders_SpotId",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "SpotId",
                table: "WorkOrders");

            migrationBuilder.AddColumn<string>(
                name: "Spot",
                table: "WorkOrders",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");
        }
    }
}
