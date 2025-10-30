using System.Security.Cryptography;
using System.Text;

namespace MobyPark.Services;

public static class SystemService
{
    public static Guid GenerateGuid(string input)
    {
        byte[] hash = MD5.HashData(Encoding.UTF8.GetBytes(input));
        return new Guid(hash);
    }
}
