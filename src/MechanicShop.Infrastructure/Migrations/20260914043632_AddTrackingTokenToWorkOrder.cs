using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MechanicShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTrackingTokenToWorkOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TrackingToken",
                table: "WorkOrders",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.Sql("UPDATE [WorkOrders] SET [TrackingToken] = NEWID() WHERE [TrackingToken] = '00000000-0000-0000-0000-000000000000'");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_TrackingToken",
                table: "WorkOrders",
                column: "TrackingToken",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkOrders_TrackingToken",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "TrackingToken",
                table: "WorkOrders");
        }
    }
}
