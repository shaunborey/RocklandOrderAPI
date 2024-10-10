using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RocklandOrderAPI.Migrations
{
    /// <inheritdoc />
    public partial class ModifyOrderDetail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderDetails_UserOrders_UserOrderId",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "OrderId",
                table: "OrderDetails");

            migrationBuilder.AlterColumn<int>(
                name: "UserOrderId",
                table: "OrderDetails",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderDetails_UserOrders_UserOrderId",
                table: "OrderDetails",
                column: "UserOrderId",
                principalTable: "UserOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderDetails_UserOrders_UserOrderId",
                table: "OrderDetails");

            migrationBuilder.AlterColumn<int>(
                name: "UserOrderId",
                table: "OrderDetails",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "OrderId",
                table: "OrderDetails",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderDetails_UserOrders_UserOrderId",
                table: "OrderDetails",
                column: "UserOrderId",
                principalTable: "UserOrders",
                principalColumn: "Id");
        }
    }
}
