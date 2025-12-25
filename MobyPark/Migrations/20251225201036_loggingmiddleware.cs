using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MobyPark.Migrations
{
    /// <inheritdoc />
    public partial class loggingmiddleware : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:parking_lot_status", "open,closed,maintenance")
                .Annotation("Npgsql:Enum:parking_session_status", "preauthorized,pending,paid,failed,refunded,hotelpass,businessparking,pendinginvoice,invoiced")
                .Annotation("Npgsql:Enum:reservation_status", "pending,confirmed,cancelled,completed,no_show")
                .OldAnnotation("Npgsql:Enum:parking_lot_status", "open,closed,maintenance")
                .OldAnnotation("Npgsql:Enum:parking_session_status", "preauthorized,pending,paid,failed,refunded,hotelpass")
                .OldAnnotation("Npgsql:Enum:reservation_status", "pending,confirmed,cancelled,completed,no_show");

            migrationBuilder.AddColumn<long>(
                name: "BusinessParkingRegistrationId",
                table: "ParkingSessions",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ParkingSessions_BusinessParkingRegistrationId",
                table: "ParkingSessions",
                column: "BusinessParkingRegistrationId");

            migrationBuilder.AddForeignKey(
                name: "FK_ParkingSessions_BusinessParkingRegistrations_BusinessParkin~",
                table: "ParkingSessions",
                column: "BusinessParkingRegistrationId",
                principalTable: "BusinessParkingRegistrations",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ParkingSessions_BusinessParkingRegistrations_BusinessParkin~",
                table: "ParkingSessions");

            migrationBuilder.DropIndex(
                name: "IX_ParkingSessions_BusinessParkingRegistrationId",
                table: "ParkingSessions");

            migrationBuilder.DropColumn(
                name: "BusinessParkingRegistrationId",
                table: "ParkingSessions");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:parking_lot_status", "open,closed,maintenance")
                .Annotation("Npgsql:Enum:parking_session_status", "preauthorized,pending,paid,failed,refunded,hotelpass")
                .Annotation("Npgsql:Enum:reservation_status", "pending,confirmed,cancelled,completed,no_show")
                .OldAnnotation("Npgsql:Enum:parking_lot_status", "open,closed,maintenance")
                .OldAnnotation("Npgsql:Enum:parking_session_status", "preauthorized,pending,paid,failed,refunded,hotelpass,businessparking,pendinginvoice,invoiced")
                .OldAnnotation("Npgsql:Enum:reservation_status", "pending,confirmed,cancelled,completed,no_show");
        }
    }
}
