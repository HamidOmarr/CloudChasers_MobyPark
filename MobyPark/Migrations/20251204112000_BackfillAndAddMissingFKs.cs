using Microsoft.EntityFrameworkCore.Migrations;

namespace MobyPark.Migrations
{
    public partial class BackfillAndAddMissingFKs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Backfill LicensePlates from ParkingSessions
            migrationBuilder.Sql(@"
INSERT INTO ""LicensePlates"" (""LicensePlateNumber"")
SELECT DISTINCT ps.""LicensePlateNumber""
FROM ""ParkingSessions"" ps
LEFT JOIN ""LicensePlates"" lp ON ps.""LicensePlateNumber"" = lp.""LicensePlateNumber""
WHERE lp.""LicensePlateNumber"" IS NULL;
");

            // Fix UserPlates sequence to current max Id (if sequence exists)
            migrationBuilder.Sql(@"
DO $$ BEGIN
IF EXISTS (SELECT 1 FROM information_schema.sequences WHERE sequence_schema='public' AND sequence_name='user_plates_id_seq') THEN
    PERFORM setval('public.user_plates_id_seq', (SELECT COALESCE(MAX(""Id""), 1) FROM ""UserPlates""), TRUE);
END IF;
END $$;
");

            // Backfill placeholder UserPlates for plates without user link
            migrationBuilder.Sql(@"
INSERT INTO ""UserPlates"" (""UserId"", ""LicensePlateNumber"", ""IsPrimary"", ""CreatedAt"")
SELECT -1 AS ""UserId"",
       lp.""LicensePlateNumber"",
       FALSE AS ""IsPrimary"",
       NOW() AT TIME ZONE 'UTC' AS ""CreatedAt""
FROM ""LicensePlates"" lp
LEFT JOIN ""UserPlates"" up ON lp.""LicensePlateNumber"" = up.""LicensePlateNumber""
WHERE up.""LicensePlateNumber"" IS NULL;
");

            // Ensure missing FKs (ParkingSessions -> LicensePlates, UserPlates -> LicensePlates) with CASCADE
            migrationBuilder.Sql(@"
DO $$ BEGIN
IF NOT EXISTS (
    SELECT 1 FROM pg_constraint WHERE conname = 'FK_ParkingSessions_LicensePlates_LicensePlateNumber'
) THEN
    ALTER TABLE ONLY ""ParkingSessions""
        ADD CONSTRAINT ""FK_ParkingSessions_LicensePlates_LicensePlateNumber""
        FOREIGN KEY (""LicensePlateNumber"")
        REFERENCES ""LicensePlates""(""LicensePlateNumber"")
        ON DELETE CASCADE;
END IF;
END $$;

DO $$ BEGIN
IF NOT EXISTS (
    SELECT 1 FROM pg_constraint WHERE conname = 'FK_UserPlates_LicensePlates_LicensePlateNumber'
) THEN
    ALTER TABLE ONLY ""UserPlates""
        ADD CONSTRAINT ""FK_UserPlates_LicensePlates_LicensePlateNumber""
        FOREIGN KEY (""LicensePlateNumber"")
        REFERENCES ""LicensePlates""(""LicensePlateNumber"")
        ON DELETE CASCADE;
END IF;
END $$;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No down; safe forward-only data backfill and FK additions.
        }
    }
}