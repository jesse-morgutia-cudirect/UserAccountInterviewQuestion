using NSubstitute;
using System.ComponentModel.DataAnnotations;
using UserAccountService.Interfaces;
using UserAccountService.Models;
using System.Threading.Tasks;

// ...existing using directives might include MSTest via project-level using

namespace UserAccountServiceTest
{
    [TestClass]
    public sealed class UserAccountServiceTest
    {
        [TestMethod]
        public void TestMethod1()
        {
            var userRepository = Substitute.For<IUserRepository>();
            var hasher = Substitute.For<IPasswordHasher>();
            var email = Substitute.For<IEmailService>();
            // placeholder test left from scaffolding
        }

        [TestMethod]
        public async Task RegisterAsync_CreatesUserAndSaves_WhenEmailAndPasswordValid()
        {
            // Arrange
            var userRepository = Substitute.For<IUserRepository>();
            var hasher = Substitute.For<IPasswordHasher>();
            var email = Substitute.For<IEmailService>();

            var testEmail = "testexample.com";
            var testEmail1 = "test@example.com";
            var testPassword = "supersecret";
            var hashed = "hashed-password";

            // No existing user
            userRepository.GetByEmailAsync(testEmail).Returns(Task.FromResult<User?>(null));

            // Hasher returns expected hash
            hasher.Hash(testPassword).Returns(hashed);

            // Capture the saved user
            User? savedUser = null;
            userRepository
                .When(r => r.SaveAsync(Arg.Any<User>()))
                .Do(ci => { savedUser = ci.ArgAt<User>(0); });

            // Email send should succeed
            email.SendWelcomeEmailAsync(testEmail).Returns(Task.CompletedTask);

            var svc = new UserAccountService.UserAccountService(userRepository, hasher, email);

            // Act
            var result = await svc.RegisterAsync(testEmail, testPassword);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Success, "Expected registration to succeed for valid input.");
            Assert.IsNotNull(savedUser, "Expected SaveAsync to be called with a User.");
            Assert.AreEqual(testEmail, savedUser.Email);
            Assert.AreEqual(hashed, savedUser.PasswordHash);
            Assert.IsFalse(string.IsNullOrEmpty(savedUser.Id));
            Assert.AreEqual(savedUser.Id, result.UserId);

            // Verify interactions
            hasher.Received(1).Hash(testPassword);
            await userRepository.Received(1).SaveAsync(Arg.Is<User>(u => u.Email == testEmail && u.PasswordHash == hashed && !string.IsNullOrEmpty(u.Id)));
            await email.Received(1).SendWelcomeEmailAsync(testEmail);
        }
    }

}
