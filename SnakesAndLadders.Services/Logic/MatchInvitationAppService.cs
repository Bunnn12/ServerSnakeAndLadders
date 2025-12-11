using System;
using log4net;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Enums;
using SnakeAndLadders.Contracts.Interfaces;
using SnakesAndLadders.Services.Constants;

namespace SnakesAndLadders.Services.Logic
{

    public sealed class MatchInvitationAppService : IMatchInvitationAppService
    {
        private static readonly ILog _logger =
            LogManager.GetLogger(typeof(MatchInvitationAppService));

        private readonly IFriendsRepository _friendsRepository;
        private readonly IUserRepository _userRepository;
        private readonly IAccountsRepository _accountsRepository;
        private readonly IEmailSender _emailSender;
        private readonly Func<string, int> _getUserIdFromToken;

        public MatchInvitationAppService(
            IFriendsRepository friendsRepository,
            IUserRepository userRepository,
            IAccountsRepository accountsRepository,
            IEmailSender emailSender,
            Func<string, int> getUserIdFromToken)
        {
            _friendsRepository = friendsRepository
                ?? throw new ArgumentNullException(nameof(friendsRepository));
            _userRepository = userRepository
                ?? throw new ArgumentNullException(nameof(userRepository));
            _accountsRepository = accountsRepository
                ?? throw new ArgumentNullException(nameof(accountsRepository));
            _emailSender = emailSender
                ?? throw new ArgumentNullException(nameof(emailSender));
            _getUserIdFromToken = getUserIdFromToken
                ?? throw new ArgumentNullException(nameof(getUserIdFromToken));
        }

        public OperationResult InviteFriendToGame(InviteFriendToGameRequestDto request)
        {
            if (request == null)
            {
                return BuildFailure(MatchInvitationConstants.ERROR_INVALID_REQUEST);
            }

            int inviterUserId = _getUserIdFromToken(request.SessionToken);
            if (!IsValidUserId(inviterUserId))
            {
                return BuildFailure(MatchInvitationConstants.ERROR_INVALID_SESSION);
            }

            if (!IsValidUserId(request.FriendUserId))
            {
                return BuildFailure(MatchInvitationConstants.ERROR_INVALID_FRIEND_ID);
            }

            string gameCode = NormalizeGameCode(request.GameCode);
            if (!IsValidGameCode(gameCode))
            {
                return BuildFailure(MatchInvitationConstants.ERROR_INVALID_GAME_CODE);
            }

            if (!AreUsersFriends(inviterUserId, request.FriendUserId))
            {
                return BuildFailure(MatchInvitationConstants.ERROR_NOT_FRIENDS);
            }

            string friendEmail = GetFriendEmailOrDefault(request.FriendUserId);
            if (string.IsNullOrWhiteSpace(friendEmail))
            {
                return BuildFailure(MatchInvitationConstants.ERROR_FRIEND_EMAIL_NOT_FOUND);
            }

            AccountDto inviterAccount = _userRepository.GetByUserId(inviterUserId);
            AccountDto friendAccount = _userRepository.GetByUserId(request.FriendUserId);

            string inviterUserName = ResolveUserName(inviterAccount, inviterUserId);
            string friendUserName = ResolveUserName(friendAccount, request.FriendUserId);

            var emailRequest = BuildEmailRequest(
                friendEmail,
                friendUserName,
                inviterUserName,
                gameCode);

            return SendInvitationEmail(emailRequest, inviterUserId, request.FriendUserId);
        }

        private static bool IsValidUserId(int userId)
        {
            return userId >= MatchInvitationConstants.MIN_VALID_USER_ID;
        }

        private static string NormalizeGameCode(string gameCode)
        {
            return (gameCode ?? string.Empty).Trim();
        }

        private static bool IsValidGameCode(string gameCode)
        {
            if (string.IsNullOrWhiteSpace(gameCode))
            {
                return false;
            }

            if (gameCode.Length != MatchInvitationConstants.EXPECTED_GAME_CODE_LENGTH)
            {
                return false;
            }

            foreach (char character in gameCode)
            {
                if (!char.IsDigit(character))
                {
                    return false;
                }
            }

            return true;
        }

        private bool AreUsersFriends(int inviterUserId, int friendUserId)
        {
            var link = _friendsRepository.GetNormalized(inviterUserId, friendUserId);

            return link != null &&
                   link.Status == FriendRequestStatus.Accepted;
        }

        private string GetFriendEmailOrDefault(int friendUserId)
        {
            var emailResult = _accountsRepository.GetEmailByUserId(friendUserId);

            if (!emailResult.IsSuccess)
            {
                return string.Empty;
            }

            return emailResult.Data ?? string.Empty;
        }

        private static string ResolveUserName(AccountDto account, int userIdFallback)
        {
            if (account != null && !string.IsNullOrWhiteSpace(account.UserName))
            {
                return account.UserName;
            }

            return string.Format(
                MatchInvitationConstants.FALLBACK_USERNAME_FORMAT,
                userIdFallback);
        }

        private static GameInvitationEmailDto BuildEmailRequest(
            string friendEmail,
            string friendUserName,
            string inviterUserName,
            string gameCode)
        {
            return new GameInvitationEmailDto
            {
                ToEmail = friendEmail,
                ToUserName = friendUserName,
                InviterUserName = inviterUserName,
                GameCode = gameCode
            };
        }

        private OperationResult SendInvitationEmail(
            GameInvitationEmailDto emailRequest,
            int inviterUserId,
            int friendUserId)
        {
            try
            {
                _emailSender.SendGameInvitation(emailRequest);

                _logger.InfoFormat(
                    MatchInvitationConstants.LOG_GAME_INVITATION_SENT,
                    emailRequest.GameCode,
                    inviterUserId,
                    friendUserId);

                return BuildSuccess();
            }
            catch (Exception ex)
            {
                _logger.Error(MatchInvitationConstants.LOG_ERROR_SENDING_INVITATION, ex);
                return BuildFailure(MatchInvitationConstants.ERROR_EMAIL_SEND_FAILED);
            }
        }

        private static OperationResult BuildSuccess()
        {
            return new OperationResult
            {
                Success = true,
                Message = string.Empty
            };
        }

        private static OperationResult BuildFailure(string errorCode)
        {
            return new OperationResult
            {
                Success = false,
                Message = errorCode
            };
        }
    }
}
