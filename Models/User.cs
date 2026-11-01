namespace UserManagementAPI.Models;

/// <summary>
/// Represents a user managed by the API.
/// </summary>
public sealed record User(int Id, string Name, string Email);
