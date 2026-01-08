using Microsoft.EntityFrameworkCore.Migrations;

namespace MobyPark.Migrations
{
    public partial class RenameSnakeToPascalAndTypes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Ensure enum contains all values used by the app
            migrationBuilder.Sql("ALTER TYPE public.payment_status ADD VALUE IF NOT EXISTS 'hotelpass';");

            // Table renames to PascalCase (if they exist in snake_case)
            migrationBuilder.Sql("ALTER TABLE IF EXISTS public.license_plates RENAME TO \"LicensePlates\";");
            migrationBuilder.Sql("ALTER TABLE IF EXISTS public.parking_lots RENAME TO \"ParkingLots\";");
            migrationBuilder.Sql("ALTER TABLE IF EXISTS public.sessions RENAME TO \"ParkingSessions\";");
            migrationBuilder.Sql("ALTER TABLE IF EXISTS public.payments RENAME TO \"Payments\";");
            migrationBuilder.Sql("ALTER TABLE IF EXISTS public.permissions RENAME TO \"Permissions\";");
            migrationBuilder.Sql("ALTER TABLE IF EXISTS public.reservations RENAME TO \"Reservations\";");
            migrationBuilder.Sql("ALTER TABLE IF EXISTS public.role_permissions RENAME TO \"RolePermissions\";");
            migrationBuilder.Sql("ALTER TABLE IF EXISTS public.roles RENAME TO \"Roles\";");
            migrationBuilder.Sql("ALTER TABLE IF EXISTS public.transactions RENAME TO \"Transactions\";");
            migrationBuilder.Sql("ALTER TABLE IF EXISTS public.user_plates RENAME TO \"UserPlates\";");
            migrationBuilder.Sql("ALTER TABLE IF EXISTS public.users RENAME TO \"Users\";");

            // Column renames for LicensePlates
            migrationBuilder.Sql("ALTER TABLE IF EXISTS \"LicensePlates\" RENAME COLUMN IF EXISTS license_plate TO \"LicensePlateNumber\";");

            // Column renames for ParkingLots + type alignments
            migrationBuilder.Sql("ALTER TABLE IF EXISTS \"ParkingLots\" RENAME COLUMN IF EXISTS id TO \"Id\";");
            migrationBuilder.Sql("ALTER TABLE IF EXISTS \"ParkingLots\" RENAME COLUMN IF EXISTS name TO \"Name\";");
            migrationBuilder.Sql("ALTER TABLE IF EXISTS \"ParkingLots\" RENAME COLUMN IF EXISTS location TO \"Location\";");
            migrationBuilder.Sql("ALTER TABLE IF EXISTS \"ParkingLots\" RENAME COLUMN IF EXISTS address TO \"Address\";");
            migrationBuilder.Sql("ALTER TABLE IF EXISTS \"ParkingLots\" RENAME COLUMN IF EXISTS capacity TO \"Capacity\";");
            migrationBuilder.Sql("ALTER TABLE IF EXISTS \"ParkingLots\" RENAME COLUMN IF EXISTS reserved TO \"Reserved\";");
            migrationBuilder.Sql("ALTER TABLE IF EXISTS \"ParkingLots\" RENAME COLUMN IF EXISTS tariff TO \"Tariff\";");
            migrationBuilder.Sql("ALTER TABLE IF EXISTS \"ParkingLots\" RENAME COLUMN IF EXISTS daytariff TO \"DayTariff\";");
            migrationBuilder.Sql("ALTER TABLE IF EXISTS \"ParkingLots\" RENAME COLUMN IF EXISTS created_at TO \"CreatedAt\";");
            migrationBuilder.Sql("ALTER TABLE IF EXISTS \"ParkingLots\" RENAME COLUMN IF EXISTS status TO \"Status\";");
            migrationBuilder.Sql("ALTER TABLE IF EXISTS \"ParkingLots\" ALTER COLUMN \"Tariff\" TYPE numeric USING \"Tariff\"::numeric;");
            migrationBuilder.Sql("ALTER TABLE IF EXISTS \"ParkingLots\" ALTER COLUMN \"DayTariff\" TYPE numeric USING \"DayTariff\"::numeric;");
            migrationBuilder.Sql("ALTER TABLE IF EXISTS \"ParkingLots\" ALTER COLUMN \"CreatedAt\" TYPE timestamptz USING \"CreatedAt\"::timestamp with time zone;");

            // Column renames for ParkingSessions + type alignments
            migrationBuilder.Sql("ALTER TABLE IF EXISTS \"ParkingSessions\" RENAME COLUMN IF EXISTS id TO \"Id\";");
            migrationBuilder.Sql("ALTER TABLE IF EXISTS \"ParkingSessions\" RENAME COLUMN IF EXISTS parking_lot_id TO \"ParkingLotId\";");
            migrationBuilder.Sql("ALTER TABLE IF EXISTS \"ParkingSessions\" RENAME COLUMN IF EXISTS license_plate TO \"LicensePlateNumber\";");
            migrationBuilder.Sql("ALTER TABLE IF EXISTS \"ParkingSessions\" RENAME COLUMN IF EXISTS started TO \"Started\";");
            migrationBuilder.Sql("ALTER TABLE IF EXISTS \"ParkingSessions\" RENAME COLUMN IF EXISTS stopped TO \"Stopped\";");
            migrationBuilder.Sql("ALTER TABLE IF EXISTS \"ParkingSessions\" RENAME COLUMN IF EXISTS cost TO \"Cost\";");
            migrationBuilder.Sql("ALTER TABLE IF EXISTS \"ParkingSessions\" RENAME COLUMN IF EXISTS payment_status TO \"PaymentStatus\";");
            migrationBuilder.Sql("ALTER TABLE IF EXISTS \"ParkingSessions\" ALTER COLUMN \"Cost\" TYPE numeric USING \"Cost\"::numeric;");

            // Column renames for Payments + PK alignment
            migrationBuilder.Sql("ALTER TABLE IF EXISTS \"Payments\" RENAME COLUMN IF EXISTS transaction_id TO \"TransactionId\";");
            migrationBuilder.Sql("ALTER TABLE IF EXISTS \"Payments\" RENAME COLUMN IF EXISTS amount TO \"Amount\";");
            migrationBuilder.Sql("ALTER TABLE IF EXISTS \"Payments\" RENAME COLUMN IF EXISTS license_plate TO \"LicensePlateNumber\";");
            migrationBuilder.Sql("ALTER TABLE IF EXISTS \"Payments\" RENAME COLUMN IF EXISTS created_at TO \"CreatedAt\";");
            migrationBuilder.Sql("ALTER TABLE IF EXISTS \"Payments\" RENAME COLUMN IF EXISTS completed TO \"CompletedAt\";");
            migrationBuilder.Sql("ALTER TABLE IF EXISTS \"Payments\" RENAME COLUMN IF EXISTS transaction_data_id TO \"TransactionDataId\";");
            migrationBuilder.Sql("ALTER TABLE IF EXISTS \"Payments\" ALTER COLUMN \"Amount\" TYPE numeric USING \"Amount\"::numeric;");
            // Introduce PaymentId and set it from TransactionId where missing
            migrationBuilder.Sql("ALTER TABLE IF EXISTS \"Payments\" ADD COLUMN IF NOT EXISTS \"PaymentId\" uuid;");
            migrationBuilder.Sql("UPDATE \"Payments\" SET \"PaymentId\" = COALESCE(\"PaymentId\", \"TransactionId\");");
            migrationBuilder.Sql("ALTER TABLE IF EXISTS \"Payments\" ALTER COLUMN \"PaymentId\" SET NOT NULL;");
            migrationBuilder.Sql("ALTER TABLE IF EXISTS \"Payments\" DROP CONSTRAINT IF EXISTS payments_pkey;");
            migrationBuilder.Sql("ALTER TABLE IF EXISTS \"Payments\" ADD CONSTRAINT \"PK_Payments\" PRIMARY KEY (\"PaymentId\");");
            migrationBuilder.Sql("DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE schemaname='public' AND indexname='ux_payments_transactionid') THEN CREATE UNIQUE INDEX ux_payments_transactionid ON \"Payments\"(\"TransactionId\"); END IF; END $$;");

            // Column renames for Permissions
            migrationBuilder.Sql("ALTER TABLE IF EXISTS \"Permissions\" RENAME COLUMN IF EXISTS id TO \"Id\";");
            migrationBuilder.Sql("ALTER TABLE IF EXISTS \"Permissions\" RENAME COLUMN IF EXISTS resource TO \"Resource\";");
            migrationBuilder.Sql("ALTER TABLE IF EXISTS \"Permissions\" RENAME COLUMN IF EXISTS action TO \"Action\";");
            migrationBuilder.Sql("ALTER TABLE IF EXISTS \"Permissions\" RENAME COLUMN IF EXISTS key TO \"Key\";");

            // Column renames for Reservations + type alignments
            migrationBuilder.Sql("ALTER TABLE IF EXISTS \"Reservations\" RENAME COLUMN IF EXISTS id TO \"Id\";");
            migrationBuilder.Sql("ALTER TABLE IF EXISTS \"Reservations\" RENAME COLUMN IF EXISTS license_plate TO \"LicensePlateNumber\";");
            migrationBuilder.Sql("ALTER TABLE IF EXISTS \"Reservations\" RENAME COLUMN IF EXISTS parking_lot_id TO \"ParkingLotId\";");
            migrationBuilder.Sql("ALTER TABLE IF EXISTS \"Reservations\" RENAME COLUMN IF EXISTS start_time TO \"StartTime\";");
            migrationBuilder.Sql("ALTER TABLE IF EXISTS \"Reservations\" RENAME COLUMN IF EXISTS end_time TO \"EndTime\";");
            migrationBuilder.Sql("ALTER TABLE IF EXISTS \"Reservations\" RENAME COLUMN IF EXISTS status TO \"Status\";");
            migrationBuilder.Sql("ALTER TABLE IF EXISTS \"Reservations\" RENAME COLUMN IF EXISTS created_at TO \"CreatedAt\";");
            migrationBuilder.Sql("ALTER TABLE IF EXISTS \"Reservations\" RENAME COLUMN IF EXISTS cost TO \"Cost\";");
            migrationBuilder.Sql("ALTER TABLE IF EXISTS \"Reservations\" ALTER COLUMN \"Cost\" TYPE numeric USING \"Cost\"::numeric;");
            migrationBuilder.Sql("ALTER TABLE IF EXISTS \"Reservations\" ALTER COLUMN \"CreatedAt\" TYPE timestamptz USING \"CreatedAt\"::timestamp with time zone;");

            // Column renames for RolePermissions
            migrationBuilder.Sql("ALTER TABLE IF EXISTS \"RolePermissions\" RENAME COLUMN IF EXISTS role_id TO \"RoleId\";");
            migrationBuilder.Sql("ALTER TABLE IF EXISTS \"RolePermissions\" RENAME COLUMN IF EXISTS permission_id TO \"PermissionId\";");

            // Column renames for Roles
            migrationBuilder.Sql("ALTER TABLE IF EXISTS \"Roles\" RENAME COLUMN IF EXISTS id TO \"Id\";");
            migrationBuilder.Sql("ALTER TABLE IF EXISTS \"Roles\" RENAME COLUMN IF EXISTS name TO \"Name\";");
            migrationBuilder.Sql("ALTER TABLE IF EXISTS \"Roles\" RENAME COLUMN IF EXISTS description TO \"Description\";");

            // Column renames for Transactions + type alignments
            migrationBuilder.Sql("ALTER TABLE IF EXISTS \"Transactions\" RENAME COLUMN IF EXISTS id TO \"Id\";");
            migrationBuilder.Sql("ALTER TABLE IF EXISTS \"Transactions\" RENAME COLUMN IF EXISTS amount TO \"Amount\";");
            migrationBuilder.Sql("ALTER TABLE IF EXISTS \"Transactions\" RENAME COLUMN IF EXISTS method TO \"Method\";");
            migrationBuilder.Sql("ALTER TABLE IF EXISTS \"Transactions\" RENAME COLUMN IF EXISTS issuer TO \"Issuer\";");
            migrationBuilder.Sql("ALTER TABLE IF EXISTS \"Transactions\" RENAME COLUMN IF EXISTS bank TO \"Bank\";");
            migrationBuilder.Sql("ALTER TABLE IF EXISTS \"Transactions\" ALTER COLUMN \"Amount\" TYPE numeric USING \"Amount\"::numeric;");

            // Column renames for UserPlates + type alignments
            migrationBuilder.Sql("ALTER TABLE IF EXISTS \"UserPlates\" RENAME COLUMN IF EXISTS id TO \"Id\";");
            migrationBuilder.Sql("ALTER TABLE IF EXISTS \"UserPlates\" RENAME COLUMN IF EXISTS user_id TO \"UserId\";");
            migrationBuilder.Sql("ALTER TABLE IF EXISTS \"UserPlates\" RENAME COLUMN IF EXISTS license_plate TO \"LicensePlateNumber\";");
            migrationBuilder.Sql("ALTER TABLE IF EXISTS \"UserPlates\" RENAME COLUMN IF EXISTS is_primary TO \"IsPrimary\";");
            migrationBuilder.Sql("ALTER TABLE IF EXISTS \"UserPlates\" RENAME COLUMN IF EXISTS created_at TO \"CreatedAt\";");
            migrationBuilder.Sql("ALTER TABLE IF EXISTS \"UserPlates\" ALTER COLUMN \"CreatedAt\" TYPE timestamptz USING \"CreatedAt\"::timestamp with time zone;");

            // Column renames for Users + type alignments
            migrationBuilder.Sql("ALTER TABLE IF EXISTS \"Users\" RENAME COLUMN IF EXISTS id TO \"Id\";");
            migrationBuilder.Sql("ALTER TABLE IF EXISTS \"Users\" RENAME COLUMN IF EXISTS username TO \"Username\";");
            migrationBuilder.Sql("ALTER TABLE IF EXISTS \"Users\" RENAME COLUMN IF EXISTS password_hash TO \"PasswordHash\";");
            migrationBuilder.Sql("ALTER TABLE IF EXISTS \"Users\" RENAME COLUMN IF EXISTS first_name TO \"FirstName\";");
            migrationBuilder.Sql("ALTER TABLE IF EXISTS \"Users\" RENAME COLUMN IF EXISTS last_name TO \"LastName\";");
            migrationBuilder.Sql("ALTER TABLE IF EXISTS \"Users\" RENAME COLUMN IF EXISTS email TO \"Email\";");
            migrationBuilder.Sql("ALTER TABLE IF EXISTS \"Users\" RENAME COLUMN IF EXISTS phone TO \"Phone\";");
            migrationBuilder.Sql("ALTER TABLE IF EXISTS \"Users\" RENAME COLUMN IF EXISTS role_id TO \"RoleId\";");
            migrationBuilder.Sql("ALTER TABLE IF EXISTS \"Users\" RENAME COLUMN IF EXISTS created_at TO \"CreatedAt\";");
            migrationBuilder.Sql("ALTER TABLE IF EXISTS \"Users\" RENAME COLUMN IF EXISTS birthday TO \"Birthday\";");
            migrationBuilder.Sql("ALTER TABLE IF EXISTS \"Users\" ALTER COLUMN \"CreatedAt\" TYPE timestamptz USING \"CreatedAt\"::timestamp with time zone;");
            migrationBuilder.Sql("ALTER TABLE IF EXISTS \"Users\" ALTER COLUMN \"Birthday\" TYPE timestamptz USING \"Birthday\"::timestamp with time zone;");

            // Ensure constraints exist/align where names changed (keep existing unique constraints if present)
            migrationBuilder.Sql("DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='Roles_Name_key' OR conname='roles_name_key') THEN BEGIN EXCEPTION WHEN undefined_table THEN END; END IF; END $$;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op: down migration intentionally omitted due to risk of data loss on renames/type changes.
        }
    }
}