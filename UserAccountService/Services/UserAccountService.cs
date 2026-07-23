using System.Text.RegularExpressions;
using UserAccountService.Exception;
using UserAccountService.Interfaces;
using UserAccountService.Models;

namespace UserAccountService
{
    public class UserAccountService
    {
        private const int MaxFailedAttempts = 3;
        private const int LockoutMinutes = 15;

        private readonly IUserRepository _users;
        private readonly IPasswordHasher _hasher;
        private readonly IEmailService _email;
        private readonly Func<DateTime> _utcNow;   // injectable for deterministic tests

        public UserAccountService(
            IUserRepository users,
            IPasswordHasher hasher,
            IEmailService email,
            Func<DateTime> utcNow = null)
        {
            _users = users ?? throw new ArgumentNullException(nameof(users));
            _hasher = hasher ?? throw new ArgumentNullException(nameof(hasher));
            _email = email ?? throw new ArgumentNullException(nameof(email));
            _utcNow = utcNow ?? (() => DateTime.UtcNow);
        }

        // ── Register ─────────────────────────────────────────────────────────

        public async Task<RegisterResult> RegisterAsync(string email, string password)
        {
            // Rule 1
            if (!IsValidEmail(email))
                return new RegisterResult { Success = false, FailureReason = "Invalid email format." };

            // Rule 2
            if (password == null || password.Length < 8)
                return new RegisterResult { Success = false, FailureReason = "Password must be at least 8 characters." };

            // Rule 3
            var existing = await _users.GetByEmailAsync(email);
            if (existing != null)
                return new RegisterResult { Success = false, FailureReason = "Email is already registered." };

            // Rule 4
            var user = new User
            {
                Id = Guid.NewGuid().ToString(),
                Email = email,
                PasswordHash = _hasher.Hash(password)
            };

            await _users.SaveAsync(user);

            // Rule 5
            try { await _email.SendWelcomeEmailAsync(email); }
            catch { /* log in real impl */ }

            return new RegisterResult { Success = true, UserId = user.Id };
        }

        // ── Login ─────────────────────────────────────────────────────────────

        public async Task<LoginResult> LoginAsync(string email, string password)
        {
            var user = await _users.GetByEmailAsync(email);
            if (user == null)
                return new LoginResult { Success = false, FailureReason = "Invalid email or password." };

            // Rule 6
            if (user.IsLocked && user.LockedUntilUtc.HasValue && user.LockedUntilUtc.Value > _utcNow())
                return new LoginResult { Success = false, FailureReason = "Account is locked. Try again later." };

            if (!_hasher.Verify(password, user.PasswordHash))
            {
                user.FailedLoginAttempts++;

                // Rule 7
                if (user.FailedLoginAttempts >= MaxFailedAttempts)
                {
                    user.IsLocked = true;
                    user.LockedUntilUtc = _utcNow().AddMinutes(LockoutMinutes);
                }

                await _users.SaveAsync(user);
                return new LoginResult { Success = false, FailureReason = "Invalid email or password." };
            }

            // Rule 8
            user.FailedLoginAttempts = 0;
            user.IsLocked = false;
            user.LockedUntilUtc = null;
            await _users.SaveAsync(user);

            return new LoginResult { Success = true };
        }

        // ── Password reset ────────────────────────────────────────────────────

        public async Task RequestPasswordResetAsync(string email)
        {
            var user = await _users.GetByEmailAsync(email);
            if (user == null)
                throw new UserNotFoundException($"No account found for '{email}'.");

            // Rule 9
            var token = Guid.NewGuid().ToString("N");
            await _email.SendPasswordResetEmailAsync(email, token);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        }
    }
}
