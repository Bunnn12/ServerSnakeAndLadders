using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Moq;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Interfaces;
using SnakesAndLadders.Services.Logic;
using Xunit;

namespace SnakesAndLadders.Tests.Unit
{
    public sealed class ChatAppServiceTests
    {
        private const int DefaultRecentMessagesCount = 100;
        private const int MaxMessageLength = 500;

        private readonly Mock<IChatRepository> _chatRepositoryMock;
        private readonly ChatAppService _service;

        public ChatAppServiceTests()
        {
            _chatRepositoryMock = new Mock<IChatRepository>(MockBehavior.Strict);

            _service = new ChatAppService(_chatRepositoryMock.Object);
        }

        #region Send

        [Fact]
        public void TestSendDoesNothingWhenMessageIsNull()
        {
            _service.Send(1, null);

            _chatRepositoryMock.Verify(
                repo => repo.SaveMessage(It.IsAny<int>(), It.IsAny<ChatMessageDto>()),
                Times.Never);
        }

        [Fact]
        public void TestSendDoesNothingWhenMessageHasNoTextAndNoSticker()
        {
            var message = new ChatMessageDto
            {
                Text = "   ",
                StickerId = 0,
                StickerCode = null
            };

            _service.Send(1, message);

            _chatRepositoryMock.Verify(
                repo => repo.SaveMessage(It.IsAny<int>(), It.IsAny<ChatMessageDto>()),
                Times.Never);
        }

        [Fact]
        public void TestSendDoesNothingWhenMessageTextTooLong()
        {
            var message = new ChatMessageDto
            {
                Text = new string('a', MaxMessageLength + 1),
                StickerId = 0,
                StickerCode = null
            };

            _service.Send(1, message);

            _chatRepositoryMock.Verify(
                repo => repo.SaveMessage(It.IsAny<int>(), It.IsAny<ChatMessageDto>()),
                Times.Never);
        }

        [Fact]
        public void TestSendSavesMessageWhenValidTextMessage()
        {
            var originalText = "  hola mundo  ";
            ChatMessageDto savedMessage = null;

            _chatRepositoryMock
                .Setup(repo => repo.SaveMessage(10, It.IsAny<ChatMessageDto>()))
                .Callback<int, ChatMessageDto>((_, msg) => savedMessage = msg);

            var message = new ChatMessageDto
            {
                Text = originalText,
                StickerId = 0,
                StickerCode = null,
                SenderAvatarId = "3"
            };

            DateTime beforeSendUtc = DateTime.UtcNow;

            _service.Send(10, message);

            _chatRepositoryMock.Verify(
                repo => repo.SaveMessage(10, It.IsAny<ChatMessageDto>()),
                Times.Once);

            Assert.True(
                savedMessage != null
                && savedMessage.Text == "hola mundo"
                && savedMessage.TimestampUtc >= beforeSendUtc);
        }

        [Fact]
        public void TestSendSavesMessageWhenStickerOnlyValid()
        {
            var message = new ChatMessageDto
            {
                Text = "   ",
                StickerId = 1,
                StickerCode = "STICKER_01",
                SenderAvatarId = "2"
            };

            _chatRepositoryMock
                .Setup(repo => repo.SaveMessage(5, It.IsAny<ChatMessageDto>()));

            _service.Send(5, message);

            _chatRepositoryMock.Verify(
                repo => repo.SaveMessage(5, It.IsAny<ChatMessageDto>()),
                Times.Once);
        }

        #endregion

        #region GetRecent

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void TestGetRecentUsesDefaultCountWhenTakeNonPositive(int requestedTake)
        {
            int usedTake = -1;

            _chatRepositoryMock
                .Setup(repo => repo.ReadLast(20, It.IsAny<int>()))
                .Callback<int, int>((_, take) => usedTake = take)
                .Returns(new List<ChatMessageDto>());

            _service.GetRecent(20, requestedTake);

            Assert.True(usedTake == DefaultRecentMessagesCount);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(50)]
        public void TestGetRecentPassesRequestedTakeWhenPositive(int requestedTake)
        {
            int usedTake = -1;

            _chatRepositoryMock
                .Setup(repo => repo.ReadLast(30, It.IsAny<int>()))
                .Callback<int, int>((_, take) => usedTake = take)
                .Returns(new List<ChatMessageDto>());

            _service.GetRecent(30, requestedTake);

            Assert.True(usedTake == requestedTake);
        }

        [Theory]
        [InlineData("io")]
        [InlineData("unauthorized")]
        [InlineData("other")]
        public void TestGetRecentReturnsEmptyListWhenRepositoryThrows(string scenario)
        {
            Exception exceptionToThrow;

            switch (scenario)
            {
                case "io":
                    exceptionToThrow = new IOException("IO error");
                    break;
                case "unauthorized":
                    exceptionToThrow = new UnauthorizedAccessException("Access denied");
                    break;
                default:
                    exceptionToThrow = new InvalidOperationException("Other error");
                    break;
            }

            _chatRepositoryMock
                .Setup(repo => repo.ReadLast(1, It.IsAny<int>()))
                .Throws(exceptionToThrow);

            IList<ChatMessageDto> result = _service.GetRecent(1, 5);

            Assert.True(result != null && result.Count == 0);
        }

        [Fact]
        public void TestGetRecentReturnsEmptyListWhenRepositoryReturnsNull()
        {
            _chatRepositoryMock
                .Setup(repo => repo.ReadLast(2, It.IsAny<int>()))
                .Returns((IList<ChatMessageDto>)null);

            IList<ChatMessageDto> result = _service.GetRecent(2, 5);

            Assert.True(result != null && result.Count == 0);
        }

        [Fact]
        public void TestGetRecentReturnsMessagesWhenRepositorySucceeds()
        {
            var messages = new List<ChatMessageDto>
            {
                new ChatMessageDto { Text = "Hola", SenderAvatarId = "1" },
                new ChatMessageDto { Text = "Mundo", SenderAvatarId = "2" }
            };

            _chatRepositoryMock
                .Setup(repo => repo.ReadLast(3, It.IsAny<int>()))
                .Returns(messages);

            IList<ChatMessageDto> result = _service.GetRecent(3, 10);

            Assert.Equal(2, result.Count);
        }

        #endregion

        #region NormalizeMessageText (private, vía reflexión)

        private static string InvokeNormalizeMessageText(string text)
        {
            MethodInfo methodInfo = typeof(ChatAppService)
                .GetMethod(
                    "NormalizeMessageText",
                    BindingFlags.NonPublic | BindingFlags.Static);

            return (string)methodInfo.Invoke(null, new object[] { text });
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void TestNormalizeMessageTextReturnsEmptyForNullOrWhiteSpace(string input)
        {
            string result = InvokeNormalizeMessageText(input);

            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void TestNormalizeMessageTextTrimsInnerText()
        {
            const string input = "  hola  ";
            string result = InvokeNormalizeMessageText(input);

            Assert.Equal("hola", result);
        }

        [Fact]
        public void TestNormalizeMessageTextTruncatesWhenLongerThanMax()
        {
            string input = new string('x', MaxMessageLength + 50);

            string result = InvokeNormalizeMessageText(input);

            Assert.Equal(MaxMessageLength, result.Length);
        }

        #endregion
    }
}
