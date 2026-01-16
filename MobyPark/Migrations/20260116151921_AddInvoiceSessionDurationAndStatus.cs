using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MobyPark.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceSessionDurationAndStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:invoice_status", "Pending,Paid,Overdue,Cancelled")
                .Annotation("Npgsql:Enum:parking_lot_status", "open,closed,maintenance")
                .Annotation("Npgsql:Enum:parking_session_status", "preauthorized,pending,paid,failed,refunded,hotelpass,businessparking,pendinginvoice,invoiced")
                .Annotation("Npgsql:Enum:reservation_status", "pending,confirmed,cancelled,completed,no_show")
                .OldAnnotation("Npgsql:Enum:parking_lot_status", "open,closed,maintenance")
                .OldAnnotation("Npgsql:Enum:parking_session_status", "preauthorized,pending,paid,failed,refunded,hotelpass,businessparking,pendinginvoice,invoiced")
                .OldAnnotation("Npgsql:Enum:reservation_status", "pending,confirmed,cancelled,completed,no_show");

            migrationBuilder.AddColumn<int>(
                name: "SessionDuration",
                table: "Invoices",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Invoices",
                type: "invoice_status",
                nullable: false,
                defaultValueSql: "'Pending'::invoice_status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SessionDuration",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Invoices");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:parking_lot_status", "open,closed,maintenance")
                .Annotation("Npgsql:Enum:parking_session_status", "preauthorized,pending,paid,failed,refunded,hotelpass,businessparking,pendinginvoice,invoiced")
                .Annotation("Npgsql:Enum:reservation_status", "pending,confirmed,cancelled,completed,no_show")
                .OldAnnotation("Npgsql:Enum:invoice_status", "Pending,Paid,Overdue,Cancelled")
                .OldAnnotation("Npgsql:Enum:parking_lot_status", "open,closed,maintenance")
                .OldAnnotation("Npgsql:Enum:parking_session_status", "preauthorized,pending,paid,failed,refunded,hotelpass,businessparking,pendinginvoice,invoiced")
                .OldAnnotation("Npgsql:Enum:reservation_status", "pending,confirmed,cancelled,completed,no_show");
        }
    }
}
