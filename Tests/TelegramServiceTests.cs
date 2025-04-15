using System;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using FoodTracker.Services;
using Microsoft.Extensions.Configuration;

namespace FoodTracker.Tests
{
    [TestFixture]
    public class TelegramServiceTests
    {
        private Mock<IConfiguration> _mockConfig;
        private Mock<UserService> _mockUserService;
        private Mock<HttpClient> _mockHttpClient;
        private TelegramService _telegramService;

        [SetUp]
        public void Setup()
        {
            _mockConfig = new Mock<IConfiguration>();
            _mockUserService = new Mock<UserService>();
            _mockHttpClient = new Mock<HttpClient>();

            _mockConfig.Setup(c => c["TELEGRAM_BOT_TOKEN"]).Returns("test_token");
            
            _telegramService = new TelegramService(_mockConfig.Object, _mockUserService.Object);
        }

        [Test]
        public async Task ProcessUpdate_WithNullMessage_DoesNothing()
        {
            // Arrange
            var update = new TelegramUpdate { Message = null };

            // Act
            await _telegramService.ProcessUpdate(update);

            // Assert
            _mockUserService.VerifyNoOtherCalls();
        }

        [Test]
        public async Task ProcessCommand_SetupUser_ReturnsExpectedResponse()
        {
            // Arrange
            var expectedResponse = "User setup completed";
            _mockUserService.Setup(s => s.SetupUser(It.IsAny<long>(), It.IsAny<string>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var response = await _telegramService.ProcessCommand("/s 30 M 175 80 70", 12345);

            // Assert
            Assert.AreEqual(expectedResponse, response);
            _mockUserService.Verify(s => s.SetupUser(12345, "30 M 175 80 70"), Times.Once);
        }

        [Test]
        public async Task ProcessCommand_LogWeight_ReturnsExpectedResponse()
        {
            // Arrange
            var expectedResponse = "Weight logged successfully";
            _mockUserService.Setup(s => s.LogWeight(It.IsAny<long>(), It.IsAny<string>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var response = await _telegramService.ProcessCommand("/wi 75", 12345);

            // Assert
            Assert.AreEqual(expectedResponse, response);
            _mockUserService.Verify(s => s.LogWeight(12345, "75"), Times.Once);
        }

        [Test]
        public async Task ProcessCommand_LogFood_ReturnsExpectedResponse()
        {
            // Arrange
            var expectedResponse = "Food logged successfully";
            _mockUserService.Setup(s => s.LogFood(It.IsAny<long>(), It.IsAny<string>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var response = await _telegramService.ProcessCommand("/f apple 52", 12345);

            // Assert
            Assert.AreEqual(expectedResponse, response);
            _mockUserService.Verify(s => s.LogFood(12345, "apple 52"), Times.Once);
        }

        [Test]
        public async Task ProcessCommand_LogExercise_ReturnsExpectedResponse()
        {
            // Arrange
            var expectedResponse = "Exercise logged successfully";
            _mockUserService.Setup(s => s.LogExercise(It.IsAny<long>(), It.IsAny<string>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var response = await _telegramService.ProcessCommand("/e running 30", 12345);

            // Assert
            Assert.AreEqual(expectedResponse, response);
            _mockUserService.Verify(s => s.LogExercise(12345, "running 30"), Times.Once);
        }

        [Test]
        public async Task ProcessCommand_GetHistory_ReturnsExpectedResponse()
        {
            // Arrange
            var expectedResponse = "History data";
            _mockUserService.Setup(s => s.GetHistory(It.IsAny<long>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var response = await _telegramService.ProcessCommand("/h", 12345);

            // Assert
            Assert.AreEqual(expectedResponse, response);
            _mockUserService.Verify(s => s.GetHistory(12345), Times.Once);
        }

        [Test]
        public async Task ProcessCommand_GetProjection_ReturnsExpectedResponse()
        {
            // Arrange
            var expectedResponse = "Projection data";
            _mockUserService.Setup(s => s.GetProjection(It.IsAny<long>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var response = await _telegramService.ProcessCommand("/p", 12345);

            // Assert
            Assert.AreEqual(expectedResponse, response);
            _mockUserService.Verify(s => s.GetProjection(12345), Times.Once);
        }

        [Test]
        public async Task ProcessCommand_GetTheoreticalWeight_ReturnsExpectedResponse()
        {
            // Arrange
            var expectedResponse = "Theoretical weight: 75.5 kg";
            _mockUserService.Setup(s => s.GetTheoreticalWeight(It.IsAny<long>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var response = await _telegramService.ProcessCommand("/tw", 12345);

            // Assert
            Assert.AreEqual(expectedResponse, response);
            _mockUserService.Verify(s => s.GetTheoreticalWeight(12345), Times.Once);
        }

        [Test]
        public async Task ProcessCommand_UnknownCommand_ReturnsHelpMessage()
        {
            // Act
            var response = await _telegramService.ProcessCommand("/unknown", 12345);

            // Assert
            Assert.AreEqual("Unknown command. Use /s, /wi, /f, /e, /h, /p, or /tw", response);
            _mockUserService.VerifyNoOtherCalls();
        }

        [Test]
        public async Task ProcessCommand_WithException_ReturnsErrorMessage()
        {
            // Arrange
            _mockUserService.Setup(s => s.SetupUser(It.IsAny<long>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Test error"));

            // Act
            var response = await _telegramService.ProcessCommand("/s invalid", 12345);

            // Assert
            Assert.AreEqual("Error processing command: Test error", response);
        }
    }
} 