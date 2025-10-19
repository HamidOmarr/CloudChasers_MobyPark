using MobyPark.Models;

namespace MobyPark.Services.Services;

public static class Validator
{
    public static void LicensePlate(LicensePlateModel licensePlate)
    {
        ArgumentNullException.ThrowIfNull(licensePlate, nameof(licensePlate));
        ArgumentNullException.ThrowIfNull(licensePlate.LicensePlateNumber, nameof(licensePlate.LicensePlateNumber));

        if (string.IsNullOrWhiteSpace(licensePlate.LicensePlateNumber))
            throw new ArgumentException("License plate number cannot be empty or whitespace.", nameof(licensePlate.LicensePlateNumber));
    }

    public static void ParkingLot(ParkingLotModel parkingLot)
    {
        ArgumentNullException.ThrowIfNull(parkingLot, nameof(parkingLot));

        var properties = new Dictionary<string, object?>
        {
            { nameof(parkingLot.Name), parkingLot.Name },
            { nameof(parkingLot.Location), parkingLot.Location },
            { nameof(parkingLot.Address), parkingLot.Address }
        };

        foreach (var prop in properties)
            ArgumentNullException.ThrowIfNull(prop.Value, prop.Key);

        if (parkingLot.Capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(parkingLot.Capacity), "Capacity must be greater than 0.");
        if (parkingLot.Tariff < 0)
            throw new ArgumentOutOfRangeException(nameof(parkingLot.Tariff), "Tariff cannot be negative.");
        if (parkingLot.DayTariff < 0)
            throw new ArgumentOutOfRangeException(nameof(parkingLot.DayTariff), "Day tariff cannot be negative.");

        if (parkingLot.CreatedAt == default)
            parkingLot.CreatedAt = DateOnly.FromDateTime(DateTime.UtcNow);
    }

    public static void ParkingSession(ParkingSessionModel session)
    {
        ArgumentNullException.ThrowIfNull(session, nameof(session));

        var properties = new Dictionary<string, object?>
        {
            { nameof(session.ParkingLotId), session.ParkingLotId },
            { nameof(session.LicensePlateNumber), session.LicensePlateNumber }
        };

        foreach (var prop in properties)
            ArgumentNullException.ThrowIfNull(prop.Value, prop.Key);

        if (string.IsNullOrWhiteSpace(session.LicensePlateNumber))
            throw new ArgumentException("License plate number cannot be empty or whitespace.", nameof(session.LicensePlateNumber));
        if (session.ParkingLotId <= 0)
            throw new ArgumentOutOfRangeException(nameof(session.ParkingLotId), "Parking lot ID must exist.");
        if (session.Started == default)
            session.Started = DateTime.UtcNow;
    }

    public static void Payment(PaymentModel payment)
    {
        ArgumentNullException.ThrowIfNull(payment, nameof(payment));

        var properties = new Dictionary<string, object?>
        {
            { nameof (payment.PaymentId), payment.PaymentId },
            { nameof(payment.Amount), payment.Amount },
            { nameof(payment.LicensePlateNumber), payment.LicensePlateNumber },
            { nameof(payment.TransactionDataId), payment.TransactionDataId }
        };

        foreach (var prop in properties)
            ArgumentNullException.ThrowIfNull(prop.Value, prop.Key);

        if (string.IsNullOrWhiteSpace(payment.LicensePlateNumber))
            throw new ArgumentException("License plate number cannot be empty or whitespace.", nameof(payment.LicensePlateNumber));
        if (payment.Amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(payment.Amount), "Amount must be greater than 0.");
        if (payment.TransactionDataId == Guid.Empty)
            throw new ArgumentException("Transaction data ID must be a valid GUID.", nameof(payment.TransactionDataId));

        if (payment.CreatedAt == default)
            payment.CreatedAt = DateTime.UtcNow;
    }

    public static void Permission(PermissionModel permission)
    {
        ArgumentNullException.ThrowIfNull(permission, nameof(permission));

        var properties = new Dictionary<string, object?>
        {
            { nameof(permission.Resource), permission.Resource },
            { nameof(permission.Action), permission.Action }
        };

        foreach (var prop in properties)
            ArgumentNullException.ThrowIfNull(prop.Value, prop.Key);

        if (string.IsNullOrWhiteSpace(permission.Resource))
            throw new ArgumentException("Resource cannot be empty or whitespace.", nameof(permission.Resource));
        if (string.IsNullOrWhiteSpace(permission.Action))
            throw new ArgumentException("Action cannot be empty or whitespace.", nameof(permission.Action));
    }

    public static void Reservation(ReservationModel reservation)
    {
        ArgumentNullException.ThrowIfNull(reservation, nameof(reservation));

        var properties = new Dictionary<string, object?>
        {
            { nameof(reservation.LicensePlateNumber), reservation.LicensePlateNumber },
            { nameof(reservation.ParkingLotId), reservation.ParkingLotId },
            { nameof(reservation.StartTime), reservation.StartTime },
            { nameof(reservation.EndTime), reservation.EndTime }
        };

        foreach (var prop in properties)
            ArgumentNullException.ThrowIfNull(prop.Value, prop.Key);

        if (string.IsNullOrWhiteSpace(reservation.LicensePlateNumber))
            throw new ArgumentException("License plate number cannot be empty or whitespace.", nameof(reservation.LicensePlateNumber));
        if (reservation.ParkingLotId <= 0)
            throw new ArgumentOutOfRangeException(nameof(reservation.ParkingLotId), "Parking lot ID must exist.");
        if (reservation.StartTime >= reservation.EndTime)
            throw new ArgumentException("Start time must be earlier than end time.", nameof(reservation.StartTime));

        if (reservation.Status == default)
            reservation.Status = ReservationStatus.Pending;

        if (reservation.CreatedAt == default)
            reservation.CreatedAt = DateTime.UtcNow;

        if (reservation.Cost < 0)
            throw new ArgumentOutOfRangeException(nameof(reservation.Cost), "Cost cannot be negative.");
    }

    public static void RolePermission(RolePermissionModel rolePermission)
    {
        ArgumentNullException.ThrowIfNull(rolePermission, nameof(rolePermission));

        if (rolePermission.RoleId <= 0)
            throw new ArgumentOutOfRangeException(nameof(rolePermission.RoleId), "Role ID must exist.");
        if (rolePermission.PermissionId <= 0)
            throw new ArgumentOutOfRangeException(nameof(rolePermission.PermissionId), "Permission ID must exist.");
    }

    public static void Role(RoleModel role)
    {
        ArgumentNullException.ThrowIfNull(role, nameof(role));

        if (string.IsNullOrWhiteSpace(role.Name))
            throw new ArgumentException("Role name cannot be empty or whitespace.", nameof(role.Name));
    }

    public static void Transaction(TransactionModel transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction, nameof(transaction));

        var properties = new Dictionary<string, object?>
        {
            { nameof(transaction.Amount), transaction.Amount },
            { nameof(transaction.Method), transaction.Method },
            { nameof(transaction.Issuer), transaction.Issuer },
            { nameof(transaction.Bank), transaction.Bank }
        };

        foreach (var prop in properties)
            ArgumentNullException.ThrowIfNull(prop.Value, prop.Key);

        if (transaction.Amount < 0)
            throw new ArgumentOutOfRangeException(nameof(transaction.Amount), "Amount cannot be negative.");
    }

    public static void UserPlate(UserPlateModel userPlate)
    {
        ArgumentNullException.ThrowIfNull(userPlate, nameof(userPlate));

        var properties = new Dictionary<string, object?>
        {
            { nameof(userPlate.UserId), userPlate.UserId },
            { nameof(userPlate.LicensePlateNumber), userPlate.LicensePlateNumber }
        };

        foreach (var prop in properties)
            ArgumentNullException.ThrowIfNull(prop.Value, prop.Key);

        if (userPlate.UserId == UserPlateModel.DefaultUserId)
            userPlate.IsPrimary = false;

        if (userPlate.CreatedAt == default)
            userPlate.CreatedAt = DateOnly.FromDateTime(DateTime.UtcNow);
    }

    public static void User(UserModel user)
    {
        ArgumentNullException.ThrowIfNull(user, nameof(user));

        var properties = new Dictionary<string, object?>
        {
            { nameof(user.Username), user.Username },
            { nameof(user.PasswordHash), user.PasswordHash },
            { nameof(user.FirstName), user.FirstName },
            { nameof(user.LastName), user.LastName },
            { nameof(user.Email), user.Email },
            { nameof(user.Phone), user.Phone },
            { nameof(user.Birthday), user.Birthday }
        };

        foreach (var prop in properties)
            ArgumentNullException.ThrowIfNull(prop.Value, prop.Key);

        if (user.CreatedAt == default)
            user.CreatedAt = DateOnly.FromDateTime(DateTime.UtcNow);

        if (user.RoleId <= 0)
            user.RoleId = UserModel.DefaultUserRoleId;
    }
}