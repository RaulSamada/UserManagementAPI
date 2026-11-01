using UserManagementAPI.Models;

namespace UserManagementAPI.Services;

/// <summary>
/// Provides thread-safe, in-memory storage for users.
/// </summary>
public sealed class UserStore
{
    private readonly Lock _lock = new();
    private readonly Dictionary<int, User> _users = [];
    private volatile IReadOnlyList<User> _snapshot = Array.Empty<User>();
    private int _nextId = 1;

    public IReadOnlyList<User> GetAll() => _snapshot;

    public User? GetById(int id)
    {
        lock (_lock)
        {
            return _users.GetValueOrDefault(id);
        }
    }

    public User? Create(string name, string email)
    {
        lock (_lock)
        {
            if (EmailExists(email))
            {
                return null;
            }

            var user = new User(_nextId++, name, email);
            _users.Add(user.Id, user);
            RefreshSnapshot();

            return user;
        }
    }

    public UserUpdateResult Update(int id, string name, string email)
    {
        lock (_lock)
        {
            if (!_users.ContainsKey(id))
            {
                return new UserUpdateResult(UserUpdateStatus.NotFound, null);
            }

            if (EmailExists(email, id))
            {
                return new UserUpdateResult(UserUpdateStatus.EmailConflict, null);
            }

            var updatedUser = new User(id, name, email);
            _users[id] = updatedUser;
            RefreshSnapshot();

            return new UserUpdateResult(UserUpdateStatus.Success, updatedUser);
        }
    }

    public bool Delete(int id)
    {
        lock (_lock)
        {
            var wasDeleted = _users.Remove(id);
            if (wasDeleted)
            {
                RefreshSnapshot();
            }

            return wasDeleted;
        }
    }

    private void RefreshSnapshot() =>
        _snapshot = Array.AsReadOnly(
            _users.Values
                .OrderBy(user => user.Id)
                .ToArray());

    private bool EmailExists(string email, int? excludedUserId = null) =>
        _users.Values.Any(user =>
            user.Id != excludedUserId &&
            string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase));
}

public enum UserUpdateStatus
{
    Success,
    NotFound,
    EmailConflict
}

public sealed record UserUpdateResult(UserUpdateStatus Status, User? User);
