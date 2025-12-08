using System;
using System.Collections.Generic;
using System.Linq;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Enums;
using SnakesAndLadders.Data;
using SnakesAndLadders.Data.Repositories;
using SnakesAndLadders.Tests.integration;
using Xunit;

namespace SnakesAndLadders.Tests.Integration
{
    public sealed class FriendsRepositoryTests : IntegrationTestBase
    {
        private const byte STATUS_ACTIVE = 0x01;
        private const byte FRIEND_STATUS_PENDING = 0x01;
        private const byte FRIEND_STATUS_ACCEPTED = 0x02;

        private const int INVALID_USER_ID_ZERO = 0;
        private const int INVALID_USER_ID_NEGATIVE = -1;

        private const string BASE_USERNAME = "FriendUser";
        private const string BASE_FIRST_NAME = "Friend";
        private const string BASE_LAST_NAME = "User";

        private FriendsRepository CreateRepository()
        {
            return new FriendsRepository(CreateContext);
        }

        private int CreateUser(string suffix)
        {
            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                Usuario user = new Usuario
                {
                    NombreUsuario = $"{BASE_USERNAME}_{suffix}",
                    Nombre = BASE_FIRST_NAME,
                    Apellidos = BASE_LAST_NAME,
                    DescripcionPerfil = null,
                    FotoPerfil = null,
                    Monedas = 0,
                    Estado = new[] { STATUS_ACTIVE },
                    IdAvatarDesbloqueadoActual = null
                };

                db.Usuario.Add(user);
                db.SaveChanges();

                return user.IdUsuario;
            }
        }

        private int CreateUser()
        {
            string suffix = Guid.NewGuid().ToString("N").Substring(0, 8);
            return CreateUser(suffix);
        }

        private ListaAmigos InsertFriendLink(int userId1, int userId2, byte status)
        {
            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                ListaAmigos link = new ListaAmigos
                {
                    UsuarioIdUsuario1 = userId1,
                    UsuarioIdUsuario2 = userId2,
                    EstadoSolicitud = new[] { status }
                };

                db.ListaAmigos.Add(link);
                db.SaveChanges();

                return link;
            }
        }

        // GetById

        [Fact]
        public void TestGetByIdWhenFriendLinkDoesNotExistReturnsEmptyDto()
        {
            FriendsRepository repository = CreateRepository();

            FriendLinkDto result = repository.GetById(999999);

            bool isOk =
                result != null &&
                result.FriendLinkId == 0 &&
                result.UserId1 == 0 &&
                result.UserId2 == 0 &&
                result.Status == FriendRequestStatus.Pending;

            Assert.True(isOk);
        }

        [Fact]
        public void TestGetByIdWhenFriendLinkExistsReturnsMappedDto()
        {
            int userId1 = CreateUser();
            int userId2 = CreateUser();

            ListaAmigos link = InsertFriendLink(userId1, userId2, FRIEND_STATUS_ACCEPTED);

            FriendsRepository repository = CreateRepository();

            FriendLinkDto result = repository.GetById(link.IdListaAmigos);

            bool isOk =
                result != null &&
                result.FriendLinkId == link.IdListaAmigos &&
                result.UserId1 == userId1 &&
                result.UserId2 == userId2 &&
                result.Status == FriendRequestStatus.Accepted;

            Assert.True(isOk);
        }

        // GetNormalized

        [Fact]
        public void TestGetNormalizedWhenLinkDoesNotExistReturnsEmptyDto()
        {
            FriendsRepository repository = CreateRepository();

            FriendLinkDto result = repository.GetNormalized(100, 200);

            bool isOk =
                result != null &&
                result.FriendLinkId == 0 &&
                result.UserId1 == 0 &&
                result.UserId2 == 0;

            Assert.True(isOk);
        }

        [Fact]
        public void TestGetNormalizedReturnsSameLinkRegardlessOfOrder()
        {
            int userId1 = CreateUser();
            int userId2 = CreateUser();

            ListaAmigos link = InsertFriendLink(userId1, userId2, FRIEND_STATUS_PENDING);

            FriendsRepository repository = CreateRepository();

            FriendLinkDto result = repository.GetNormalized(userId2, userId1);

            bool isOk =
                result != null &&
                result.FriendLinkId == link.IdListaAmigos &&
                ((result.UserId1 == userId1 && result.UserId2 == userId2) ||
                 (result.UserId1 == userId2 && result.UserId2 == userId1));

            Assert.True(isOk);
        }

        // CreatePending – validaciones

        [Theory]
        [InlineData(INVALID_USER_ID_ZERO, 2)]
        [InlineData(INVALID_USER_ID_NEGATIVE, 2)]
        public void TestCreatePendingWhenRequesterIdLessOrEqualZeroThrowsArgumentOutOfRange(int invalidRequesterId, int dummy)
        {
            FriendsRepository repository = CreateRepository();

            Action action = () => repository.CreatePending(invalidRequesterId, dummy);

            Assert.Throws<ArgumentOutOfRangeException>(action);
        }

        [Theory]
        [InlineData(INVALID_USER_ID_ZERO)]
        [InlineData(INVALID_USER_ID_NEGATIVE)]
        public void TestCreatePendingWhenTargetIdLessOrEqualZeroThrowsArgumentOutOfRange(int invalidTargetId)
        {
            FriendsRepository repository = CreateRepository();

            int requesterId = 1;

            Action action = () => repository.CreatePending(requesterId, invalidTargetId);

            Assert.Throws<ArgumentOutOfRangeException>(action);
        }

        [Fact]
        public void TestCreatePendingWhenUsersDoNotExistThrowsInvalidOperationException()
        {
            FriendsRepository repository = CreateRepository();

            Action action = () => repository.CreatePending(12345, 67890);

            Assert.Throws<InvalidOperationException>(action);
        }

        [Fact]
        public void TestCreatePendingWhenNoExistingLinkCreatesPendingLink()
        {
            int requesterId = CreateUser();
            int targetId = CreateUser();

            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                db.ListaAmigos.RemoveRange(db.ListaAmigos);
                db.SaveChanges();
            }

            FriendsRepository repository = CreateRepository();

            FriendLinkDto result = repository.CreatePending(requesterId, targetId);

            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                ListaAmigos stored = db.ListaAmigos.Single(l => l.IdListaAmigos == result.FriendLinkId);
                bool isOk =
                    result != null &&
                    result.FriendLinkId > 0 &&
                    stored.UsuarioIdUsuario1 == requesterId &&
                    stored.UsuarioIdUsuario2 == targetId &&
                    stored.EstadoSolicitud[0] == FRIEND_STATUS_PENDING &&
                    result.Status == FriendRequestStatus.Pending;
                Assert.True(isOk);
            }
        }

        [Fact]
        public void TestCreatePendingWhenExistingPendingSameDirectionThrowsInvalidOperationException()
        {
            int requesterId = CreateUser();
            int targetId = CreateUser();

            InsertFriendLink(requesterId, targetId, FRIEND_STATUS_PENDING);

            FriendsRepository repository = CreateRepository();

            Action action = () => repository.CreatePending(requesterId, targetId);

            Assert.Throws<InvalidOperationException>(action);
        }

        [Fact]
        public void TestCreatePendingWhenExistingPendingReverseDirectionAcceptsFriendship()
        {
            int userA = CreateUser();
            int userB = CreateUser();

            ListaAmigos link = InsertFriendLink(userB, userA, FRIEND_STATUS_PENDING);

            FriendsRepository repository = CreateRepository();

            FriendLinkDto result = repository.CreatePending(userA, userB);

            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                ListaAmigos stored = db.ListaAmigos.Single(l => l.IdListaAmigos == link.IdListaAmigos);

                bool isOk =
                    result != null &&
                    result.FriendLinkId == link.IdListaAmigos &&
                    stored.EstadoSolicitud[0] == FRIEND_STATUS_ACCEPTED &&
                    result.Status == FriendRequestStatus.Accepted;
                Assert.True(isOk);
            }
        }

        [Fact]
        public void TestCreatePendingWhenExistingAcceptedThrowsInvalidOperationException()
        {
            int userA = CreateUser();
            int userB = CreateUser();

            InsertFriendLink(userA, userB, FRIEND_STATUS_ACCEPTED);

            FriendsRepository repository = CreateRepository();

            Action action = () => repository.CreatePending(userA, userB);

            Assert.Throws<InvalidOperationException>(action);
        }

        [Fact]
        public void TestCreatePendingWhenExistingWithUnknownStatusResetsToPendingWithNewDirection()
        {
            int originalUser1 = CreateUser();
            int originalUser2 = CreateUser();

            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                ListaAmigos weird = new ListaAmigos
                {
                    UsuarioIdUsuario1 = originalUser2,
                    UsuarioIdUsuario2 = originalUser1,
                    EstadoSolicitud = new byte[] { 0x09 }
                };

                db.ListaAmigos.Add(weird);
                db.SaveChanges();
            }

            int requesterId = originalUser1;
            int targetId = originalUser2;

            FriendsRepository repository = CreateRepository();

            FriendLinkDto result = repository.CreatePending(requesterId, targetId);

            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                ListaAmigos stored = db.ListaAmigos.Single();
                bool isOk =
                    result != null &&
                    stored.UsuarioIdUsuario1 == requesterId &&
                    stored.UsuarioIdUsuario2 == targetId &&
                    stored.EstadoSolicitud[0] == FRIEND_STATUS_PENDING &&
                    result.Status == FriendRequestStatus.Pending;
                Assert.True(isOk);
            }
        }

        // UpdateStatus

        [Fact]
        public void TestUpdateStatusWhenFriendLinkNotFoundThrowsInvalidOperationException()
        {
            FriendsRepository repository = CreateRepository();

            Action action = () => repository.UpdateStatus(999999, FRIEND_STATUS_ACCEPTED);

            Assert.Throws<InvalidOperationException>(action);
        }

        [Fact]
        public void TestUpdateStatusWhenFriendLinkExistsUpdatesStatus()
        {
            int userId1 = CreateUser();
            int userId2 = CreateUser();

            ListaAmigos link = InsertFriendLink(userId1, userId2, FRIEND_STATUS_PENDING);

            FriendsRepository repository = CreateRepository();

            repository.UpdateStatus(link.IdListaAmigos, FRIEND_STATUS_ACCEPTED);

            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                ListaAmigos stored = db.ListaAmigos.Single(l => l.IdListaAmigos == link.IdListaAmigos);
                bool isOk = stored.EstadoSolicitud[0] == FRIEND_STATUS_ACCEPTED;
                Assert.True(isOk);
            }
        }

        // DeleteLink

        [Fact]
        public void TestDeleteLinkWhenFriendLinkNotFoundDoesNothing()
        {
            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                db.ListaAmigos.RemoveRange(db.ListaAmigos);
                db.SaveChanges();
            }

            FriendsRepository repository = CreateRepository();

            repository.DeleteLink(999999);

            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                bool isOk = !db.ListaAmigos.Any();
                Assert.True(isOk);
            }
        }

        [Fact]
        public void TestDeleteLinkWhenFriendLinkExistsRemovesRow()
        {
            int userId1 = CreateUser();
            int userId2 = CreateUser();

            ListaAmigos link = InsertFriendLink(userId1, userId2, FRIEND_STATUS_PENDING);

            FriendsRepository repository = CreateRepository();

            repository.DeleteLink(link.IdListaAmigos);

            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                bool isOk = !db.ListaAmigos.Any(l => l.IdListaAmigos == link.IdListaAmigos);
                Assert.True(isOk);
            }
        }

        // GetAcceptedFriendsIds

        [Theory]
        [InlineData(INVALID_USER_ID_ZERO)]
        [InlineData(INVALID_USER_ID_NEGATIVE)]
        public void TestGetAcceptedFriendsIdsWhenUserIdLessOrEqualZeroThrowsArgumentOutOfRange(int invalidUserId)
        {
            FriendsRepository repository = CreateRepository();

            Action action = () => repository.GetAcceptedFriendsIds(invalidUserId);

            Assert.Throws<ArgumentOutOfRangeException>(action);
        }

        [Fact]
        public void TestGetAcceptedFriendsIdsWhenNoFriendsReturnsEmptyList()
        {
            int userId = CreateUser();

            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                db.ListaAmigos.RemoveRange(db.ListaAmigos);
                db.SaveChanges();
            }

            FriendsRepository repository = CreateRepository();

            IReadOnlyList<int> result = repository.GetAcceptedFriendsIds(userId);

            bool isOk = result != null && result.Count == 0;
            Assert.True(isOk);
        }

        [Fact]
        public void TestGetAcceptedFriendsIdsReturnsOnlyAcceptedFriendIds()
        {
            int userId = CreateUser();
            int friendAccepted = CreateUser();
            int friendPending = CreateUser();

            InsertFriendLink(userId, friendAccepted, FRIEND_STATUS_ACCEPTED);
            InsertFriendLink(userId, friendPending, FRIEND_STATUS_PENDING);

            FriendsRepository repository = CreateRepository();

            IReadOnlyList<int> result = repository.GetAcceptedFriendsIds(userId);

            bool isOk =
                result != null &&
                result.Count == 1 &&
                result[0] == friendAccepted;
            Assert.True(isOk);
        }

        // GetPendingRelated

        [Theory]
        [InlineData(INVALID_USER_ID_ZERO)]
        [InlineData(INVALID_USER_ID_NEGATIVE)]
        public void TestGetPendingRelatedWhenUserIdLessOrEqualZeroThrowsArgumentOutOfRange(int invalidUserId)
        {
            FriendsRepository repository = CreateRepository();

            Action action = () => repository.GetPendingRelated(invalidUserId);

            Assert.Throws<ArgumentOutOfRangeException>(action);
        }

        [Fact]
        public void TestGetPendingRelatedWhenNoPendingReturnsEmptyList()
        {
            int userId = CreateUser();

            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                db.ListaAmigos.RemoveRange(db.ListaAmigos);
                db.SaveChanges();
            }

            FriendsRepository repository = CreateRepository();

            IReadOnlyList<FriendLinkDto> result = repository.GetPendingRelated(userId);

            bool isOk = result != null && result.Count == 0;
            Assert.True(isOk);
        }

        [Fact]
        public void TestGetPendingRelatedReturnsLinksWhereUserIsAnySide()
        {
            int userId = CreateUser();
            int other1 = CreateUser();
            int other2 = CreateUser();

            InsertFriendLink(userId, other1, FRIEND_STATUS_PENDING);
            InsertFriendLink(other2, userId, FRIEND_STATUS_PENDING);

            FriendsRepository repository = CreateRepository();

            IReadOnlyList<FriendLinkDto> result = repository.GetPendingRelated(userId);

            bool isOk =
                result != null &&
                result.Count == 2 &&
                result.All(r => r.Status == FriendRequestStatus.Pending);
            Assert.True(isOk);
        }

        // GetAcceptedFriendsDetailed

        [Theory]
        [InlineData(INVALID_USER_ID_ZERO)]
        [InlineData(INVALID_USER_ID_NEGATIVE)]
        public void TestGetAcceptedFriendsDetailedWhenUserIdLessOrEqualZeroThrowsArgumentOutOfRange(int invalidUserId)
        {
            FriendsRepository repository = CreateRepository();

            Action action = () => repository.GetAcceptedFriendsDetailed(invalidUserId);

            Assert.Throws<ArgumentOutOfRangeException>(action);
        }

        [Fact]
        public void TestGetAcceptedFriendsDetailedWhenNoFriendsReturnsEmptyList()
        {
            int userId = CreateUser();

            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                db.ListaAmigos.RemoveRange(db.ListaAmigos);
                db.SaveChanges();
            }

            FriendsRepository repository = CreateRepository();

            IReadOnlyList<FriendListItemDto> result = repository.GetAcceptedFriendsDetailed(userId);

            bool isOk = result != null && result.Count == 0;
            Assert.True(isOk);
        }

        [Fact]
        public void TestGetAcceptedFriendsDetailedReturnsFriendWithUserNameFilled()
        {
            int userId = CreateUser("Owner");
            int friendId = CreateUser("FriendX");

            ListaAmigos link = InsertFriendLink(userId, friendId, FRIEND_STATUS_ACCEPTED);

            FriendsRepository repository = CreateRepository();

            IReadOnlyList<FriendListItemDto> result = repository.GetAcceptedFriendsDetailed(userId);

            bool isOk =
                result != null &&
                result.Count == 1 &&
                result[0].FriendLinkId == link.IdListaAmigos &&
                result[0].FriendUserId == friendId &&
                !string.IsNullOrWhiteSpace(result[0].FriendUserName);
            Assert.True(isOk);
        }

        // GetIncomingPendingDetailed

        [Theory]
        [InlineData(INVALID_USER_ID_ZERO)]
        [InlineData(INVALID_USER_ID_NEGATIVE)]
        public void TestGetIncomingPendingDetailedWhenUserIdLessOrEqualZeroThrowsArgumentOutOfRange(int invalidUserId)
        {
            FriendsRepository repository = CreateRepository();

            Action action = () => repository.GetIncomingPendingDetailed(invalidUserId);

            Assert.Throws<ArgumentOutOfRangeException>(action);
        }

        [Fact]
        public void TestGetIncomingPendingDetailedWhenNoIncomingReturnsEmptyList()
        {
            int userId = CreateUser();

            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                db.ListaAmigos.RemoveRange(db.ListaAmigos);
                db.SaveChanges();
            }

            FriendsRepository repository = CreateRepository();

            IReadOnlyList<FriendRequestItemDto> result = repository.GetIncomingPendingDetailed(userId);

            bool isOk = result != null && result.Count == 0;
            Assert.True(isOk);
        }

        [Fact]
        public void TestGetIncomingPendingDetailedReturnsRequestsWhereUserIsTarget()
        {
            int requesterId = CreateUser();
            int targetId = CreateUser();

            InsertFriendLink(requesterId, targetId, FRIEND_STATUS_PENDING);

            FriendsRepository repository = CreateRepository();

            IReadOnlyList<FriendRequestItemDto> result = repository.GetIncomingPendingDetailed(targetId);

            bool isOk =
                result != null &&
                result.Count == 1 &&
                result[0].RequesterUserId == requesterId &&
                result[0].TargetUserId == targetId &&
                result[0].Status == FriendRequestStatus.Pending;
            Assert.True(isOk);
        }

        // GetOutgoingPendingDetailed

        [Theory]
        [InlineData(INVALID_USER_ID_ZERO)]
        [InlineData(INVALID_USER_ID_NEGATIVE)]
        public void TestGetOutgoingPendingDetailedWhenUserIdLessOrEqualZeroThrowsArgumentOutOfRange(int invalidUserId)
        {
            FriendsRepository repository = CreateRepository();

            Action action = () => repository.GetOutgoingPendingDetailed(invalidUserId);

            Assert.Throws<ArgumentOutOfRangeException>(action);
        }

        [Fact]
        public void TestGetOutgoingPendingDetailedWhenNoOutgoingReturnsEmptyList()
        {
            int userId = CreateUser();

            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                db.ListaAmigos.RemoveRange(db.ListaAmigos);
                db.SaveChanges();
            }

            FriendsRepository repository = CreateRepository();

            IReadOnlyList<FriendRequestItemDto> result = repository.GetOutgoingPendingDetailed(userId);

            bool isOk = result != null && result.Count == 0;
            Assert.True(isOk);
        }

        [Fact]
        public void TestGetOutgoingPendingDetailedReturnsRequestsWhereUserIsRequester()
        {
            int requesterId = CreateUser();
            int targetId = CreateUser();

            InsertFriendLink(requesterId, targetId, FRIEND_STATUS_PENDING);

            FriendsRepository repository = CreateRepository();

            IReadOnlyList<FriendRequestItemDto> result = repository.GetOutgoingPendingDetailed(requesterId);

            bool isOk =
                result != null &&
                result.Count == 1 &&
                result[0].RequesterUserId == requesterId &&
                result[0].TargetUserId == targetId &&
                result[0].Status == FriendRequestStatus.Pending;
            Assert.True(isOk);
        }

        // SearchUsers

        [Theory]
        [InlineData(INVALID_USER_ID_ZERO)]
        [InlineData(INVALID_USER_ID_NEGATIVE)]
        public void TestSearchUsersWhenExcludeUserIdLessOrEqualZeroThrowsArgumentOutOfRange(int invalidExcludeId)
        {
            FriendsRepository repository = CreateRepository();

            Action action = () => repository.SearchUsers("x", 10, invalidExcludeId);

            Assert.Throws<ArgumentOutOfRangeException>(action);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void TestSearchUsersWhenQueryIsNullOrWhiteSpaceReturnsEmptyList(string invalidQuery)
        {
            int userId = CreateUser();

            FriendsRepository repository = CreateRepository();

            IReadOnlyList<UserBriefDto> result = repository.SearchUsers(invalidQuery, 10, userId);

            bool isOk = result != null && result.Count == 0;
            Assert.True(isOk);
        }

        [Fact]
        public void TestSearchUsersExcludesSelfAndExistingPendingOrAccepted()
        {
            int me = CreateUser("Owner");
            int friendAccepted = CreateUser("FriendAccepted");
            int friendPending = CreateUser("FriendPending");
            int candidate = CreateUser("FriendCandidate");

            InsertFriendLink(me, friendAccepted, FRIEND_STATUS_ACCEPTED);
            InsertFriendLink(me, friendPending, FRIEND_STATUS_PENDING);

            FriendsRepository repository = CreateRepository();

            IReadOnlyList<UserBriefDto> result = repository.SearchUsers("Friend", 10, me);

            bool isOk =
                result != null &&
                result.Count == 1 &&
                result[0].UserId == candidate;
            Assert.True(isOk);
        }

        [Fact]
        public void TestSearchUsersRespectsMaxResultsLimit()
        {
            int me = CreateUser("Owner");

            // Creamos muchos usuarios que matchean
            for (int i = 0; i < 30; i++)
            {
                CreateUser($"Many_{i}");
            }

            FriendsRepository repository = CreateRepository();

            int maxResults = 5;
            IReadOnlyList<UserBriefDto> result = repository.SearchUsers("FriendUser_Many", maxResults, me);

            bool isOk =
                result != null &&
                result.Count <= maxResults;
            Assert.True(isOk);
        }
    }
}
