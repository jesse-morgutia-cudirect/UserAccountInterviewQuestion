using Microsoft.AspNetCore.Identity;

namespace Interviews.UserAccounts
{
    public class AspNetPasswordHasher : IPasswordHasher
    {
        private readonly PasswordHasher<User> _passwordHasher = new PasswordHasher<User>();

        public string Hash(string plainText)
        {
            return _passwordHasher.HashPassword(new User(), plainText);
        }

        public bool Verify(string plainText, string hash)
        {
            if (plainText == null || hash == null)
                return false;

            var result = _passwordHasher.VerifyHashedPassword(new User(), hash, plainText);
            return result == PasswordVerificationResult.Success ||
                   result == PasswordVerificationResult.SuccessRehashNeeded;
        }
    }
}
