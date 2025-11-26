using Microsoft.EntityFrameworkCore;

namespace MobyPark.Models.DbContext;

public class AppDbContext : Microsoft.EntityFrameworkCore.DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<UserModel> Users => Set<UserModel>();
    public DbSet<RoleModel> Roles => Set<RoleModel>();
    public DbSet<PermissionModel> Permissions => Set<PermissionModel>();
    public DbSet<RolePermissionModel> RolePermissions => Set<RolePermissionModel>();
    public DbSet<ReservationModel> Reservations => Set<ReservationModel>();
    public DbSet<ParkingLotModel> ParkingLots => Set<ParkingLotModel>();
    public DbSet<LicensePlateModel> LicensePlates => Set<LicensePlateModel>();
    public DbSet<ParkingSessionModel> ParkingSessions => Set<ParkingSessionModel>();
    public DbSet<PaymentModel> Payments => Set<PaymentModel>();
    public DbSet<TransactionModel> Transactions => Set<TransactionModel>();
    public DbSet<UserPlateModel> UserPlates => Set<UserPlateModel>();
    public DbSet<HotelPassModel> HotelPasses => Set<HotelPassModel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Map C# enum to Postgres enum
        modelBuilder.HasPostgresEnum<ParkingLotStatus>();

        modelBuilder.Entity<ParkingLotModel>()
            .Property(parkingLot => parkingLot.Status)
            .HasColumnType("parking_lot_status");

        modelBuilder.HasPostgresEnum<ReservationStatus>();

        modelBuilder.Entity<ReservationModel>()
            .Property(reservation => reservation.Status)
            .HasColumnType("reservation_status");

        modelBuilder.HasPostgresEnum<ParkingSessionStatus>();
        modelBuilder.Entity<ParkingSessionModel>()
            .Property(session => session.PaymentStatus)
            .HasColumnType("payment_status");

        // RolePermission composite key
        modelBuilder.Entity<RolePermissionModel>()
            .HasKey(rolePermission => new { rolePermission.RoleId, rolePermission.PermissionId });

        // Relationships
        modelBuilder.Entity<RolePermissionModel>()
            .HasOne(rolePermission => rolePermission.Role)
            .WithMany(role => role.RolePermissions)
            .HasForeignKey(rolePermission => rolePermission.RoleId);

        modelBuilder.Entity<RolePermissionModel>()
            .HasOne(rolePermissions => rolePermissions.Permission)
            .WithMany(permission => permission.RolePermissions)
            .HasForeignKey(rolePermissions => rolePermissions.PermissionId);

        modelBuilder.Entity<UserModel>()
            .HasOne(user => user.Role)
            .WithMany(role => role.Users)
            .HasForeignKey(user => user.RoleId);

        modelBuilder.Entity<ReservationModel>()
            .HasOne(reservation => reservation.ParkingLot)
            .WithMany(parkingLot => parkingLot.Reservations)
            .HasForeignKey(reservation => reservation.ParkingLotId);

        modelBuilder.Entity<ParkingSessionModel>()
            .HasOne(parkingSession => parkingSession.ParkingLot)
            .WithMany()
            .HasForeignKey(parkingSession => parkingSession.ParkingLotId);

        modelBuilder.Entity<ParkingSessionModel>()
            .HasOne(parkingSession => parkingSession.LicensePlate)
            .WithMany()
            .HasForeignKey(parkingSession => parkingSession.LicensePlateNumber);

        modelBuilder.Entity<PaymentModel>()
            .HasOne(payment => payment.LicensePlate)
            .WithMany()
            .HasForeignKey(payment => payment.LicensePlateNumber);

        modelBuilder.Entity<PaymentModel>()
            .HasOne(payment => payment.Transaction)
            .WithMany()
            .HasForeignKey(payment => payment.TransactionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserPlateModel>()
            .HasOne(userPlate => userPlate.User)
            .WithMany()
            .HasForeignKey(userPlate => userPlate.UserId);

        modelBuilder.Entity<UserPlateModel>()
            .HasOne(userPlate => userPlate.LicensePlate)
            .WithMany()
            .HasForeignKey(userPlate => userPlate.LicensePlateNumber);

    }
}