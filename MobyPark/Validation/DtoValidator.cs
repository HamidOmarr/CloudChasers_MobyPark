// using MobyPark.DTOs.ParkingSession.Request;
// using MobyPark.DTOs.User.Request;
//
// namespace MobyPark.Validation;
//
// public static class DtoValidator
// {
//     public static void Register(RegisterDto registerDto)
//     {
//         ValHelper.ThrowIfNull(registerDto, nameof(registerDto));
//         ValHelper.ThrowIfNullOrWhiteSpace(registerDto.Email, nameof(registerDto.Email));
//         ValHelper.ThrowIfNullOrWhiteSpace(registerDto.Username, nameof(registerDto.Username));
//         ValHelper.ThrowIfNullOrWhiteSpace(registerDto.Password, nameof(registerDto.Password));
//         ValHelper.ThrowIfNullOrWhiteSpace(registerDto.FirstName, nameof(registerDto.FirstName));
//         ValHelper.ThrowIfNullOrWhiteSpace(registerDto.LastName, nameof(registerDto.LastName));
//         ValHelper.ThrowIfNullOrWhiteSpace(registerDto.Phone, nameof(registerDto.Phone));
//
//         if (registerDto.Birthday == default)
//         { throw new ArgumentException("Birthday is required.", nameof(registerDto.Birthday)); }
//
//         if (registerDto.Birthday > DateOnly.FromDateTime(DateTime.Now))
//         { throw new ArgumentException("Birthday cannot be in the future.", nameof(registerDto.Birthday)); }
//
//         if (registerDto.Birthday.Year < 1900)
//         { throw new ArgumentException("Birthday is not valid.", nameof(registerDto.Birthday)); }
//
//         var today = DateOnly.FromDateTime(DateTime.Now);
//         var age = today.Year - registerDto.Birthday.Year;
//         if (registerDto.Birthday > today.AddYears(-age)) age--;
//         if (age < 16) throw new ArgumentException("User must be at least 16 years old to register.", nameof(registerDto.Birthday));
//
//         if (string.IsNullOrWhiteSpace(registerDto.LicensePlate)) return;
//
//         registerDto.LicensePlate = ValHelper.NormalizePlate(registerDto.LicensePlate);
//         if (registerDto.LicensePlate.Length < 6)
//             throw new ArgumentException("License plate is too short.", nameof(registerDto.LicensePlate));
//     }
//
//     public static void UpdateProfile(UpdateProfileDto updateDto)
//     {
//         ValHelper.ThrowIfNull(updateDto, nameof(updateDto));
//
//         if (updateDto.Username is not null)
//             ValHelper.ThrowIfNullOrWhiteSpace(updateDto.Username, nameof(updateDto.Username));
//         if (updateDto.Password is not null)
//             ValHelper.ThrowIfNullOrWhiteSpace(updateDto.Password, nameof(updateDto.Password));
//         if (updateDto.FirstName is not null)
//             ValHelper.ThrowIfNullOrWhiteSpace(updateDto.FirstName, nameof(updateDto.FirstName));
//         if (updateDto.LastName is not null)
//             ValHelper.ThrowIfNullOrWhiteSpace(updateDto.LastName, nameof(updateDto.LastName));
//         if (updateDto.Email is not null)
//             ValHelper.ThrowIfNullOrWhiteSpace(updateDto.Email, nameof(updateDto.Email));
//         if (updateDto.Phone is not null)
//             ValHelper.ThrowIfNullOrWhiteSpace(updateDto.Phone, nameof(updateDto.Phone));
//         if (updateDto.Birthday is null) return;
//
//         if (updateDto.Birthday > DateOnly.FromDateTime(DateTime.Now))
//             throw new ArgumentException("Birthday cannot be in the future.", nameof(updateDto.Birthday));
//         if (updateDto.Birthday.Value.Year < 1900)
//             throw new ArgumentException("Birthday is not valid.", nameof(updateDto.Birthday));
//
//         var today = DateOnly.FromDateTime(DateTime.Now);
//         var age = today.Year - updateDto.Birthday.Value.Year;
//         if (updateDto.Birthday > today.AddYears(-age)) age--;
//         if (age < 16) throw new ArgumentException("User must be at least 16 years old to register.", nameof(updateDto.Birthday));
//     }
//
//     public static void ParkingSessionCreate(ParkingSessionCreateDto session)
//     {
//         ValHelper.ThrowIfNotPositive(session.ParkingLotId, nameof(session.ParkingLotId));
//         ValHelper.ThrowIfNullOrWhiteSpace(session.LicensePlate, nameof(session.LicensePlate));
//     }
// }