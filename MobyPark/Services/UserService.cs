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
    private const string PasswordPattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*\W).{8,}$";
    private const string PhonePattern = @"^[\d\s\-\+\(\)]+$";
    private const string EmailPattern = @"^(?!\.)(?!.*\.\.)[A-Za-z0-9._%+-]+(?<!\.)@(?:(?:xn--)?[A-Za-z0-9](?:[A-Za-z0-9-]{0,61}[A-Za-z0-9])?\.)+[A-Za-z]{2,63}$";

    private const int UsernameLength = 25;
    private const int NameLength = 50;

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

    private string CleanPhone(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            throw new ArgumentException("Phone number cannot be empty.", nameof(phone));

        string cleanPhone = phone.Trim();

        if (!PhoneRegex().IsMatch(cleanPhone))
            throw new ArgumentException("Phone number contains invalid characters.", nameof(phone));

        string digitsOnlyWithPrefix = cleanPhone.StartsWith('+')
            ? "+" + DigitsOnly().Replace(cleanPhone[1..], "")
            : DigitsOnly().Replace(cleanPhone, "");

        if (digitsOnlyWithPrefix.StartsWith("00"))
            digitsOnlyWithPrefix = string.Concat("+", digitsOnlyWithPrefix.AsSpan(2));

        string digitsOnly = digitsOnlyWithPrefix.TrimStart('+');
        bool inInternationalFormat = digitsOnlyWithPrefix.StartsWith('+');

        cleanPhone = inInternationalFormat switch
        {
            false when digitsOnly.StartsWith("06") => "+31" + digitsOnly,
            false when digitsOnly.StartsWith('0') => "+31" + digitsOnly[1..],
            false => throw new ArgumentException(
                "Phone number must be provided in international format (starting with '+') or as a Dutch mobile (06...)",
                nameof(phone)),
            true when digitsOnly.StartsWith("31") => digitsOnly.Length > 2 && digitsOnly[2] != '0'
                ? $"+310{digitsOnly[2..]}"
                : "+" + digitsOnly,
            true => "+" + digitsOnly,
        };

        if (!PhonePlusAndLength().IsMatch(cleanPhone))
            throw new ArgumentException("Normalized phone number is not a valid international length (+ followed by 10-16 digits).", nameof(phone));

        return cleanPhone;
    }

    private string CleanEmail(string email)
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
        {
            throw new ArgumentException("Email contains invalid international domain name.", nameof(email));
        }

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

        // var user = new UserModel
        // {
        //     Username = username,
        //     Password = hashedPassword,
        //     Name = name,
        //     Email = email,
        //     Phone = phone,
        //     CreatedAt = DateTime.UtcNow,
        //     BirthYear = birthYear,
        //     Active = true
        // };

        await _dataAccess.Users.Create(user);
        return user;
    }

    public async Task<UserModel?> GetUserByUsername(string username)
    {
        UserModel? user = await _dataAccess.Users.GetByUsername(username);

        return user;
    }

    public async Task<UserModel> UpdateUser(UserModel user)
    {
        ArgumentNullException.ThrowIfNull(user);
        await _dataAccess.Users.Update(user);
        return user;
    }

    [GeneratedRegex(PasswordPattern)]
    private static partial Regex PasswordRegex();
    [GeneratedRegex(PhonePattern)]
    private static partial Regex PhoneRegex();
    [GeneratedRegex(@"\D")]
    private static partial Regex DigitsOnly();
    [GeneratedRegex(@"^\+\d{10,16}$")]
    private static partial Regex PhonePlusAndLength();
    [GeneratedRegex(EmailPattern)]
    private static partial Regex EmailRegex();
}
