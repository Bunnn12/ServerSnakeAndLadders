using System;
using log4net;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Enums;
using SnakeAndLadders.Contracts.Interfaces;

namespace SnakesAndLadders.Services.Logic
{
    public sealed class MatchInvitationAppService : IMatchInvitationAppService
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(MatchInvitationAppService));

        private const string ERROR_INVALID_REQUEST = "INVITE_INVALID_REQUEST";
        private const string ERROR_INVALID_SESSION = "INVITE_INVALID_SESSION";
        private const string ERROR_INVALID_GAME_CODE = "INVITE_INVALID_GAME_CODE";
        private const string ERROR_INVALID_FRIEND_ID = "INVITE_INVALID_FRIEND_ID";
        private const string ERROR_NOT_FRIENDS = "INVITE_NOT_FRIENDS";
        private const string ERROR_FRIEND_EMAIL_NOT_FOUND = "INVITE_FRIEND_EMAIL_NOT_FOUND";
        private const string ERROR_EMAIL_SEND_FAILED = "INVITE_EMAIL_SEND_FAILED";

        private const int EXPECTED_GAME_CODE_LENGTH = 6;

        private readonly IFriendsRepository friendsRepository;
        private readonly IUserRepository userRepository;
        private readonly IAccountsRepository accountsRepository;
        private readonly IEmailSender emailSender;
        private readonly Func<string, int> getUserIdFromToken;

        public MatchInvitationAppService(
            IFriendsRepository friendsRepository,
            IUserRepository userRepository,
            IAccountsRepository accountsRepository,
            IEmailSender emailSender,
            Func<string, int> getUserIdFromToken)
        {
            this.friendsRepository = friendsRepository
                ?? throw new ArgumentNullException(nameof(friendsRepository));
            this.userRepository = userRepository
                ?? throw new ArgumentNullException(nameof(userRepository));
            this.accountsRepository = accountsRepository
                ?? throw new ArgumentNullException(nameof(accountsRepository));
            this.emailSender = emailSender
                ?? throw new ArgumentNullException(nameof(emailSender));
            this.getUserIdFromToken = getUserIdFromToken
                ?? throw new ArgumentNullException(nameof(getUserIdFromToken));
        }

        public OperationResult InviteFriendToGame(InviteFriendToGameRequestDto request)
        {
            if (request == null)
            {
                return BuildFailure(ERROR_INVALID_REQUEST);
            }

            int inviterUserId = getUserIdFromToken(request.SessionToken);
            if (inviterUserId <= 0)
            {
                return BuildFailure(ERROR_INVALID_SESSION);
            }

            if (request.FriendUserId <= 0)
            {
                return BuildFailure(ERROR_INVALID_FRIEND_ID);
            }

            string gameCode = (request.GameCode ?? string.Empty).Trim();
            if (!IsValidGameCode(gameCode))
            {
                return BuildFailure(ERROR_INVALID_GAME_CODE);
            }

            if (!AreUsersFriends(inviterUserId, request.FriendUserId))
            {
                return BuildFailure(ERROR_NOT_FRIENDS);
            }

            string friendEmail = accountsRepository.GetEmailByUserId(request.FriendUserId);
            if (string.IsNullOrWhiteSpace(friendEmail))
            {
                return BuildFailure(ERROR_FRIEND_EMAIL_NOT_FOUND);
            }

            AccountDto inviterAccount = userRepository.GetByUserId(inviterUserId);
            AccountDto friendAccount = userRepository.GetByUserId(request.FriendUserId);

            string inviterUserName = ResolveUserName(inviterAccount, inviterUserId);
            string friendUserName = ResolveUserName(friendAccount, request.FriendUserId);

            var emailRequest = new GameInvitationEmailDto
            {
                ToEmail = friendEmail,
                ToUserName = friendUserName,
                InviterUserName = inviterUserName,
                GameCode = gameCode
            };

            try
            {
                emailSender.SendGameInvitation(emailRequest);

                Logger.InfoFormat(
                    "Game invitation sent. GameCode={0}, Inviter={1}, Friend={2}",
                    gameCode,
                    inviterUserId,
                    request.FriendUserId);

                return new OperationResult
                {
                    Success = true,
                    Message = string.Empty
                };
            }
            catch (Exception ex)
            {
                Logger.Error("Error sending game invitation email.", ex);
                return BuildFailure(ERROR_EMAIL_SEND_FAILED);
            }
        }

        private static bool IsValidGameCode(string gameCode)
        {
            if (string.IsNullOrWhiteSpace(gameCode))
            {
                return false;
            }

            if (gameCode.Length != EXPECTED_GAME_CODE_LENGTH)
            {
                return false;
            }

            foreach (char c in gameCode)
            {
                if (!char.IsDigit(c))
                {
                    return false;
                }
            }

            return true;
        }

        private bool AreUsersFriends(int inviterUserId, int friendUserId)
        {
            var link = friendsRepository.GetNormalized(inviterUserId, friendUserId);

            return link != null &&
                   link.Status == FriendRequestStatus.Accepted;
        }

        private static string ResolveUserName(AccountDto account, int userIdFallback)
        {
            if (account != null && !string.IsNullOrWhiteSpace(account.Username))
            {
                return account.Username;
            }

            return $"User-{userIdFallback}";
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
