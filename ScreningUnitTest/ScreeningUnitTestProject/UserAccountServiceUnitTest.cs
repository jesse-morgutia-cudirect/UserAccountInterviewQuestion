using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace ScreeningUnitTestProject
{
    [TestClass]
    public class UnitTest1
    {
        private Mock<IUserRepository> _userRepository;
        private Mock<IPasswordHasher> _passwordHasher;

        private UserAccountService _service;

        [TestInitialize]
        public void Setup()
        {
            _userRepository = new Mock<IUserRepository>();
            _passwordHasher = new Mock<IPasswordHasher>();            

            _service = new UserAccountService(
                _userRepository.Object,
                _passwordHasher.Object);
        }

        [TestMethod]
        public async Task RegisterAsync_InvalidEmail_ReturnsFailure()
        {
            // Arrange
            string email = "invalid-email";
            string password = "Password123";

            // Act
            var result = await _service.RegisterAsync(email, password);

            // Assert
            Assert.IsFalse(result.Success);
            Assert.AreEqual("Invalid email format.", result.FailureReason);

            _userRepository.Verify(x => x.GetByEmailAsync(It.IsAny<string>()), Times.Never);
            _userRepository.Verify(x => x.SaveAsync(It.IsAny<User>()), Times.Never);
        }

        [TestMethod]
        public async Task RegisterAsync_ShortPassword_ReturnsFailure()
        {
            // Arrange
            string email = "test@test.com";
            string password = "12345";

            // Act
            var result = await _service.RegisterAsync(email, password);

            // Assert
            Assert.IsFalse(result.Success);
            Assert.AreEqual("Password must be at least 8 characters.", result.FailureReason);

            _userRepository.Verify(x => x.GetByEmailAsync(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public async Task RegisterAsync_EmailAlreadyExists_ReturnsFailure()
        {
            // Arrange
            string email = "test@test.com";
            string password = "Password123";

            _userRepository
                .Setup(x => x.GetByEmailAsync(email))
                .ReturnsAsync(new User());

            // Act
            var result = await _service.RegisterAsync(email, password);

            // Assert
            Assert.IsFalse(result.Success);
            Assert.AreEqual("Email is already registered.", result.FailureReason);

            _userRepository.Verify(x => x.SaveAsync(It.IsAny<User>()), Times.Never);
        }

        [TestMethod]
        public async Task RegisterAsync_ValidUser_ReturnsSuccess()
        {
            // Arrange
            string email = "test@test.com";
            string password = "Password123";

            _userRepository
                .Setup(x => x.GetByEmailAsync(email))
                .ReturnsAsync((User)null);

            _passwordHasher
                .Setup(x => x.Hash(password))
                .Returns("hashed-password");

            // Act
            var result = await _service.RegisterAsync(email, password);

            // Assert
            Assert.IsTrue(result.Success);
            Assert.IsNotNull(result.UserId);

            _passwordHasher.Verify(x => x.Hash(password), Times.Once);
            _userRepository.Verify(x => x.SaveAsync(It.IsAny<User>()), Times.Once);
            _emailService.Verify(x => x.SendWelcomeEmailAsync(email), Times.Once);
        }

        [TestMethod]
        public async Task RegisterAsync_EmailFails_StillReturnsSuccess()
        {
            // Arrange
            string email = "test@test.com";
            string password = "Password123";

            _userRepository
                .Setup(x => x.GetByEmailAsync(email))
                .ReturnsAsync((User)null);

            _passwordHasher
                .Setup(x => x.Hash(password))
                .Returns("hashed-password");

            _emailService
                .Setup(x => x.SendWelcomeEmailAsync(email))
                .ThrowsAsync(new Exception("SMTP Error"));

            // Act
            var result = await _service.RegisterAsync(email, password);

            // Assert
            Assert.IsTrue(result.Success);
            Assert.IsNotNull(result.UserId);

            _userRepository.Verify(x => x.SaveAsync(It.IsAny<User>()), Times.Once);
            _emailService.Verify(x => x.SendWelcomeEmailAsync(email), Times.Once);
        }

        [TestMethod]
        public async Task LoginAsync_UserNotFound_ReturnsFailure()
        {
            // Arrange
            _userRepository
                .Setup(x => x.GetByEmailAsync("test@test.com"))
                .ReturnsAsync((User)null);

            // Act
            var result = await _service.LoginAsync("test@test.com", "Password123");

            // Assert
            Assert.IsFalse(result.Success);
            Assert.AreEqual("Invalid email or password.", result.FailureReason);

            _userRepository.Verify(x => x.SaveAsync(It.IsAny<User>()), Times.Never);
        }
    }
}
