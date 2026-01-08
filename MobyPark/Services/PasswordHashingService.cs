using System.Security.Cryptography;
using System.Text;

using Microsoft.AspNetCore.Identity;

using MobyPark.Models;

namespace MobyPark.Services;

public class PasswordHashingService : PasswordHasher<UserModel>
{
    public override PasswordVerificationResult VerifyHashedPassword(UserModel user, string hashedPassword, string providedPassword)
    {
        var result = base.VerifyHashedPassword(user, hashedPassword, providedPassword);

        return result != PasswordVerificationResult.Failed
            ? result
            : VerifyMd5Hash(providedPassword, hashedPassword)
                ? PasswordVerificationResult.SuccessRehashNeeded
                : PasswordVerificationResult.Failed;
    }

    private static bool VerifyMd5Hash(string input, string hash)
    {
        byte[] inputBytes = Encoding.ASCII.GetBytes(input);
        byte[] hashBytes = MD5.HashData(inputBytes);
        string inputHash = Convert.ToHexString(hashBytes).ToLowerInvariant();

        return string.Equals(inputHash, hash, StringComparison.OrdinalIgnoreCase);
    }
}