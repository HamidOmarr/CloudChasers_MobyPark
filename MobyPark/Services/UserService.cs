using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using MobyPark.Models;
using MobyPark.Models.DataService;

namespace MobyPark.Services;

public partial class UserService
{
    private readonly IDataAccess _dataAccess;
    private const int UsernameLength = 25;
    private const int NameLength = 50;

    private const string PasswordPattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*\W).{8,}$";
    private const string PhoneTrimPattern = @"\D";
    private const string PhonePattern = @"^\+?[0-9]+$";
    private const string PhoneDigitPattern = @"^0\d{9}$";
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

    public UserService(IDataAccess dataAccess)
    {
        _dataAccess = dataAccess;
    }

    public string HashPassword(string password)
    {
        if (password is null) throw new ArgumentNullException(nameof(password));
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }

    public bool VerifyPassword(string password, string hashedPassword)
    {
        ArgumentNullException.ThrowIfNull(password);
        ArgumentNullException.ThrowIfNull(hashedPassword);
        return HashPassword(password) == hashedPassword;
    }

    private static string CleanPhone(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            throw new ArgumentException("Phone number cannot be empty.", nameof(phone));

        bool hasLeadingPlus = phone.StartsWith('+');
        phone = PhoneTrim().Replace(phone, "");
        if (hasLeadingPlus)
            phone = "+" + phone;

        if (!Phone().IsMatch(phone))
            throw new ArgumentException("Phone number contains invalid characters.", nameof(phone));

        // Normalize to +310 ...
        if (phone.StartsWith("00"))
            phone = phone[2..];
        if (phone.StartsWith('+'))
            phone = phone[1..];
        if (phone.StartsWith("31"))
            phone = phone[2..];
        if (!phone.StartsWith('0'))
            phone = "0" + phone;

        if (!PhoneDigits().IsMatch(phone))
            throw new ArgumentException("Invalid Dutch phone number digits.", nameof(phone));

        return "+31" + phone;
    }

    private static string CleanEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty.", nameof(email));

        email = email.Trim();

        var parts = email.Split('@');
        if (parts.Length != 2)
            throw new ArgumentException("Email must contain exactly one '@' symbol.", nameof(email));

        string local = parts[0];
        string domain = parts[1];

        try
        {
            var idn = new IdnMapping();
            domain = idn.GetAscii(domain);
        }
        catch (ArgumentException)
        { throw new ArgumentException("Email contains invalid international domain name.", nameof(email)); }

        string normalizedEmail = $"{local.ToLowerInvariant()}@{domain.ToLowerInvariant()}";

        if (!EmailRegex().IsMatch(normalizedEmail))
            throw new ArgumentException("Email address is not in a valid format.", nameof(email));

        return normalizedEmail;
    }

    public async Task<UserModel> CreateUser(UserModel user)
    {
        if (string.IsNullOrWhiteSpace(user.Username))
            throw new ArgumentException("Username cannot be empty.", nameof(user.Username));
        if (string.IsNullOrWhiteSpace(user.Password))
            throw new ArgumentException("Password cannot be empty.", nameof(user.Password));
        if (string.IsNullOrWhiteSpace(user.Name))
            throw new ArgumentException("Name cannot be empty.", nameof(user.Name));
        if (string.IsNullOrWhiteSpace(user.Email))
            throw new ArgumentException("Email cannot be empty.", nameof(user.Email));
        if (string.IsNullOrWhiteSpace(user.Phone))
            throw new ArgumentException("Phone cannot be empty.", nameof(user.Phone));
        if (user.BirthYear < 1900 || user.BirthYear > DateTime.Now.Year)
            throw new ArgumentException("Birth year is out of valid range.", nameof(user.BirthYear));

        if (user.Username.Length > UsernameLength)
            throw new ArgumentException($"Username cannot exceed {UsernameLength} characters.", nameof(user.Username));
        if (user.Name.Length > NameLength)
            throw new ArgumentException($"Name cannot exceed {NameLength} characters.", nameof(user.Name));

        try
        { user.Phone = CleanPhone(user.Phone); }
        catch (ArgumentException ex)
        { throw new ArgumentException(ex.Message, nameof(user.Phone)); }

        try
        { user.Email = CleanEmail(user.Email); }
        catch (ArgumentException ex)
        { throw new ArgumentException(ex.Message, nameof(user.Email)); }

        if (!PasswordRegex().IsMatch(user.Password))
            throw new ArgumentException("Password does not meet complexity requirements.", nameof(user.Password));

        user.Password = HashPassword(user.Password);

        (bool success, int id) = await _dataAccess.Users.CreateWithId(user);
        if (success) user.Id = id;
        return user;
    }

    public async Task<UserModel?> GetUserByUsername(string username)
    {
        UserModel? user = await _dataAccess.Users.GetByUsername(username);

        return user;
    }

    // public async Task<UserModel> GetUserById(int id)
    // {
    //     UserModel? user = await _dataAccess.Users.GetById(id);
    //     if (user is null) throw new KeyNotFoundException("User not found");
    //     return user;
    // }

    public async Task<UserModel?> GetUserById(int id) => await _dataAccess.Users.GetById(id);

    public async Task<List<UserModel>> GetAllUsers() => await _dataAccess.Users.GetAll();

    public async Task<int> CountUsers() => await _dataAccess.Users.Count();

    public async Task<UserModel> UpdateUser(UserModel user)
    {
        ArgumentNullException.ThrowIfNull(user);
        await _dataAccess.Users.Update(user);
        return user;
    }

    public async Task<bool> DeleteUser(int id)
    {
        var user = GetUserById(id);
        if (user is null) throw new KeyNotFoundException("User not found");

        bool success = await _dataAccess.Users.Delete(id);
        return success;
    }
}
