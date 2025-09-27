using System.Collections.Concurrent;
using MobyPark.Models.DataService;

namespace MobyPark.Models.DataAccess.InMemory;

public class InMemoryUserRepository : IUserRepository
{
    private readonly ConcurrentDictionary<int, UserModel> _byId = new();
    private readonly ConcurrentDictionary<string,int> _byEmail =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string,int> _byUsername =
        new(StringComparer.OrdinalIgnoreCase);

    private int _nextId = 0;

    public Task<bool> ExistsByEmailOrUsernameAsync(string email, string username)
        => Task.FromResult(_byEmail.ContainsKey(email) || _byUsername.ContainsKey(username));

    public Task<UserModel?> GetByEmailOrUsernameAsync(string identifier)
    {
        if (_byEmail.TryGetValue(identifier, out var id) && _byId.TryGetValue(id, out var u1))
            return Task.FromResult<UserModel?>(u1);
        if (_byUsername.TryGetValue(identifier, out id) && _byId.TryGetValue(id, out var u2))
            return Task.FromResult<UserModel?>(u2);
        return Task.FromResult<UserModel?>(null);
    }

    // NEW
    public Task<UserModel?> GetByUsernameAsync(string username)
    {
        if (_byUsername.TryGetValue(username, out var id) && _byId.TryGetValue(id, out var u))
            return Task.FromResult<UserModel?>(u);
        return Task.FromResult<UserModel?>(null);
    }

    // NEW
    public Task<UserModel?> GetByEmailAsync(string email)
    {
        if (_byEmail.TryGetValue(email, out var id) && _byId.TryGetValue(id, out var u))
            return Task.FromResult<UserModel?>(u);
        return Task.FromResult<UserModel?>(null);
    }

    public Task AddAsync(UserModel user)
    {
        if (_byEmail.ContainsKey(user.Email) || _byUsername.ContainsKey(user.Username))
            throw new InvalidOperationException("Email or username already in use.");

        var id = Interlocked.Increment(ref _nextId);
        user.Id = id;
        if (user.CreatedAt == default) user.CreatedAt = DateTime.UtcNow;

        if (!_byId.TryAdd(id, user)) throw new InvalidOperationException("Could not add user.");
        if (!_byEmail.TryAdd(user.Email, id) || !_byUsername.TryAdd(user.Username, id))
        {
            _byId.TryRemove(id, out _);
            _byEmail.TryRemove(user.Email, out _);
            _byUsername.TryRemove(user.Username, out _);
            throw new InvalidOperationException("Email or username already in use.");
        }

        return Task.CompletedTask;
    }

    // NEW
    public Task UpdateAsync(UserModel user)
    {
        if (!_byId.TryGetValue(user.Id, out var existing))
            throw new InvalidOperationException("User not found.");

        // Uniqueness checks if email/username changed
        if (!string.Equals(existing.Email, user.Email, StringComparison.OrdinalIgnoreCase) &&
            _byEmail.TryGetValue(user.Email, out var emailId) && emailId != user.Id)
            throw new InvalidOperationException("Email already in use.");

        if (!string.Equals(existing.Username, user.Username, StringComparison.OrdinalIgnoreCase) &&
            _byUsername.TryGetValue(user.Username, out var userId) && userId != user.Id)
            throw new InvalidOperationException("Username already in use.");

        // Update maps when keys changed
        if (!string.Equals(existing.Email, user.Email, StringComparison.OrdinalIgnoreCase))
        {
            _byEmail.TryRemove(existing.Email, out _);
            _byEmail[user.Email] = user.Id;
        }

        if (!string.Equals(existing.Username, user.Username, StringComparison.OrdinalIgnoreCase))
        {
            _byUsername.TryRemove(existing.Username, out _);
            _byUsername[user.Username] = user.Id;
        }

        _byId[user.Id] = user;
        return Task.CompletedTask;
    }
}
