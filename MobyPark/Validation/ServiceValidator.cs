// using MobyPark.Models;
//
// namespace MobyPark.Validation;
//
// public static class ServiceValidator
// {
//     // License plate validation
//     public static void LicensePlate(LicensePlateModel licensePlate)
//     {
//         ValHelper.ThrowIfNull(licensePlate, nameof(licensePlate));
//         ValHelper.ThrowIfNullOrWhiteSpace(licensePlate.LicensePlateNumber, nameof(licensePlate.LicensePlateNumber));
//     }
//
//     // Parking lot validation
//     public static void ParkingLot(ParkingLotModel parkingLot)
//     {
//         ValHelper.ThrowIfNull(parkingLot, nameof(parkingLot));
//
//         ValHelper.ThrowIfNullOrWhiteSpace(parkingLot.Name, nameof(parkingLot.Name));
//         ValHelper.ThrowIfNullOrWhiteSpace(parkingLot.Location, nameof(parkingLot.Location));
//         ValHelper.ThrowIfNullOrWhiteSpace(parkingLot.Address, nameof(parkingLot.Address));
//
//         ValHelper.ThrowIfNotPositive(parkingLot.Capacity, nameof(parkingLot.Capacity));
//         ValHelper.ThrowIfNegative(parkingLot.Tariff, nameof(parkingLot.Tariff));
//         if (parkingLot.DayTariff is not null) ValHelper.ThrowIfNegative(parkingLot.DayTariff.Value, nameof(parkingLot.DayTariff));
//
//         parkingLot.CreatedAt = ValHelper.EnsureCreatedAt(parkingLot.CreatedAt);
//     }
//
//     // Parking session validation
//     public static void ParkingSession(ParkingSessionModel session)
//     {
//         ValHelper.ThrowIfNull(session, nameof(session));
//
//         ValHelper.ThrowIfNotPositive(session.ParkingLotId, nameof(session.ParkingLotId));
//         ValHelper.ThrowIfNullOrWhiteSpace(session.LicensePlateNumber, nameof(session.LicensePlateNumber));
//
//         if (session.Started == default)
//             session.Started = DateTime.UtcNow;
//     }
//
//     // Payment validation
//     public static void Payment(PaymentModel payment)
//     {
//         ValHelper.ThrowIfNull(payment, nameof(payment));
//
//         ValHelper.ThrowIfNullOrWhiteSpace(payment.LicensePlateNumber, nameof(payment.LicensePlateNumber));
//         ValHelper.ThrowIfNegative(payment.Amount, nameof(payment.Amount));
//
//         if (payment.TransactionDataId == Guid.Empty)
//             throw new ArgumentException("Transaction data ID must be a valid GUID.", nameof(payment.TransactionDataId));
//
//         payment.CreatedAt = ValHelper.EnsureCreatedAt(payment.CreatedAt);
//     }
//
//     // Permission validation
//     public static void Permission(PermissionModel permission)
//     {
//         ValHelper.ThrowIfNull(permission, nameof(permission));
//         ValHelper.ThrowIfNullOrWhiteSpace(permission.Resource, nameof(permission.Resource));
//         ValHelper.ThrowIfNullOrWhiteSpace(permission.Action, nameof(permission.Action));
//     }
//
//     // Reservation validation
//     public static void Reservation(ReservationModel reservation)
//     {
//         ValHelper.ThrowIfNull(reservation, nameof(reservation));
//         ValHelper.ThrowIfNotPositive(reservation.ParkingLotId, nameof(reservation.ParkingLotId));
//         ValHelper.ThrowIfNullOrWhiteSpace(reservation.LicensePlateNumber, nameof(reservation.LicensePlateNumber));
//
//         if (reservation.StartTime >= reservation.EndTime)
//             throw new ArgumentException("Start time must be earlier than end time.", nameof(reservation.StartTime));
//
//         if (reservation.Status == default)
//             reservation.Status = ReservationStatus.Pending;
//
//         reservation.CreatedAt = ValHelper.EnsureCreatedAt(reservation.CreatedAt);
//         ValHelper.ThrowIfNegative(reservation.Cost, nameof(reservation.Cost));
//     }
//
//     // Role permission validation
//     public static void RolePermission(RolePermissionModel rolePermission)
//     {
//         ValHelper.ThrowIfNull(rolePermission, nameof(rolePermission));
//         ValHelper.ThrowIfNotPositive(rolePermission.RoleId, nameof(rolePermission.RoleId));
//         ValHelper.ThrowIfNotPositive(rolePermission.PermissionId, nameof(rolePermission.PermissionId));
//     }
//
//     // Role validation
//     public static void Role(RoleModel role)
//     {
//         ValHelper.ThrowIfNull(role, nameof(role));
//         ValHelper.ThrowIfNullOrWhiteSpace(role.Name, nameof(role.Name));
//     }
//
//     // Transaction validation
//     public static void Transaction(TransactionModel transaction)
//     {
//         ValHelper.ThrowIfNull(transaction, nameof(transaction));
//
//         ValHelper.ThrowIfNegative(transaction.Amount, nameof(transaction.Amount));
//         ValHelper.ThrowIfNullOrWhiteSpace(transaction.Method, nameof(transaction.Method));
//         ValHelper.ThrowIfNullOrWhiteSpace(transaction.Issuer, nameof(transaction.Issuer));
//         ValHelper.ThrowIfNullOrWhiteSpace(transaction.Bank, nameof(transaction.Bank));
//     }
//
//     // User plate validation
//     public static void UserPlate(UserPlateModel userPlate)
//     {
//         ValHelper.ThrowIfNull(userPlate, nameof(userPlate));
//         ValHelper.ThrowIfNotPositive(userPlate.UserId, nameof(userPlate.UserId));
//         ValHelper.ThrowIfNullOrWhiteSpace(userPlate.LicensePlateNumber, nameof(userPlate.LicensePlateNumber));
//
//         if (userPlate.UserId == UserPlateModel.DefaultUserId)
//             userPlate.IsPrimary = false;
//
//         userPlate.CreatedAt = ValHelper.EnsureCreatedAt(userPlate.CreatedAt);
//     }
//
//     // User validation
     // public static void User(UserModel user)
     // {
     //     ValHelper.ThrowIfNull(user, nameof(user));
     //
     //     ValHelper.ThrowIfNullOrWhiteSpace(user.Username, nameof(user.Username));
     //     ValHelper.ThrowIfNullOrWhiteSpace(user.PasswordHash, nameof(user.PasswordHash));
     //     ValHelper.ThrowIfNullOrWhiteSpace(user.FirstName, nameof(user.FirstName));
     //     ValHelper.ThrowIfNullOrWhiteSpace(user.LastName, nameof(user.LastName));
     //     ValHelper.ThrowIfNullOrWhiteSpace(user.Email, nameof(user.Email));
     //     ValHelper.ThrowIfNullOrWhiteSpace(user.Phone, nameof(user.Phone));
     //
     //     if (user.CreatedAt == default)
     //         user.CreatedAt = DateOnly.FromDateTime(DateTime.UtcNow);
     //
     //     if (user.RoleId <= 0)
     //         user.RoleId = UserModel.DefaultUserRoleId;
     // }
// }
