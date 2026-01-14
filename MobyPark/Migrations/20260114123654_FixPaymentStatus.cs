using Microsoft.EntityFrameworkCore.Migrations;
using MobyPark.Models;

#nullable disable

namespace MobyPark.Migrations
{
    /// <inheritdoc />
    public partial class FixPaymentStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'parking_session_status') THEN
                        CREATE TYPE parking_session_status AS ENUM (
                            'preauthorized', 'pending', 'paid', 'failed', 'refunded', 
                            'hotelpass', 'businessparking', 'pendinginvoice', 'invoiced'
                        );
                    END IF;
                END $$;
            ");

                    migrationBuilder.Sql(@"
                ALTER TABLE ""ParkingSessions"" 
                ALTER COLUMN ""PaymentStatus"" DROP DEFAULT;
            ");

                    migrationBuilder.Sql(@"
                ALTER TABLE ""ParkingSessions"" 
                ALTER COLUMN ""PaymentStatus"" 
                TYPE parking_session_status 
                USING ""PaymentStatus""::text::parking_session_status;
            ");

                    migrationBuilder.Sql(@"
                ALTER TABLE ""ParkingSessions"" 
                ALTER COLUMN ""PaymentStatus"" 
                SET DEFAULT 'pending'::parking_session_status;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE ""ParkingSessions"" 
                ALTER COLUMN ""PaymentStatus"" 
                SET DATA TYPE payment_status 
                USING ""PaymentStatus""::text::payment_status;
            ");

            // Drop the new type
            migrationBuilder.Sql("DROP TYPE IF EXISTS parking_session_status;");
        }
    }
}
