using System.Collections.Concurrent;
using System.Security.Cryptography;
using MobyPark.Models;

namespace MobyPark.Services;

public class SessionService
{
    private readonly ConcurrentDictionary<string, UserModel> _sessions = new();
    
    public string CreateSession(UserModel user)
    {
        var token = GenerateToken();
        _sessions[token] = user;
        return token;
    }

    public void AddSession(string token, UserModel user)
    {
        _sessions[token] = user;
    }

    public UserModel? RemoveSession(string token)
    {
        _sessions.TryRemove(token, out var user);
        return user;
    }

    public UserModel? GetSession(string token)
    {
        _sessions.TryGetValue(token, out var user);
        return user;
    }
    
    private static string GenerateToken()
    {
        Span<byte> bytes = stackalloc byte[32]; // 256-bit
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes); // 64 hex chars
    }
}
