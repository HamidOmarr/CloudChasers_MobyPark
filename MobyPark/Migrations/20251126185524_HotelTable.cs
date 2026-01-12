using System;

using Microsoft.EntityFrameworkCore.Migrations;

using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MobyPark.Migrations
{
    /// <inheritdoc />
    public partial class HotelTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
            DO $$
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'parking_session_status') THEN
                    CREATE TYPE parking_session_status AS ENUM ('preauthorized','pending','paid','failed','refunded');
                END IF;
                IF NOT EXISTS (
                    SELECT 1
                    FROM pg_enum e
                    JOIN pg_type t ON e.enumtypid = t.oid
                    WHERE t.typname = 'parking_session_status'
                      AND e.enumlabel = 'hotelpass'
                ) THEN
                    ALTER TYPE parking_session_status ADD VALUE 'hotelpass';
                END IF;
            END
            $$;
            ");

            migrationBuilder.AddColumn<long>(
                name: "HotelPassId",
                table: "ParkingSessions",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "HotelPasses",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ParkingLotId = table.Column<long>(type: "bigint", nullable: false),
                    LicensePlateNumber = table.Column<string>(type: "text", nullable: false),
                    Start = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    End = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExtraTime = table.Column<TimeSpan>(type: "interval", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HotelPasses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HotelPasses_LicensePlates_LicensePlateNumber",
                        column: x => x.LicensePlateNumber,
                        principalTable: "LicensePlates",
                        principalColumn: "LicensePlateNumber",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HotelPasses_ParkingLots_ParkingLotId",
                        column: x => x.ParkingLotId,
                        principalTable: "ParkingLots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ParkingSessions_HotelPassId",
                table: "ParkingSessions",
                column: "HotelPassId");

            migrationBuilder.CreateIndex(
                name: "IX_HotelPasses_LicensePlateNumber",
                table: "HotelPasses",
                column: "LicensePlateNumber");

            migrationBuilder.CreateIndex(
                name: "IX_HotelPasses_ParkingLotId",
                table: "HotelPasses",
                column: "ParkingLotId");

            migrationBuilder.AddForeignKey(
                name: "FK_ParkingSessions_HotelPasses_HotelPassId",
                table: "ParkingSessions",
                column: "HotelPassId",
                principalTable: "HotelPasses",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ParkingSessions_HotelPasses_HotelPassId",
                table: "ParkingSessions");

            migrationBuilder.DropTable(
                name: "HotelPasses");

            migrationBuilder.DropIndex(
                name: "IX_ParkingSessions_HotelPassId",
                table: "ParkingSessions");

            migrationBuilder.DropColumn(
                name: "HotelPassId",
                table: "ParkingSessions");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:parking_lot_status", "open,closed,maintenance")
                .Annotation("Npgsql:Enum:parking_session_status", "preauthorized,pending,paid,failed,refunded")
                .Annotation("Npgsql:Enum:reservation_status", "pending,confirmed,cancelled,completed,no_show")
                .OldAnnotation("Npgsql:Enum:parking_lot_status", "open,closed,maintenance")
                .OldAnnotation("Npgsql:Enum:parking_session_status", "preauthorized,pending,paid,failed,refunded,hotelpass")
                .OldAnnotation("Npgsql:Enum:reservation_status", "pending,confirmed,cancelled,completed,no_show");
        }
    }
}