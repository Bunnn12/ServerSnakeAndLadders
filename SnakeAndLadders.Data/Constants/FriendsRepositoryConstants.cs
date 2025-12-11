using SnakeAndLadders.Contracts.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakesAndLadders.Data.Constants
{
    internal static class FriendsRepositoryConstants
    {
        public const byte FRIEND_REQUEST_STATUS_PENDING_VALUE = 0x01;
        public const byte FRIEND_REQUEST_STATUS_ACCEPTED_VALUE = 0x02;

        public const int COMMAND_TIMEOUT_SECONDS = 30;
        public const int DEFAULT_SEARCH_MAX_RESULTS = 20;
        public const int MAX_SEARCH_RESULTS = 100;
        public const int MIN_VALID_USER_ID = 1;
        public const int STATUS_MIN_LENGTH = 1;
        public const int STATUS_INDEX = 0;
        public const int EXPECTED_USER_COUNT_FOR_LINK = 2;
        public const int DEFAULT_FRIEND_LINK_ID = 0;
        public const int DEFAULT_USER_ID = 0;

        public const string ERROR_USERS_NOT_FOUND = "Users not found.";
        public const string ERROR_PENDING_ALREADY_EXISTS = "Pending friend request already exists.";
        public const string ERROR_ALREADY_FRIENDS = "Users are already friends.";
        public const string ERROR_FRIEND_LINK_NOT_FOUND = "Friend link not found.";
        public const string ERROR_USER_ID_POSITIVE = "UserId must be positive.";

        public const FriendRequestStatus DEFAULT_FRIEND_REQUEST_STATUS = FriendRequestStatus.Pending;
    }
}
