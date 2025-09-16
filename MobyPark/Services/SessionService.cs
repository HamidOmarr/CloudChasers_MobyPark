using System.Collections.Concurrent;
using MobyPark.Models;

namespace MobyPark.Services;

public class SessionService
{
    private readonly ConcurrentDictionary<string, UserModel> _sessions = new();

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
}
