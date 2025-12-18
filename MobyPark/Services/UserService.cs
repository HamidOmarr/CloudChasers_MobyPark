using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using MobyPark.DTOs.Token;
using MobyPark.DTOs.User.Request;
using MobyPark.DTOs.User.Response;
using MobyPark.Models;
using MobyPark.Models.Repositories;
using MobyPark.Models.Repositories.Interfaces;
using MobyPark.Services.Interfaces;
using MobyPark.Services.Results;
using MobyPark.Services.Results.Tokens;
using MobyPark.Services.Results.User;
using MobyPark.Validation;

namespace MobyPark.Services;

public partial class UserService : IUserService
{
    private readonly IUserRepository _users;
    private readonly IUserPlateRepository _userPlates;
    private readonly IParkingSessionRepository _parkingSessions;
    private readonly IRoleRepository _roles;
    private readonly IPasswordHasher<UserModel> _hasher;
    private readonly ITokenService _tokens;
    private readonly IRepository<HotelModel> _hotels;
    private readonly IRepository<BusinessModel> _businesses;

    private const string PasswordPattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*\W).{8,}$";
    private const string PhoneTrimPattern = @"\D";
    private const string PhonePattern = @"^\+?[0-9]+$";
    private const string PhoneDigitPattern = @"^\d{10}$";
    private const string EmailPattern = @"^(?!\.)(?!.*\.\.)[A-Za-z0-9._%+-]+(?<!\.)@(?:(?:xn--)?[A-Za-z0-9](?:[A-Za-z0-9-]{0,61}[A-Za-z0-9])?\.)+[A-Za-z]{2,63}$";

    [GeneratedRegex(PasswordPattern)]
    private static partial Regex PasswordRegex();
    [GeneratedRegex(PhoneTrimPattern)]
    private static partial Regex PhoneTrim();
    [GeneratedRegex(PhonePattern)]
    private static partial Regex Phone();
    [GeneratedRegex(PhoneDigitPattern)]
    private static partial Regex PhoneDigits();
    [GeneratedRegex(EmailPattern)]
    private static partial Regex EmailRegex();

    public UserService(
        IUserRepository users,
        IUserPlateRepository userPlates,
        IParkingSessionRepository parkingSessions,
        IRoleRepository roles,
        IPasswordHasher<UserModel> hasher,
        ITokenService tokens, IRepository<HotelModel> hotels, IRepository<BusinessModel> businesses)
    {
        _users = users;
        _userPlates = userPlates;
        _parkingSessions = parkingSessions;
        _roles = roles;
        _hasher = hasher;
        _tokens = tokens;
        _hotels = hotels;
        _businesses = businesses;
    }

    private async Task<UserModel> CreateUser(UserModel user)
    {
        (bool createdSuccessfully, long id) = await _users.CreateWithId(user);
        if (!createdSuccessfully) throw new InvalidOperationException("Database insertion failed unexpectedly.");

        user.Id = id;
        return user;
    }

    public async Task<GetUserResult> GetUserByUsername(string username)
    {
        var user = await _users.GetByUsername(username);
        if (user is null)
            return new GetUserResult.NotFound();
        return new GetUserResult.Success(user);
    }

    public async Task<GetUserResult> GetUserByEmail(string email)
    {
        var user = await _users.GetByEmail(email);
        if (user is null)
            return new GetUserResult.NotFound();
        return new GetUserResult.Success(user);
    }

    public async Task<GetUserResult> GetUserById(long userId)
    {
        var user = await _users.GetById<UserModel>(userId);
        if (user is null)
            return new GetUserResult.NotFound();
        return new GetUserResult.Success(user);
    }

    public async Task<GetUserListResult> GetAllUsers()
    {
        var users = await _users.GetAll();
        if (users.Count == 0)
            return new GetUserListResult.NotFound();
        return new GetUserListResult.Success(users);
    }

    public async Task<int> CountUsers() => await _users.Count();

    private async Task<UpdateUserResult> UpdateUser(UserModel user)
    {
        var getResult = await GetUserById(user.Id);

        if (getResult is not GetUserResult.Success success)
            return getResult switch
            {
                GetUserResult.NotFound => new UpdateUserResult.NotFound(),
                _ => new UpdateUserResult.Error("Unknown error occurred while retrieving the user.")
            };

        try
        {
            var existingUser = success.User;

            bool updated = await _users.Update(existingUser, user);
            if (!updated)
                return new UpdateUserResult.Error("Database update failed.");
            return new UpdateUserResult.Success(existingUser);
        }
        catch (Exception ex)
        { return new UpdateUserResult.Error(ex.Message); }
    }

    public async Task<DeleteUserResult> DeleteUser(long id)
    {
        var userResult = await GetUserById(id);
        if (userResult is GetUserResult.NotFound)
            return new DeleteUserResult.NotFound();

        var user = ((GetUserResult.Success)userResult).User;

        try
        {
            if (!await _users.Delete(user))
                return new DeleteUserResult.Error("Database deletion failed.");

            return new DeleteUserResult.Success();
        }
        catch (Exception ex)
        { return new DeleteUserResult.Error(ex.Message); }
    }

    public async Task<RegisterResult> CreateUserAsync(RegisterDto dto)
    {
        var userResult = await GetUserByUsername(dto.Username);
        if (userResult is GetUserResult.Success)
            return new RegisterResult.UsernameTaken();

        var emailResult = CleanEmail(dto.Email, out var cleanEmail);
        if (emailResult is RegisterResult.InvalidData invalidEmail)
            return invalidEmail;
        var phoneResult = CleanPhone(dto.Phone, out var cleanPhone);
        if (phoneResult is RegisterResult.InvalidData invalidPhone)
            return invalidPhone;

        if (!PasswordRegex().IsMatch(dto.Password))
            return new RegisterResult.InvalidData("Password does not meet complexity requirements.");

        var user = new UserModel
        {
            Username = dto.Username.Trim(),
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            Email = cleanEmail!,
            Phone = cleanPhone!,
            Birthday = dto.Birthday,
            CreatedAt = DateTimeOffset.UtcNow
        };

        user.PasswordHash = _hasher.HashPassword(user, dto.Password);

        try
        {
            UserModel? createdUser = await CreateUser(user);
            // Reload model to include role and permissions. Needed to create a session with the correct claims.
            createdUser = await _users.GetByIdWithRoleAndPermissions(createdUser.Id);
            if (createdUser is null)
                return new RegisterResult.Error("Failed to create user due to database issue.");

            if (string.IsNullOrWhiteSpace(dto.LicensePlate))
                return new RegisterResult.Success(createdUser);

            var plateResult = await ValidateGuestLicensePlate(createdUser.Id, dto.LicensePlate);
            return plateResult ?? new RegisterResult.Success(createdUser);
        }
        catch (InvalidOperationException)
        { return new RegisterResult.Error("Failed to create user due to database issue."); }
    }

    private async Task<RegisterResult?> ValidateGuestLicensePlate(long userId, string licensePlate)
    {
        if (string.IsNullOrWhiteSpace(licensePlate)) return null;

        licensePlate = licensePlate.Upper();
        var userPlates = await _userPlates.GetPlatesByPlate(licensePlate);

        foreach (var uPlate in userPlates)
        {
            var userResult = await GetUserById(uPlate.UserId);
            if (userResult is GetUserResult.NotFound) continue;

            var user = ((GetUserResult.Success)userResult).User;
            if (user.Id == UserRepository.DeletedUserId) continue;

            var userRecentSessions = await _parkingSessions.GetAllRecentSessionsByLicensePlate(uPlate.LicensePlateNumber, TimeSpan.FromDays(30));
            if (userRecentSessions.Count > 0)
                return new RegisterResult.InvalidData("License plate is already associated with another user.");
        }

        await _userPlates.AddPlateToUser(userId, licensePlate);
        return null;
    }

    public async Task<LoginResult> Login(LoginDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Identifier) || string.IsNullOrWhiteSpace(dto.Password))
            return new LoginResult.Error("Identifier and password are required.");

        // If else to check both email and username. If neither found, user remains null.
        UserModel? user = null;
        var emailResult = await GetUserByEmail(dto.Identifier);
        if (emailResult is GetUserResult.Success sEmail)
            user = sEmail.User;

        else
        {
            var userResult = await GetUserByUsername(dto.Identifier);
            if (userResult is GetUserResult.Success sUser)
                user = sUser.User;
        }

        if (user is null)
            return new LoginResult.InvalidCredentials();

        var verification = _hasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
        if (verification == PasswordVerificationResult.Failed)
            return new LoginResult.InvalidCredentials();
        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _hasher.HashPassword(user, dto.Password);
            _users.Update(user);
        }

        // Reload model to include role and permissions. Needed to create a session with the correct claims.
        // Double check for race condition. If that happens, return invalid credentials to avoid giving too much info.
        user = await _users.GetByIdWithRoleAndPermissions(user.Id);
        if (user is null)
            return new LoginResult.InvalidCredentials();

        var tokenResult = _tokens.CreateToken(user);
        if (tokenResult is not CreateJwtResult.Success success)
            return new LoginResult.Error("Log in failed.");

        var refreshToken = _tokens.GenerateRefreshToken();
        var slidingExpiry = _tokens.GetSlidingTokenExpiryTime();
        var absoluteExpiry = _tokens.GetAbsoluteTokenExpiryTime();

        var updateData = new TokenDto
        {
            RefreshToken = refreshToken,
            SlidingTokenExpiryTime = slidingExpiry,
            AbsoluteTokenExpiryTime = absoluteExpiry
        };

        bool updated = await _users.Update(user, updateData);
        if (!updated)
            return new LoginResult.Error("Log in failed.");

        var response = new AuthDto
        {
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email,
            Token = success.JwtToken,
            RefreshToken = refreshToken
        };

        return new LoginResult.Success(response);
    }

    public async Task<UpdateUserResult> UpdateUserProfile(long userId, UpdateUserDto dto)
    {
        var user = await _users.GetById<UserModel>(userId);
        if (user is null)
            return new UpdateUserResult.NotFound();

        // Username
        if (!string.IsNullOrWhiteSpace(dto.Username))
        {
            var existingResult = await GetUserByUsername(dto.Username);
            if (existingResult is GetUserResult.Success success && success.User.Id != user.Id)
                return new UpdateUserResult.UsernameTaken();

            user.Username = dto.Username.TrimSafe();
        }

        // Password
        if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            var passwordResult = ValidatePasswordIntegrity(dto.Password);
            if (passwordResult is UpdateUserResult.InvalidData)
                return passwordResult;

            user.PasswordHash = _hasher.HashPassword(user, dto.Password);
        }

        // Email
        if (!string.IsNullOrWhiteSpace(dto.Email))
        {
            var emailResult = ValidateAndCleanEmail(dto.Email, out string? cleanEmail);
            if (emailResult is not UpdateUserResult.Success)
                return emailResult;

            var existingEmailResult = await GetUserByEmail(cleanEmail!);
            if (existingEmailResult is GetUserResult.Success success && success.User.Id != user.Id)
                return new UpdateUserResult.EmailTaken();

            user.Email = cleanEmail!;
        }

        // Phone
        if (!string.IsNullOrWhiteSpace(dto.Phone))
        {
            var phoneResult = ValidateAndCleanPhone(dto.Phone, out string? cleanPhone);
            if (phoneResult is UpdateUserResult.InvalidData)
                return phoneResult;

            user.Phone = cleanPhone!;
        }

        return await UpdateUser(user);
    }

    public async Task<UpdateUserResult> UpdateUserIdentity(long userId, UpdateUserIdentityDto dto)
    {
        // Should only be callable by someone with USERS:MANAGE permission from the controller
        var userResult = await GetUserById(userId);
        if (userResult is GetUserResult.NotFound)
            return new UpdateUserResult.NotFound();
        var user = ((GetUserResult.Success)userResult).User;

        bool wasModified = false;

        if (!string.IsNullOrWhiteSpace(dto.FirstName))
        {
            user.FirstName = dto.FirstName.Capitalize();
            wasModified = true;
        }
        if (!string.IsNullOrWhiteSpace(dto.LastName))
        {
            user.LastName = dto.LastName.Capitalize();
            wasModified = true;
        }

        if (dto.Birthday.HasValue)
        {
            if (dto.Birthday.Value > DateTimeOffset.UtcNow)
                return new UpdateUserResult.InvalidData("Birthday cannot be in the future.");

            if (dto.Birthday.Value.Year < 1900)
                return new UpdateUserResult.InvalidData("Birthday is not valid.");

            var today = DateTimeOffset.UtcNow.Date;
            var bdayDate = dto.Birthday.Value.UtcDateTime.Date;
            int age = today.Year - bdayDate.Year;
            if (bdayDate > today.AddYears(-age)) age--;
            // Minimum age requirement; provisional license considered to be valid from 16 years old
            if (age < 16) return new UpdateUserResult.InvalidData("User must be at least 16 years old.");

            user.Birthday = dto.Birthday.Value;
            wasModified = true;
        }

        if (!wasModified) return new UpdateUserResult.Success(user);

        return await UpdateUser(user);
    }

    public async Task<UpdateUserResult> UpdateUserRole(long userId, UpdateUserRoleDto dto)
    {
        // Should only be callable by someone with USERS:MANAGE permission
        var userResult = await GetUserById(userId);
        if (userResult is GetUserResult.NotFound)
            return new UpdateUserResult.NotFound();
        var user = ((GetUserResult.Success)userResult).User;

        if (user.RoleId == dto.RoleId)
            return new UpdateUserResult.NoChangesMade();

        var newRole = await _roles.GetById<RoleModel>(dto.RoleId);
        if (newRole is null)
            return new UpdateUserResult.InvalidData($"Role with ID {dto.RoleId} does not exist.");

        if (user.Role.Name.Equals("ADMIN", StringComparison.OrdinalIgnoreCase)
            && !newRole.Name.Equals("ADMIN", StringComparison.OrdinalIgnoreCase))
            return new UpdateUserResult.InvalidData("Cannot change role of an ADMIN user.");

        user.RoleId = dto.RoleId;

        return await UpdateUser(user);
    }

    [Authorize("CanManageUsers")]
    public async Task<ServiceResult<UserHotelProfileDto>> GiveUserHotelManagementPermission(long userId, int hotelId)
    {
        var user = await _users.FindByIdAsync(userId);
        if (user is null) return ServiceResult<UserHotelProfileDto>.NotFound("no user found with that id");

        var hotel = await _hotels.FindByIdAsync(hotelId);
        if (hotel is null) return ServiceResult<UserHotelProfileDto>.NotFound("no hotel found with that id");

        user.HotelId = hotelId;
        _users.Update(user);
        await _users.SaveChangesAsync();

        return ServiceResult<UserHotelProfileDto>.Ok(new UserHotelProfileDto
        {
            Id = user.Id,
            Username = user.Username,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Phone = user.Phone,
            Birthday = user.Birthday,
            HotelId = hotel.Id,
            HotelName = hotel.Name,
            HotelAddress = hotel.Address,
            HotelIBAN = hotel.IBAN,
            HotelParkingLotId = hotel.HotelParkingLotId
        });
    }

    [Authorize("CanManageUsers")]
    public async Task<ServiceResult<UserBusinessProfileDto>> GiveUserBusinessManagementPermission(long userId,
        int businessId)
    {
        var user = await _users.FindByIdAsync(userId);
        if (user is null) return ServiceResult<UserBusinessProfileDto>.NotFound("no user found with that id");
        var business = await _businesses.FindByIdAsync(businessId);
        if (business is null) return ServiceResult<UserBusinessProfileDto>.NotFound("no business found with that id");

        user.BusinessId = businessId;
        _businesses.Update(business);
        await _businesses.SaveChangesAsync();
        return ServiceResult<UserBusinessProfileDto>.Ok(new UserBusinessProfileDto
        {
            Id = user.Id,
            Username = user.Username,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Phone = user.Phone,
            Birthday = user.Birthday,
            BusinessId = business.Id,
            BusinessName = business.Name,
            BusinessAddress = business.Address,
            BusinessIBAN = business.IBAN,
        });
    }

    private static UpdateUserResult ValidatePasswordIntegrity(string password)
    {
        if (!PasswordRegex().IsMatch(password))
            return new UpdateUserResult.InvalidData("Password does not meet complexity requirements.");
        return new UpdateUserResult.Success(null!);
    }

    private static UpdateUserResult ValidateAndCleanEmail(string email, out string? cleanEmail)
    {
        email = email.TrimSafe();
        var registerResult = CleanEmail(email, out cleanEmail);

        if (registerResult is RegisterResult.InvalidData invalidData)
            return new UpdateUserResult.InvalidData($"Email format error: {invalidData.Message}");
        return new UpdateUserResult.Success(null!);
    }

    private static UpdateUserResult ValidateAndCleanPhone(string phone, out string? cleanPhone)
    {
        phone = phone.TrimSafe();
        var registerResult = CleanPhone(phone, out cleanPhone);

        if (registerResult is RegisterResult.InvalidData invalidData)
            return new UpdateUserResult.InvalidData($"Phone format error: {invalidData.Message}");
        return new UpdateUserResult.Success(null!);
    }

    private static RegisterResult CleanPhone(string phone, out string? cleanPhone)
    {
        phone = phone.TrimSafe();
        if (string.IsNullOrWhiteSpace(phone))
        {
            cleanPhone = null;
            return new RegisterResult.InvalidData("Phone number cannot be empty.");
        }

        bool hasLeadingPlus = phone.StartsWith('+');
        phone = PhoneTrim().Replace(phone, "");
        if (hasLeadingPlus && !phone.StartsWith('+'))
            phone = "+" + phone;

        if (!Phone().IsMatch(phone))
        {
            cleanPhone = null;
            return new RegisterResult.InvalidData("Phone number contains invalid characters.");
        }

        if (phone.StartsWith("00")) phone = phone[2..];
        if (phone.StartsWith('+')) phone = phone[1..];
        if (phone.StartsWith("31")) phone = phone[2..];
        if (!phone.StartsWith('0')) phone = '0' + phone;

        if (!PhoneDigits().IsMatch(phone))
        {
            cleanPhone = null;
            return new RegisterResult.InvalidData("Phone number is not a Dutch number.");
        }

        cleanPhone = "+31" + phone[1..];
        return new RegisterResult.Success(null!);
    }

    private static RegisterResult CleanEmail(string email, out string? cleanEmail)
    {
        email = email.TrimSafe();
        if (string.IsNullOrWhiteSpace(email))
        {
            cleanEmail = null;
            return new RegisterResult.InvalidData("Email cannot be empty.");
        }

        email = email.Trim();

        var parts = email.Split('@');
        if (parts.Length != 2)
        {
            cleanEmail = null;
            return new RegisterResult.InvalidData("Email must contain exactly one '@' symbol.");
        }

        string local = parts[0];
        string domain = parts[1];

        try
        {
            var idn = new IdnMapping();
            domain = idn.GetAscii(domain);
        }
        catch (ArgumentException)
        {
            cleanEmail = null;
            return new RegisterResult.InvalidData("Email contains invalid international domain name.");
        }

        string normalizedEmail = $"{local}@{domain.ToLowerInvariant()}";

        if (!EmailRegex().IsMatch(normalizedEmail))
        {
            cleanEmail = null;
            return new RegisterResult.InvalidData("Email address is not in a valid format.");
        }

        cleanEmail = normalizedEmail;
        return new RegisterResult.Success(null!);
    }
}
