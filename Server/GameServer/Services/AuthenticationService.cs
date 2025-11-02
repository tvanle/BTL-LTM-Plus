using GameServer.Models;
using BCrypt.Net;
using System.Security.Cryptography;

namespace GameServer.Services;

public class AuthenticationService
{
    private readonly DatabaseService _database;

    public AuthenticationService(DatabaseService database)
    {
        _database = database;
    }

    public async Task<(bool Success, string? Token, User? User, string? Error)> RegisterAsync(
        string username, string email, string password)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(username) || username.Length < 3)
            return (false, null, null, "Username must be at least 3 characters");

        if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
            return (false, null, null, "Invalid email address");

        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            return (false, null, null, "Password must be at least 6 characters");

        // Check if username exists
        var existingUser = await _database.GetUserByUsernameAsync(username);
        if (existingUser != null)
            return (false, null, null, "Username already taken");

        // Check if email exists
        existingUser = await _database.GetUserByEmailAsync(email);
        if (existingUser != null)
            return (false, null, null, "Email already registered");

        // Hash password
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

        // Create user
        var user = await _database.CreateUserAsync(username, email, passwordHash);

        // Create session token
        var token = GenerateSecureToken();
        await _database.CreateSessionTokenAsync(user.Id, token, null, null);

        return (true, token, user, null);
    }

    public async Task<(bool Success, string? Token, User? User, string? Error)> LoginAsync(
        string usernameOrEmail, string password)
    {
        // Find user by username or email
        var user = await _database.GetUserByUsernameAsync(usernameOrEmail);
        if (user == null)
        {
            user = await _database.GetUserByEmailAsync(usernameOrEmail);
        }

        if (user == null)
            return (false, null, null, "Invalid credentials");

        // Verify password
        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return (false, null, null, "Invalid credentials");

        // Update online status
        await _database.UpdateUserOnlineStatusAsync(user.Id, true);

        // Create session token
        var token = GenerateSecureToken();
        await _database.CreateSessionTokenAsync(user.Id, token, null, null);

        return (true, token, user, null);
    }

    public async Task<User?> ValidateTokenAsync(string token)
    {
        var sessionToken = await _database.GetSessionTokenAsync(token);
        if (sessionToken == null)
            return null;

        var user = await _database.GetUserByIdAsync(sessionToken.UserId);
        return user;
    }

    public async Task LogoutAsync(Guid userId)
    {
        await _database.UpdateUserOnlineStatusAsync(userId, false);
    }

    public async Task<(bool Success, User? User, string? Error)> UpdateProfileAsync(
        Guid userId, string? displayName, string? avatarUrl)
    {
        try
        {
            await _database.UpdateUserProfileAsync(userId, displayName, avatarUrl);

            // Get updated user data
            var user = await _database.GetUserByIdAsync(userId);
            if (user == null)
                return (false, null, "User not found");

            return (true, user, null);
        }
        catch (Exception ex)
        {
            return (false, null, $"Update failed: {ex.Message}");
        }
    }

    private string GenerateSecureToken()
    {
        var bytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }
        return Convert.ToBase64String(bytes);
    }
}
