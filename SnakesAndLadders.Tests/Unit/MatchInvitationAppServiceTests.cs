using System;
using Moq;
using ServerSnakesAndLadders.Common;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Enums;
using SnakeAndLadders.Contracts.Interfaces;
using SnakesAndLadders.Services.Logic;
using Xunit;

namespace SnakesAndLadders.Tests.Unit
{
    public sealed class MatchInvitationAppServiceTests : IDisposable
    {
        private const string ERROR_INVALID_REQUEST = "INVITE_INVALID_REQUEST";
        private const string ERROR_INVALID_SESSION = "INVITE_INVALID_SESSION";
        private const string ERROR_INVALID_GAME_CODE = "INVITE_INVALID_GAME_CODE";
        private const string ERROR_INVALID_FRIEND_ID = "INVITE_INVALID_FRIEND_ID";
        private const string ERROR_NOT_FRIENDS = "INVITE_NOT_FRIENDS";
        private const string ERROR_FRIEND_EMAIL_NOT_FOUND =
            "INVITE_FRIEND_EMAIL_NOT_FOUND";
        private const string ERROR_EMAIL_SEND_FAILED = "INVITE_EMAIL_SEND_FAILED";

        private const int EXPECTED_GAME_CODE_LENGTH = 6;
        private const int MIN_VALID_USER_ID = 1;

        private const string VALID_SESSION_TOKEN = "valid-session-token";

        private static readonly int ValidInviterUserId = MIN_VALID_USER_ID + 1;
        private static readonly int ValidFriendUserId = MIN_VALID_USER_ID + 2;

        private readonly Mock<IFriendsRepository> _friendsRepositoryMock;
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<IAccountsRepository> _accountsRepositoryMock;
        private readonly Mock<IEmailSender> _emailSenderMock;
        private readonly Mock<Func<string, int>> _getUserIdFromTokenMock;

        private readonly MatchInvitationAppService _service;

        public MatchInvitationAppServiceTests()
        {
            _friendsRepositoryMock =
                new Mock<IFriendsRepository>(MockBehavior.Strict);

            _userRepositoryMock =
                new Mock<IUserRepository>(MockBehavior.Strict);

            _accountsRepositoryMock =
                new Mock<IAccountsRepository>(MockBehavior.Strict);

            _emailSenderMock =
                new Mock<IEmailSender>(MockBehavior.Strict);

            _getUserIdFromTokenMock =
                new Mock<Func<string, int>>(MockBehavior.Strict);

            _service = new MatchInvitationAppService(
                _friendsRepositoryMock.Object,
                _userRepositoryMock.Object,
                _accountsRepositoryMock.Object,
                _emailSenderMock.Object,
                _getUserIdFromTokenMock.Object);
        }

        public void Dispose()
        {
            _friendsRepositoryMock.VerifyAll();
            _userRepositoryMock.VerifyAll();
            _accountsRepositoryMock.VerifyAll();
            _emailSenderMock.VerifyAll();
            _getUserIdFromTokenMock.VerifyAll();
        }

        #region Constructor

        [Fact]
        public void TestConstructorThrowsWhenFriendsRepositoryIsNull()
        {
            var ex = Assert.Throws<ArgumentNullException>(
                () => new MatchInvitationAppService(
                    null,
                    _userRepositoryMock.Object,
                    _accountsRepositoryMock.Object,
                    _emailSenderMock.Object,
                    _getUserIdFromTokenMock.Object));

            Assert.Equal("friendsRepository", ex.ParamName);
        }

        [Fact]
        public void TestConstructorThrowsWhenUserRepositoryIsNull()
        {
            var ex = Assert.Throws<ArgumentNullException>(
                () => new MatchInvitationAppService(
                    _friendsRepositoryMock.Object,
                    null,
                    _accountsRepositoryMock.Object,
                    _emailSenderMock.Object,
                    _getUserIdFromTokenMock.Object));

            Assert.Equal("userRepository", ex.ParamName);
        }

        [Fact]
        public void TestConstructorThrowsWhenAccountsRepositoryIsNull()
        {
            var ex = Assert.Throws<ArgumentNullException>(
                () => new MatchInvitationAppService(
                    _friendsRepositoryMock.Object,
                    _userRepositoryMock.Object,
                    null,
                    _emailSenderMock.Object,
                    _getUserIdFromTokenMock.Object));

            Assert.Equal("accountsRepository", ex.ParamName);
        }

        [Fact]
        public void TestConstructorThrowsWhenEmailSenderIsNull()
        {
            var ex = Assert.Throws<ArgumentNullException>(
                () => new MatchInvitationAppService(
                    _friendsRepositoryMock.Object,
                    _userRepositoryMock.Object,
                    _accountsRepositoryMock.Object,
                    null,
                    _getUserIdFromTokenMock.Object));

            Assert.Equal("emailSender", ex.ParamName);
        }

        [Fact]
        public void TestConstructorThrowsWhenGetUserIdFromTokenIsNull()
        {
            var ex = Assert.Throws<ArgumentNullException>(
                () => new MatchInvitationAppService(
                    _friendsRepositoryMock.Object,
                    _userRepositoryMock.Object,
                    _accountsRepositoryMock.Object,
                    _emailSenderMock.Object,
                    null));

            Assert.Equal("getUserIdFromToken", ex.ParamName);
        }

        #endregion

        #region InviteFriendToGame – Validations

        [Fact]
        public void TestInviteFriendToGameReturnsInvalidRequestWhenRequestIsNull()
        {
            OperationResult result = _service.InviteFriendToGame(null);

            Assert.False(result.Success);
            Assert.Equal(ERROR_INVALID_REQUEST, result.Message);
        }

        [Fact]
        public void TestInviteFriendToGameReturnsInvalidSessionWhenTokenResolvesToInvalidUserId()
        {
            var request = BuildValidRequest();

            _getUserIdFromTokenMock
                .Setup(f => f(VALID_SESSION_TOKEN))
                .Returns(MIN_VALID_USER_ID - 1);

            OperationResult result = _service.InviteFriendToGame(request);

            Assert.False(result.Success);
            Assert.Equal(ERROR_INVALID_SESSION, result.Message);
        }

        [Fact]
        public void TestInviteFriendToGameReturnsInvalidFriendIdWhenFriendUserIdIsInvalid()
        {
            var request = BuildValidRequest();
            request.FriendUserId = MIN_VALID_USER_ID - 1;

            _getUserIdFromTokenMock
                .Setup(f => f(VALID_SESSION_TOKEN))
                .Returns(ValidInviterUserId);

            OperationResult result = _service.InviteFriendToGame(request);

            Assert.False(result.Success);
            Assert.Equal(ERROR_INVALID_FRIEND_ID, result.Message);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("123")]    // wrong length
        [InlineData("ABC123")] // non-digit
        public void TestInviteFriendToGameReturnsInvalidGameCodeWhenCodeIsInvalid(
            string gameCode)
        {
            var request = BuildValidRequest();
            request.GameCode = gameCode;

            _getUserIdFromTokenMock
                .Setup(f => f(VALID_SESSION_TOKEN))
                .Returns(ValidInviterUserId);

            OperationResult result = _service.InviteFriendToGame(request);

            Assert.False(result.Success);
            Assert.Equal(ERROR_INVALID_GAME_CODE, result.Message);
        }

        #endregion

        #region InviteFriendToGame – Friends / Email validations

        [Fact]
        public void TestInviteFriendToGameReturnsNotFriendsWhenNoFriendLinkExists()
        {
            var request = BuildValidRequest();

            _getUserIdFromTokenMock
                .Setup(f => f(VALID_SESSION_TOKEN))
                .Returns(ValidInviterUserId);

            _friendsRepositoryMock
                .Setup(repo => repo.GetNormalized(
                    ValidInviterUserId,
                    ValidFriendUserId))
                .Returns((FriendLinkDto)null);

            OperationResult result = _service.InviteFriendToGame(request);

            Assert.False(result.Success);
            Assert.Equal(ERROR_NOT_FRIENDS, result.Message);
        }

        [Fact]
        public void TestInviteFriendToGameReturnsNotFriendsWhenLinkStatusIsNotAccepted()
        {
            var request = BuildValidRequest();

            var link = new FriendLinkDto
            {
                Status = FriendRequestStatus.Pending
            };

            _getUserIdFromTokenMock
                .Setup(f => f(VALID_SESSION_TOKEN))
                .Returns(ValidInviterUserId);

            _friendsRepositoryMock
                .Setup(repo => repo.GetNormalized(
                    ValidInviterUserId,
                    ValidFriendUserId))
                .Returns(link);

            OperationResult result = _service.InviteFriendToGame(request);

            Assert.False(result.Success);
            Assert.Equal(ERROR_NOT_FRIENDS, result.Message);
        }

        [Fact]
        public void TestInviteFriendToGameReturnsFriendEmailNotFoundWhenEmailLookupFails()
        {
            var request = BuildValidRequest();

            var link = new FriendLinkDto
            {
                Status = FriendRequestStatus.Accepted
            };

            OperationResult<string> emailResult =
                OperationResult<string>.Failure("lookup failed");

            _getUserIdFromTokenMock
                .Setup(f => f(VALID_SESSION_TOKEN))
                .Returns(ValidInviterUserId);

            _friendsRepositoryMock
                .Setup(repo => repo.GetNormalized(
                    ValidInviterUserId,
                    ValidFriendUserId))
                .Returns(link);

            _accountsRepositoryMock
                .Setup(repo => repo.GetEmailByUserId(ValidFriendUserId))
                .Returns(emailResult);

            OperationResult result = _service.InviteFriendToGame(request);

            Assert.False(result.Success);
            Assert.Equal(ERROR_FRIEND_EMAIL_NOT_FOUND, result.Message);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void TestInviteFriendToGameReturnsFriendEmailNotFoundWhenEmailIsNullOrWhitespace(
            string email)
        {
            var request = BuildValidRequest();

            var link = new FriendLinkDto
            {
                Status = FriendRequestStatus.Accepted
            };

            OperationResult<string> emailResult =
                OperationResult<string>.Success(email);

            _getUserIdFromTokenMock
                .Setup(f => f(VALID_SESSION_TOKEN))
                .Returns(ValidInviterUserId);

            _friendsRepositoryMock
                .Setup(repo => repo.GetNormalized(
                    ValidInviterUserId,
                    ValidFriendUserId))
                .Returns(link);

            _accountsRepositoryMock
                .Setup(repo => repo.GetEmailByUserId(ValidFriendUserId))
                .Returns(emailResult);

            OperationResult result = _service.InviteFriendToGame(request);

            Assert.False(result.Success);
            Assert.Equal(ERROR_FRIEND_EMAIL_NOT_FOUND, result.Message);
        }

        #endregion

        #region InviteFriendToGame – Success and email sending

        [Fact]
        public void TestInviteFriendToGameSendsEmailAndReturnsSuccessWhenAllDataIsValid()
        {
            var request = BuildValidRequest();
            const string friendEmail = "friend@example.com";
            const string inviterUserName = "InviterName";
            const string friendUserName = "FriendName";

            var link = new FriendLinkDto
            {
                Status = FriendRequestStatus.Accepted
            };

            OperationResult<string> emailResult =
                OperationResult<string>.Success(friendEmail);

            var inviterAccount = new AccountDto
            {
                UserId = ValidInviterUserId,
                UserName = inviterUserName
            };

            var friendAccount = new AccountDto
            {
                UserId = ValidFriendUserId,
                UserName = friendUserName
            };

            GameInvitationEmailDto capturedEmail = null;

            _getUserIdFromTokenMock
                .Setup(f => f(VALID_SESSION_TOKEN))
                .Returns(ValidInviterUserId);

            _friendsRepositoryMock
                .Setup(repo => repo.GetNormalized(
                    ValidInviterUserId,
                    ValidFriendUserId))
                .Returns(link);

            _accountsRepositoryMock
                .Setup(repo => repo.GetEmailByUserId(ValidFriendUserId))
                .Returns(emailResult);

            _userRepositoryMock
                .Setup(repo => repo.GetByUserId(ValidInviterUserId))
                .Returns(inviterAccount);

            _userRepositoryMock
                .Setup(repo => repo.GetByUserId(ValidFriendUserId))
                .Returns(friendAccount);

            _emailSenderMock
                .Setup(sender => sender.SendGameInvitation(
                    It.IsAny<GameInvitationEmailDto>()))
                .Callback<GameInvitationEmailDto>(dto => capturedEmail = dto);

            OperationResult result = _service.InviteFriendToGame(request);

            Assert.True(result.Success);
            Assert.Equal(string.Empty, result.Message);

            Assert.NotNull(capturedEmail);
            Assert.Equal(friendEmail, capturedEmail.ToEmail);
            Assert.Equal(friendUserName, capturedEmail.ToUserName);
            Assert.Equal(inviterUserName, capturedEmail.InviterUserName);
            Assert.Equal(request.GameCode, capturedEmail.GameCode);
        }

        [Fact]
        public void TestInviteFriendToGameUsesFallbackUserNamesWhenAccountsAreMissingOrEmpty()
        {
            var request = BuildValidRequest();
            const string friendEmail = "friend@example.com";

            var link = new FriendLinkDto
            {
                Status = FriendRequestStatus.Accepted
            };

            OperationResult<string> emailResult =
                OperationResult<string>.Success(friendEmail);

            AccountDto inviterAccount = null;

            var friendAccount = new AccountDto
            {
                UserId = ValidFriendUserId,
                UserName = " "
            };

            GameInvitationEmailDto capturedEmail = null;

            string expectedInviterName =
                string.Format("User-{0}", ValidInviterUserId);
            string expectedFriendName =
                string.Format("User-{0}", ValidFriendUserId);

            _getUserIdFromTokenMock
                .Setup(f => f(VALID_SESSION_TOKEN))
                .Returns(ValidInviterUserId);

            _friendsRepositoryMock
                .Setup(repo => repo.GetNormalized(
                    ValidInviterUserId,
                    ValidFriendUserId))
                .Returns(link);

            _accountsRepositoryMock
                .Setup(repo => repo.GetEmailByUserId(ValidFriendUserId))
                .Returns(emailResult);

            _userRepositoryMock
                .Setup(repo => repo.GetByUserId(ValidInviterUserId))
                .Returns(inviterAccount);

            _userRepositoryMock
                .Setup(repo => repo.GetByUserId(ValidFriendUserId))
                .Returns(friendAccount);

            _emailSenderMock
                .Setup(sender => sender.SendGameInvitation(
                    It.IsAny<GameInvitationEmailDto>()))
                .Callback<GameInvitationEmailDto>(dto => capturedEmail = dto);

            OperationResult result = _service.InviteFriendToGame(request);

            Assert.True(result.Success);
            Assert.Equal(string.Empty, result.Message);

            Assert.NotNull(capturedEmail);
            Assert.Equal(expectedFriendName, capturedEmail.ToUserName);
            Assert.Equal(expectedInviterName, capturedEmail.InviterUserName);
        }

        [Fact]
        public void TestInviteFriendToGameReturnsEmailSendFailedWhenEmailSenderThrows()
        {
            var request = BuildValidRequest();
            const string friendEmail = "friend@example.com";

            var link = new FriendLinkDto
            {
                Status = FriendRequestStatus.Accepted
            };

            OperationResult<string> emailResult =
                OperationResult<string>.Success(friendEmail);

            var inviterAccount = new AccountDto
            {
                UserId = ValidInviterUserId,
                UserName = "Inviter"
            };

            var friendAccount = new AccountDto
            {
                UserId = ValidFriendUserId,
                UserName = "Friend"
            };

            _getUserIdFromTokenMock
                .Setup(f => f(VALID_SESSION_TOKEN))
                .Returns(ValidInviterUserId);

            _friendsRepositoryMock
                .Setup(repo => repo.GetNormalized(
                    ValidInviterUserId,
                    ValidFriendUserId))
                .Returns(link);

            _accountsRepositoryMock
                .Setup(repo => repo.GetEmailByUserId(ValidFriendUserId))
                .Returns(emailResult);

            _userRepositoryMock
                .Setup(repo => repo.GetByUserId(ValidInviterUserId))
                .Returns(inviterAccount);

            _userRepositoryMock
                .Setup(repo => repo.GetByUserId(ValidFriendUserId))
                .Returns(friendAccount);

            _emailSenderMock
                .Setup(sender => sender.SendGameInvitation(
                    It.IsAny<GameInvitationEmailDto>()))
                .Throws(new InvalidOperationException("send failed"));

            OperationResult result = _service.InviteFriendToGame(request);

            Assert.False(result.Success);
            Assert.Equal(ERROR_EMAIL_SEND_FAILED, result.Message);
        }

        #endregion

        #region Helpers

        private static InviteFriendToGameRequestDto BuildValidRequest()
        {
            return new InviteFriendToGameRequestDto
            {
                SessionToken = VALID_SESSION_TOKEN,
                FriendUserId = ValidFriendUserId,
                GameCode = BuildValidGameCode()
            };
        }

        private static string BuildValidGameCode()
        {
            return new string('1', EXPECTED_GAME_CODE_LENGTH);
        }

        #endregion
    }
}
