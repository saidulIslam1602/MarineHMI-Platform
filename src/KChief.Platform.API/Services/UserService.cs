using Serilog;
using Serilog.Context;
using KChief.Platform.Core.Interfaces;
using KChief.Platform.Core.Models;
using KChief.Platform.Core.Exceptions;

namespace KChief.Platform.API.Services;

/// <summary>
/// Service for user management operations.
/// </summary>
public class UserService : IUserService
{
    private readonly ILogger<UserService> _logger;

    // In-memory user storage for demonstration (replace with database in production)
    private static readonly List<User> _users = new()
    {
        new User
        {
            Id = "admin-001",
            Username = "admin",
            Email = "admin@kchief.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
            FirstName = "System",
            LastName = "Administrator",
            Role = UserRole.Administrator,
            Department = "IT",
            JobTitle = "System Administrator",
            IsActive = true,
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow.AddDays(-30)
        },
        new User
        {
            Id = "captain-001",
            Username = "captain",
            Email = "captain@kchief.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("captain123"),
            FirstName = "John",
            LastName = "Smith",
            Role = UserRole.Captain,
            Department = "Navigation",
            JobTitle = "Ship Captain",
            IsActive = true,
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow.AddDays(-20)
        },
        new User
        {
            Id = "engineer-001",
            Username = "engineer",
            Email = "engineer@kchief.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("engineer123"),
            FirstName = "Sarah",
            LastName = "Johnson",
            Role = UserRole.ChiefEngineer,
            Department = "Engineering",
            JobTitle = "Chief Engineer",
            IsActive = true,
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow.AddDays(-15)
        },
        new User
        {
            Id = "operator-001",
            Username = "operator",
            Email = "operator@kchief.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("operator123"),
            FirstName = "Mike",
            LastName = "Wilson",
            Role = UserRole.Operator,
            Department = "Operations",
            JobTitle = "Vessel Operator",
            IsActive = true,
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow.AddDays(-10)
        },
        new User
        {
            Id = "observer-001",
            Username = "observer",
            Email = "observer@kchief.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("observer123"),
            FirstName = "Lisa",
            LastName = "Davis",
            Role = UserRole.Observer,
            Department = "Monitoring",
            JobTitle = "System Observer",
            IsActive = true,
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow.AddDays(-5)
        }
    };

    public UserService(ILogger<UserService> logger)
    {
        _logger = logger;
    }

    public async Task<User?> GetUserByIdAsync(string userId)
    {
        using (LogContext.PushProperty("UserId", userId))
        using (LogContext.PushProperty("Operation", "GetUserById"))
        {
            try
            {
                var user = _users.FirstOrDefault(u => u.Id == userId);
                
                if (user != null)
                {
                    Log.Debug("User found: {Username}", user.Username);
                }
                else
                {
                    Log.Debug("User not found with ID: {UserId}", userId);
                }
                
                return await Task.FromResult(user);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error retrieving user by ID: {UserId}", userId);
                throw;
            }
        }
    }

    public async Task<User?> GetUserByUsernameAsync(string username)
    {
        using (LogContext.PushProperty("Username", username))
        using (LogContext.PushProperty("Operation", "GetUserByUsername"))
        {
            try
            {
                var user = _users.FirstOrDefault(u => 
                    u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
                
                if (user != null)
                {
                    Log.Debug("User found: {UserId}", user.Id);
                }
                else
                {
                    Log.Debug("User not found with username: {Username}", username);
                }
                
                return await Task.FromResult(user);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error retrieving user by username: {Username}", username);
                throw;
            }
        }
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        using (LogContext.PushProperty("Email", email))
        using (LogContext.PushProperty("Operation", "GetUserByEmail"))
        {
            try
            {
                var user = _users.FirstOrDefault(u => 
                    u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
                
                if (user != null)
                {
                    Log.Debug("User found: {UserId}", user.Id);
                }
                else
                {
                    Log.Debug("User not found with email: {Email}", email);
                }
                
                return await Task.FromResult(user);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error retrieving user by email: {Email}", email);
                throw;
            }
        }
    }

    public async Task<IEnumerable<User>> GetUsersAsync(UserRole? role = null, bool? isActive = null, string? department = null)
    {
        using (LogContext.PushProperty("Role", role))
        using (LogContext.PushProperty("IsActive", isActive))
        using (LogContext.PushProperty("Department", department))
        using (LogContext.PushProperty("Operation", "GetUsers"))
        {
            try
            {
                var query = _users.AsQueryable();

                if (role.HasValue)
                {
                    query = query.Where(u => u.Role == role.Value);
                }

                if (isActive.HasValue)
                {
                    query = query.Where(u => u.IsActive == isActive.Value);
                }

                if (!string.IsNullOrEmpty(department))
                {
                    query = query.Where(u => u.Department != null && 
                        u.Department.Equals(department, StringComparison.OrdinalIgnoreCase));
                }

                var users = query.ToList();
                
                Log.Information("Retrieved {UserCount} users with filters", users.Count);
                
                return await Task.FromResult(users);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error retrieving users with filters");
                throw;
            }
        }
    }

    public async Task<User> CreateUserAsync(User user, string password)
    {
        using (LogContext.PushProperty("Username", user.Username))
        using (LogContext.PushProperty("Email", user.Email))
        using (LogContext.PushProperty("Operation", "CreateUser"))
        {
            try
            {
                // Validate username availability
                if (!await IsUsernameAvailableAsync(user.Username))
                {
                    throw new ValidationException("Username", "Username is already in use");
                }

                // Validate email availability
                if (!await IsEmailAvailableAsync(user.Email))
                {
                    throw new ValidationException("Email", "Email address is already in use");
                }

                // Hash password
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
                user.Id = Guid.NewGuid().ToString();
                user.CreatedAt = DateTime.UtcNow;
                user.UpdatedAt = DateTime.UtcNow;

                _users.Add(user);

                Log.Information("User created successfully: {UserId}", user.Id);
                
                return await Task.FromResult(user);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error creating user: {Username}", user.Username);
                throw;
            }
        }
    }

    public async Task<User> UpdateUserAsync(User user)
    {
        using (LogContext.PushProperty("UserId", user.Id))
        using (LogContext.PushProperty("Operation", "UpdateUser"))
        {
            try
            {
                var existingUser = _users.FirstOrDefault(u => u.Id == user.Id);
                
                if (existingUser == null)
                {
                    throw new VesselNotFoundException($"User not found: {user.Id}");
                }

                // Validate username availability (excluding current user)
                if (!await IsUsernameAvailableAsync(user.Username, user.Id))
                {
                    throw new ValidationException("Username", "Username is already in use");
                }

                // Validate email availability (excluding current user)
                if (!await IsEmailAvailableAsync(user.Email, user.Id))
                {
                    throw new ValidationException("Email", "Email address is already in use");
                }

                // Update user properties (preserve password hash and creation date)
                existingUser.Username = user.Username;
                existingUser.Email = user.Email;
                existingUser.FirstName = user.FirstName;
                existingUser.LastName = user.LastName;
                existingUser.Role = user.Role;
                existingUser.Department = user.Department;
                existingUser.JobTitle = user.JobTitle;
                existingUser.PhoneNumber = user.PhoneNumber;
                existingUser.IsActive = user.IsActive;
                existingUser.EmailVerified = user.EmailVerified;
                existingUser.UpdatedAt = DateTime.UtcNow;

                Log.Information("User updated successfully: {UserId}", user.Id);
                
                return await Task.FromResult(existingUser);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error updating user: {UserId}", user.Id);
                throw;
            }
        }
    }

    public async Task<bool> DeleteUserAsync(string userId)
    {
        using (LogContext.PushProperty("UserId", userId))
        using (LogContext.PushProperty("Operation", "DeleteUser"))
        {
            try
            {
                var user = _users.FirstOrDefault(u => u.Id == userId);
                
                if (user == null)
                {
                    Log.Warning("Attempted to delete non-existent user: {UserId}", userId);
                    return await Task.FromResult(false);
                }

                _users.Remove(user);
                
                Log.Information("User deleted successfully: {UserId}", userId);
                
                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error deleting user: {UserId}", userId);
                throw;
            }
        }
    }

    public async Task<bool> ChangePasswordAsync(string userId, string currentPassword, string newPassword)
    {
        using (LogContext.PushProperty("UserId", userId))
        using (LogContext.PushProperty("Operation", "ChangePassword"))
        {
            try
            {
                var user = _users.FirstOrDefault(u => u.Id == userId);
                
                if (user == null)
                {
                    Log.Warning("Password change attempted for non-existent user: {UserId}", userId);
                    return await Task.FromResult(false);
                }

                // Verify current password
                if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
                {
                    Log.Warning("Password change failed: Invalid current password for user {UserId}", userId);
                    return await Task.FromResult(false);
                }

                // Update password
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
                user.UpdatedAt = DateTime.UtcNow;

                Log.Information("Password changed successfully for user: {UserId}", userId);
                
                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error changing password for user: {UserId}", userId);
                throw;
            }
        }
    }

    public async Task<bool> ResetPasswordAsync(string userId, string newPassword)
    {
        using (LogContext.PushProperty("UserId", userId))
        using (LogContext.PushProperty("Operation", "ResetPassword"))
        {
            try
            {
                var user = _users.FirstOrDefault(u => u.Id == userId);
                
                if (user == null)
                {
                    Log.Warning("Password reset attempted for non-existent user: {UserId}", userId);
                    return await Task.FromResult(false);
                }

                // Update password
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
                user.UpdatedAt = DateTime.UtcNow;
                user.FailedLoginAttempts = 0; // Reset failed attempts
                user.LockedUntil = null; // Unlock account

                Log.Information("Password reset successfully for user: {UserId}", userId);
                
                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error resetting password for user: {UserId}", userId);
                throw;
            }
        }
    }

    public async Task<bool> LockUserAsync(string userId, DateTime? lockUntil = null)
    {
        using (LogContext.PushProperty("UserId", userId))
        using (LogContext.PushProperty("LockUntil", lockUntil))
        using (LogContext.PushProperty("Operation", "LockUser"))
        {
            try
            {
                var user = _users.FirstOrDefault(u => u.Id == userId);
                
                if (user == null)
                {
                    Log.Warning("Lock attempted for non-existent user: {UserId}", userId);
                    return await Task.FromResult(false);
                }

                user.LockedUntil = lockUntil ?? DateTime.UtcNow.AddHours(1);
                user.UpdatedAt = DateTime.UtcNow;

                Log.Information("User locked successfully: {UserId} until {LockUntil}", userId, user.LockedUntil);
                
                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error locking user: {UserId}", userId);
                throw;
            }
        }
    }

    public async Task<bool> UnlockUserAsync(string userId)
    {
        using (LogContext.PushProperty("UserId", userId))
        using (LogContext.PushProperty("Operation", "UnlockUser"))
        {
            try
            {
                var user = _users.FirstOrDefault(u => u.Id == userId);
                
                if (user == null)
                {
                    Log.Warning("Unlock attempted for non-existent user: {UserId}", userId);
                    return await Task.FromResult(false);
                }

                user.LockedUntil = null;
                user.FailedLoginAttempts = 0;
                user.UpdatedAt = DateTime.UtcNow;

                Log.Information("User unlocked successfully: {UserId}", userId);
                
                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error unlocking user: {UserId}", userId);
                throw;
            }
        }
    }

    public async Task<int> RecordFailedLoginAsync(string username)
    {
        using (LogContext.PushProperty("Username", username))
        using (LogContext.PushProperty("Operation", "RecordFailedLogin"))
        {
            try
            {
                var user = _users.FirstOrDefault(u => 
                    u.Username.Equals(username, StringComparison.OrdinalIgnoreCase) ||
                    u.Email.Equals(username, StringComparison.OrdinalIgnoreCase));
                
                if (user != null)
                {
                    user.FailedLoginAttempts++;
                    user.UpdatedAt = DateTime.UtcNow;

                    // Lock account after too many failed attempts
                    var maxAttempts = 5; // TODO: Get from configuration
                    if (user.FailedLoginAttempts >= maxAttempts)
                    {
                        user.LockedUntil = DateTime.UtcNow.AddMinutes(30); // TODO: Get from configuration
                        Log.Warning("User account locked due to too many failed login attempts: {UserId}", user.Id);
                    }

                    Log.Information("Failed login recorded for user: {UserId}, attempts: {FailedAttempts}", 
                        user.Id, user.FailedLoginAttempts);
                    
                    return await Task.FromResult(user.FailedLoginAttempts);
                }
                
                return await Task.FromResult(0);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error recording failed login for: {Username}", username);
                throw;
            }
        }
    }

    public async Task<User> RecordSuccessfulLoginAsync(string userId)
    {
        using (LogContext.PushProperty("UserId", userId))
        using (LogContext.PushProperty("Operation", "RecordSuccessfulLogin"))
        {
            try
            {
                var user = _users.FirstOrDefault(u => u.Id == userId);
                
                if (user == null)
                {
                    throw new VesselNotFoundException($"User not found: {userId}");
                }

                user.LastLoginAt = DateTime.UtcNow;
                user.FailedLoginAttempts = 0; // Reset failed attempts
                user.UpdatedAt = DateTime.UtcNow;

                Log.Information("Successful login recorded for user: {UserId}", userId);
                
                return await Task.FromResult(user);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error recording successful login for user: {UserId}", userId);
                throw;
            }
        }
    }

    public async Task<bool> IsUsernameAvailableAsync(string username, string? excludeUserId = null)
    {
        try
        {
            var existingUser = _users.FirstOrDefault(u => 
                u.Username.Equals(username, StringComparison.OrdinalIgnoreCase) &&
                (excludeUserId == null || u.Id != excludeUserId));
            
            return await Task.FromResult(existingUser == null);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error checking username availability: {Username}", username);
            throw;
        }
    }

    public async Task<bool> IsEmailAvailableAsync(string email, string? excludeUserId = null)
    {
        try
        {
            var existingUser = _users.FirstOrDefault(u => 
                u.Email.Equals(email, StringComparison.OrdinalIgnoreCase) &&
                (excludeUserId == null || u.Id != excludeUserId));
            
            return await Task.FromResult(existingUser == null);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error checking email availability: {Email}", email);
            throw;
        }
    }
}
