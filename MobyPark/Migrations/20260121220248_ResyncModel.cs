using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore.Migrations;

using MobyPark.Models;

using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MobyPark.Migrations
{
    /// <inheritdoc />
    public partial class ResyncModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Issuer",
                table: "Transactions",
                newName: "Token");

            migrationBuilder.AddColumn<Guid>(
                name: "PaymentId",
                table: "ParkingSessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ParkingSessions_PaymentId",
                table: "ParkingSessions",
                column: "PaymentId");

            migrationBuilder.AddForeignKey(
                name: "FK_ParkingSessions_Payments_PaymentId",
                table: "ParkingSessions",
                column: "PaymentId",
                principalTable: "Payments",
                principalColumn: "PaymentId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ParkingSessions_Payments_PaymentId",
                table: "ParkingSessions");

            migrationBuilder.DropIndex(
                name: "IX_ParkingSessions_PaymentId",
                table: "ParkingSessions");

            migrationBuilder.DropColumn(
                name: "PaymentId",
                table: "ParkingSessions");

            migrationBuilder.RenameColumn(
                name: "Token",
                table: "Transactions",
                newName: "Issuer");
        }
    }
}