namespace UserManagementAPI.Contracts;

/// <summary>
/// Contains the editable fields used to create or update a user.
/// </summary>
public sealed record UserRequest(string? Name, string? Email);
