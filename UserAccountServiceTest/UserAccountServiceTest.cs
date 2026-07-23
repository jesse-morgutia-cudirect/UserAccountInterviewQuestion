using NSubstitute;
using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using UserAccountService.Interfaces;
using UserAccountService.Models;

namespace UserAccountServiceTest
{
    [TestClass]
    public sealed class UserAccountServiceTest
    {
        // Rule 4 test (already added previously) — keep as a sanity check
        [TestMethod]
        public async Task RegisterAsync_CreatesUserAndSaves_WhenEmailAndPasswordValid()
        {
            var userRepository = Substitute.For<IUserRepository>();
            var hasher = Substitute.For<IPasswordHasher>();
            var email = Substitute.For<IEmailService>();

            var testEmail = "test@example.com";
            var testPassword = "supersecret";
            var hashed = "hashed-password";

            userRepository.GetByEmailAsync(testEmail).Returns(Task.FromResult<User?>(null));
            hasher.Hash(testPassword).Returns(hashed);

            User? savedUser = null;
            userRepository
                .When(r => r.SaveAsync(Arg.Any<User>()))
                .Do(ci => { savedUser = ci.Arg<User>(0); });

            email.SendWelcomeEmailAsync(testEmail).Returns(Task.CompletedTask);

            var svc = new UserAccountService.UserAccountService(userRepository, hasher, email);

            var result = await svc.RegisterAsync(testEmail, testPassword);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Success);
            Assert.IsNotNull(savedUser);
            Assert.AreEqual(testEmail, savedUser.Email);
            Assert.AreEqual(hashed, savedUser.PasswordHash);
            Assert.IsFalse(string.IsNullOrEmpty(savedUser.Id));
            Assert.AreEqual(savedUser.Id, result.UserId);

            hasher.Received(1).Hash(testPassword);
            await userRepository.Received(1).SaveAsync(Arg.Is<User>(u => u.Email == testEmail && u.PasswordHash == hashed && !string.IsNullOrEmpty(u.Id)));
            await email.Received(1).SendWelcomeEmailAsync(testEmail);
        }

        // Rule 1: invalid email format
        [TestMethod]
        public async Task RegisterAsync_InvalidEmail_ReturnsFailure()
        {
            var userRepository = Substitute.For<IUserRepository>();
            var hasher = Substitute.For<IPasswordHasher>();
            var email = Substitute.For<IEmailService>();

            var svc = new UserAccountService.UserAccountService(userRepository, hasher, email);

            var invalidEmails = new[]
            {
                "",
                "   ",
                "plainaddress",
                "missingatsign.com",
                "missingdomain@.com",
                "@no-local.com",
                "a@b",
                "a@b..com",
                "a b@c.com",
                "a@b@c.com"
            };

            foreach (var invalid in invalidEmails)
            {
                var result = await svc.RegisterAsync(invalid, "validpassword");
                Assert.IsFalse(result.Success, $"Expected registration to fail for '{invalid}'");
                Assert.AreEqual("Invalid email format.", result.FailureReason);
            }
        }

        // Rule 2: short or null password
        [TestMethod]
        public async Task RegisterAsync_ShortPassword_ReturnsFailure()
        {
            var userRepository = Substitute.For<IUserRepository>();
            var hasher = Substitute.For<IPasswordHasher>();
            var email = Substitute.For<IEmailService>();

            var svc = new UserAccountService.UserAccountService(userRepository, hasher, email);

            var result = await svc.RegisterAsync("a@b.com", "short");

            Assert.IsFalse(result.Success);
            Assert.AreEqual("Password must be at least 8 characters.", result.FailureReason);
        }

        // Rule 3: email already registered
        [TestMethod]
        public async Task RegisterAsync_EmailAlreadyRegistered_ReturnsFailure()
        {
            var userRepository = Substitute.For<IUserRepository>();
            var hasher = Substitute.For<IPasswordHasher>();
            var email = Substitute.For<IEmailService>();

            var testEmail = "exists@example.com";
            userRepository.GetByEmailAsync(testEmail).Returns(Task.FromResult(new User { Email = testEmail }));

            var svc = new UserAccountService.UserAccountService(userRepository, hasher, email);

            var result = await svc.RegisterAsync(testEmail, "validpass123");

            Assert.IsFalse(result.Success);
            Assert.AreEqual("Email is already registered.", result.FailureReason);
        }

        // Rule 5: email sending failure should not fail registration
        [TestMethod]
        public async Task RegisterAsync_EmailSendThrows_StillSucceeds()
        {
            var userRepository = Substitute.For<IUserRepository>();
            var hasher = Substitute.For<IPasswordHasher>();
            var email = Substitute.For<IEmailService>();

            var testEmail = "test2@example.com";
            var testPassword = "validpass123";
            var hashed = "h";

            userRepository.GetByEmailAsync(testEmail).Returns(Task.FromResult<User?>(null));
            hasher.Hash(testPassword).Returns(hashed);
            userRepository.SaveAsync(Arg.Any<User>()).Returns(Task.CompletedTask);

            email.SendWelcomeEmailAsync(testEmail).Returns(Task.FromException(new Exception("boom")));

            var svc = new UserAccountService.UserAccountService(userRepository, hasher, email);

            var result = await svc.RegisterAsync(testEmail, testPassword);

            Assert.IsTrue(result.Success);
            await userRepository.Received(1).SaveAsync(Arg.Any<User>());
            await email.Received(1).SendWelcomeEmailAsync(testEmail);
        }

        // Rule 6: login when account locked
        [TestMethod]
        public async Task LoginAsync_WhenAccountLocked_ReturnsLockedReason()
        {
            var userRepository = Substitute.For<IUserRepository>();
            var hasher = Substitute.For<IPasswordHasher>();
            var email = Substitute.For<IEmailService>();

            var now = DateTime.UtcNow;
            var testEmail = "locked@example.com";
            var user = new User { Email = testEmail, IsLocked = true, LockedUntilUtc = now.AddMinutes(5) };
            userRepository.GetByEmailAsync(testEmail).Returns(Task.FromResult(user));

            var svc = new UserAccountService.UserAccountService(userRepository, hasher, email, () => now);

            var result = await svc.LoginAsync(testEmail, "any");

            Assert.IsFalse(result.Success);
            Assert.AreEqual("Account is locked. Try again later.", result.FailureReason);
        }

        // Rule 7: failed attempts increment and lock on threshold
        [TestMethod]
        public async Task LoginAsync_FailedAttemptsIncrementAndLock_WhenThresholdReached()
        {
            var userRepository = Substitute.For<IUserRepository>();
            var hasher = Substitute.For<IPasswordHasher>();
            var email = Substitute.For<IEmailService>();

            var now = DateTime.UtcNow;
            var testEmail = "user@example.com";
            var user = new User { Email = testEmail, FailedLoginAttempts = 2, IsLocked = false };
            userRepository.GetByEmailAsync(testEmail).Returns(Task.FromResult(user));

            hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

            User? savedUser = null;
            userRepository.When(r => r.SaveAsync(Arg.Any<User>())).Do(ci => savedUser = ci.Arg<User>(0));

            var svc = new UserAccountService.UserAccountService(userRepository, hasher, email, () => now);

            var result = await svc.LoginAsync(testEmail, "badpass");

            Assert.IsFalse(result.Success);
            Assert.AreEqual("Invalid email or password.", result.FailureReason);
            Assert.IsNotNull(savedUser);
            Assert.AreEqual(3, savedUser.FailedLoginAttempts);
            Assert.IsTrue(savedUser.IsLocked);
            Assert.IsTrue(savedUser.LockedUntilUtc.HasValue && savedUser.LockedUntilUtc.Value > now);
            await userRepository.Received(1).SaveAsync(Arg.Any<User>());
        }

        // Rule 8: successful login resets counters and unlocks
        [TestMethod]
        public async Task LoginAsync_SuccessResetsFailedAttemptsAndUnlocks()
        {
            var userRepository = Substitute.For<IUserRepository>();
            var hasher = Substitute.For<IPasswordHasher>();
            var email = Substitute.For<IEmailService>();

            var now = DateTime.UtcNow;
            var testEmail = "user2@example.com";
            var user = new User { Email = testEmail, FailedLoginAttempts = 2, IsLocked = true, LockedUntilUtc = now.AddMinutes(-1) };
            userRepository.GetByEmailAsync(testEmail).Returns(Task.FromResult(user));

            hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

            User? savedUser = null;
            userRepository.When(r => r.SaveAsync(Arg.Any<User>())).Do(ci => savedUser = ci.Arg<User>(0));

            var svc = new UserAccountService.UserAccountService(userRepository, hasher, email, () => now);

            var result = await svc.LoginAsync(testEmail, "goodpass");

            Assert.IsTrue(result.Success);
            Assert.IsNotNull(savedUser);
            Assert.AreEqual(0, savedUser.FailedLoginAttempts);
            Assert.IsFalse(savedUser.IsLocked);
            Assert.IsNull(savedUser.LockedUntilUtc);
            await userRepository.Received(1).SaveAsync(Arg.Any<User>());
        }

        // Rule 9: request password reset
        [TestMethod]
        public async Task RequestPasswordResetAsync_NoUser_ThrowsUserNotFoundException()
        {
            var userRepository = Substitute.For<IUserRepository>();
            var hasher = Substitute.For<IPasswordHasher>();
            var email = Substitute.For<IEmailService>();

            var svc = new UserAccountService.UserAccountService(userRepository, hasher, email);

            await Assert.ThrowsExceptionAsync<UserAccountService.Exception.UserNotFoundException>(async () =>
            {
                await svc.RequestPasswordResetAsync("missing@example.com");
            });
        }

        [TestMethod]
        public async Task RequestPasswordResetAsync_SendsEmailWithToken()
        {
            var userRepository = Substitute.For<IUserRepository>();
            var hasher = Substitute.For<IPasswordHasher>();
            var email = Substitute.For<IEmailService>();

            var testEmail = "reset@example.com";
            userRepository.GetByEmailAsync(testEmail).Returns(Task.FromResult(new User { Email = testEmail }));

            string? capturedToken = null;
            email.When(e => e.SendPasswordResetEmailAsync(Arg.Is(testEmail), Arg.Any<string>()))
                 .Do(ci => capturedToken = ci.Arg<string>(1));

            var svc = new UserAccountService.UserAccountService(userRepository, hasher, email);

            await svc.RequestPasswordResetAsync(testEmail);

            Assert.IsNotNull(capturedToken);
            Assert.AreEqual(32, capturedToken.Length);
            await email.Received(1).SendPasswordResetEmailAsync(testEmail, Arg.Any<string>());
        }
    }
}
