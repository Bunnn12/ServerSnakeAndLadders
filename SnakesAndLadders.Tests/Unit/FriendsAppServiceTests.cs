using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Enums;
using SnakeAndLadders.Contracts.Faults;
using SnakeAndLadders.Contracts.Interfaces;
using SnakesAndLadders.Services.Logic;
using Xunit;

namespace SnakesAndLadders.Tests.Unit
{
    public sealed class FriendsAppServiceTests
    {
        private readonly Mock<IFriendsRepository> _friendsRepositoryMock;
        private readonly Mock<Func<string, int>> _getUserIdFromTokenMock;
        private readonly FriendsAppService _service;

        public FriendsAppServiceTests()
        {
            _friendsRepositoryMock = new Mock<IFriendsRepository>(MockBehavior.Strict);
            _getUserIdFromTokenMock = new Mock<Func<string, int>>(MockBehavior.Strict);

            _service = new FriendsAppService(
                _friendsRepositoryMock.Object,
                _getUserIdFromTokenMock.Object);
        }

        #region Helpers

        private static bool HasFaultCode(Exception ex, string expectedCode)
        {
            if (ex == null)
            {
                return false;
            }

            var detailProp = ex.GetType().GetProperty("Detail");
            if (detailProp == null)
            {
                return false;
            }

            var detail = detailProp.GetValue(ex);
            if (detail == null)
            {
                return false;
            }

            var codeProp = detail.GetType().GetProperty("Code");
            if (codeProp == null)
            {
                return false;
            }

            var codeValue = codeProp.GetValue(detail) as string;
            return string.Equals(codeValue, expectedCode, StringComparison.Ordinal);
        }

        #endregion

        #region SendFriendRequest

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void TestSendFriendRequestThrowsInvalidSessionWhenUserIdNotPositive(int resolvedUserId)
        {
            _getUserIdFromTokenMock
                .Setup(func => func("TOKEN"))
                .Returns(resolvedUserId);

            Exception caught = null;

            try
            {
                _service.SendFriendRequest("TOKEN", 10);
            }
            catch (Exception ex)
            {
                caught = ex;
            }

            Assert.True(HasFaultCode(caught, "FRD_INVALID_SESSION"));
        }

        [Fact]
        public void TestSendFriendRequestThrowsSameUserWhenTargetEqualsCurrent()
        {
            _getUserIdFromTokenMock
                .Setup(func => func("TOKEN"))
                .Returns(5);

            Exception caught = null;

            try
            {
                _service.SendFriendRequest("TOKEN", 5);
            }
            catch (Exception ex)
            {
                caught = ex;
            }

            Assert.True(HasFaultCode(caught, "FRD_SAME_USER"));
        }

        [Fact]
        public void TestSendFriendRequestReturnsLinkFromRepository()
        {
            const int currentUserId = 1;
            const int targetUserId = 2;

            var link = new FriendLinkDto
            {
                FriendLinkId = 10,
                UserId1 = currentUserId,
                UserId2 = targetUserId,
                Status = FriendRequestStatus.Pending
            };

            _getUserIdFromTokenMock
                .Setup(func => func("TOKEN"))
                .Returns(currentUserId);

            _friendsRepositoryMock
                .Setup(repo => repo.CreatePending(currentUserId, targetUserId))
                .Returns(link);

            FriendLinkDto result = _service.SendFriendRequest("TOKEN", targetUserId);

            Assert.True(result != null && result.FriendLinkId == 10);
        }

        [Theory]
        [InlineData("Pending already exists")]
        [InlineData("Already friends")]
        public void TestSendFriendRequestThrowsLinkExistsForKnownInvalidOperationMessages(
            string repositoryMessage)
        {
            const int currentUserId = 3;
            const int targetUserId = 4;

            _getUserIdFromTokenMock
                .Setup(func => func("TOKEN"))
                .Returns(currentUserId);

            _friendsRepositoryMock
                .Setup(repo => repo.CreatePending(currentUserId, targetUserId))
                .Throws(new InvalidOperationException(repositoryMessage));

            Exception caught = null;

            try
            {
                _service.SendFriendRequest("TOKEN", targetUserId);
            }
            catch (Exception ex)
            {
                caught = ex;
            }

            Assert.True(HasFaultCode(caught, "FRD_LINK_EXISTS"));
        }

        [Fact]
        public void TestSendFriendRequestRethrowsInvalidOperationWhenMessageUnknown()
        {
            const int currentUserId = 3;
            const int targetUserId = 4;

            _getUserIdFromTokenMock
                .Setup(func => func("TOKEN"))
                .Returns(currentUserId);

            _friendsRepositoryMock
                .Setup(repo => repo.CreatePending(currentUserId, targetUserId))
                .Throws(new InvalidOperationException("Some other error"));

            InvalidOperationException caught = null;

            try
            {
                _service.SendFriendRequest("TOKEN", targetUserId);
            }
            catch (InvalidOperationException ex)
            {
                caught = ex;
            }

            Assert.True(caught != null);
        }

        #endregion

        #region AcceptFriendRequest

        [Fact]
        public void TestAcceptFriendRequestThrowsLinkNotFoundWhenLinkMissing()
        {
            _getUserIdFromTokenMock
                .Setup(func => func("TOKEN"))
                .Returns(10);

            _friendsRepositoryMock
                .Setup(repo => repo.GetById(100))
                .Returns((FriendLinkDto)null);

            Exception caught = null;

            try
            {
                _service.AcceptFriendRequest("TOKEN", 100);
            }
            catch (Exception ex)
            {
                caught = ex;
            }

            Assert.True(HasFaultCode(caught, "FRD_LINK_NOT_FOUND"));
        }

        [Fact]
        public void TestAcceptFriendRequestThrowsNotInLinkWhenUserNotParticipant()
        {
            var link = new FriendLinkDto
            {
                FriendLinkId = 100,
                UserId1 = 1,
                UserId2 = 2,
                Status = FriendRequestStatus.Pending
            };

            _getUserIdFromTokenMock
                .Setup(func => func("TOKEN"))
                .Returns(99);

            _friendsRepositoryMock
                .Setup(repo => repo.GetById(100))
                .Returns(link);

            Exception caught = null;

            try
            {
                _service.AcceptFriendRequest("TOKEN", 100);
            }
            catch (Exception ex)
            {
                caught = ex;
            }

            Assert.True(HasFaultCode(caught, "FRD_NOT_IN_LINK"));
        }

        [Fact]
        public void TestAcceptFriendRequestUpdatesStatusToAccepted()
        {
            var link = new FriendLinkDto
            {
                FriendLinkId = 100,
                UserId1 = 5,
                UserId2 = 7,
                Status = FriendRequestStatus.Pending
            };

            _getUserIdFromTokenMock
                .Setup(func => func("TOKEN"))
                .Returns(5);

            _friendsRepositoryMock
                .Setup(repo => repo.GetById(100))
                .Returns(link);

            _friendsRepositoryMock
                .Setup(repo => repo.UpdateStatus(100, 0x02));

            _service.AcceptFriendRequest("TOKEN", 100);

            _friendsRepositoryMock.Verify(
                repo => repo.UpdateStatus(100, 0x02),
                Times.Once);

            Assert.True(true);
        }

        #endregion

        #region RejectFriendRequest

        [Fact]
        public void TestRejectFriendRequestUpdatesStatusToRejected()
        {
            var link = new FriendLinkDto
            {
                FriendLinkId = 200,
                UserId1 = 5,
                UserId2 = 7,
                Status = FriendRequestStatus.Pending
            };

            _getUserIdFromTokenMock
                .Setup(func => func("TOKEN"))
                .Returns(7);

            _friendsRepositoryMock
                .Setup(repo => repo.GetById(200))
                .Returns(link);

            _friendsRepositoryMock
                .Setup(repo => repo.UpdateStatus(200, 0x03));

            _service.RejectFriendRequest("TOKEN", 200);

            _friendsRepositoryMock.Verify(
                repo => repo.UpdateStatus(200, 0x03),
                Times.Once);

            Assert.True(true);
        }

        #endregion

        #region CancelFriendRequest

        [Fact]
        public void TestCancelFriendRequestDeletesLinkWhenUserInLink()
        {
            var link = new FriendLinkDto
            {
                FriendLinkId = 300,
                UserId1 = 10,
                UserId2 = 20,
                Status = FriendRequestStatus.Pending
            };

            _getUserIdFromTokenMock
                .Setup(func => func("TOKEN"))
                .Returns(10);

            _friendsRepositoryMock
                .Setup(repo => repo.GetById(300))
                .Returns(link);

            _friendsRepositoryMock
                .Setup(repo => repo.DeleteLink(300));

            _service.CancelFriendRequest("TOKEN", 300);

            _friendsRepositoryMock.Verify(
                repo => repo.DeleteLink(300),
                Times.Once);

            Assert.True(true);
        }

        #endregion

        #region RemoveFriend

        [Fact]
        public void TestRemoveFriendDeletesLinkWhenUserInLink()
        {
            var link = new FriendLinkDto
            {
                FriendLinkId = 400,
                UserId1 = 10,
                UserId2 = 20,
                Status = FriendRequestStatus.Accepted
            };

            _getUserIdFromTokenMock
                .Setup(func => func("TOKEN"))
                .Returns(20);

            _friendsRepositoryMock
                .Setup(repo => repo.GetById(400))
                .Returns(link);

            _friendsRepositoryMock
                .Setup(repo => repo.DeleteLink(400));

            _service.RemoveFriend("TOKEN", 400);

            _friendsRepositoryMock.Verify(
                repo => repo.DeleteLink(400),
                Times.Once);

            Assert.True(true);
        }

        #endregion

        #region Simple Forwarders

        [Fact]
        public void TestGetStatusReturnsRepositoryResult()
        {
            const int currentUserId = 5;
            const int otherUserId = 7;

            var link = new FriendLinkDto
            {
                FriendLinkId = 10,
                UserId1 = currentUserId,
                UserId2 = otherUserId
            };

            _getUserIdFromTokenMock
                .Setup(func => func("TOKEN"))
                .Returns(currentUserId);

            _friendsRepositoryMock
                .Setup(repo => repo.GetNormalized(currentUserId, otherUserId))
                .Returns(link);

            FriendLinkDto result = _service.GetStatus("TOKEN", otherUserId);

            Assert.True(result != null && result.FriendLinkId == 10);
        }

        [Fact]
        public void TestGetFriendsIdsReturnsRepositoryResult()
        {
            const int currentUserId = 9;

            var ids = new List<int> { 1, 2, 3 }.AsReadOnly();

            _getUserIdFromTokenMock
                .Setup(func => func("TOKEN"))
                .Returns(currentUserId);

            _friendsRepositoryMock
                .Setup(repo => repo.GetAcceptedFriendsIds(currentUserId))
                .Returns(ids);

            IReadOnlyList<int> result = _service.GetFriendsIds("TOKEN");

            Assert.True(result != null && result.Count == 3);
        }

        [Fact]
        public void TestGetFriendsReturnsRepositoryResult()
        {
            const int currentUserId = 9;

            var list = new List<FriendListItemDto>
            {
                new FriendListItemDto { FriendUserId = 1 },
                new FriendListItemDto { FriendUserId = 2 }
            }.AsReadOnly();

            _getUserIdFromTokenMock
                .Setup(func => func("TOKEN"))
                .Returns(currentUserId);

            _friendsRepositoryMock
                .Setup(repo => repo.GetAcceptedFriendsDetailed(currentUserId))
                .Returns(list);

            IReadOnlyList<FriendListItemDto> result = _service.GetFriends("TOKEN");

            Assert.True(result != null && result.Count == 2);
        }

        [Fact]
        public void TestGetIncomingRequestsReturnsRepositoryResult()
        {
            const int currentUserId = 9;

            var list = new List<FriendRequestItemDto>
            {
                new FriendRequestItemDto { FriendLinkId = 1 },
                new FriendRequestItemDto { FriendLinkId = 2 }
            }.AsReadOnly();

            _getUserIdFromTokenMock
                .Setup(func => func("TOKEN"))
                .Returns(currentUserId);

            _friendsRepositoryMock
                .Setup(repo => repo.GetIncomingPendingDetailed(currentUserId))
                .Returns(list);

            IReadOnlyList<FriendRequestItemDto> result =
                _service.GetIncomingRequests("TOKEN");

            Assert.True(result != null && result.Count == 2);
        }

        [Fact]
        public void TestGetOutgoingRequestsReturnsRepositoryResult()
        {
            const int currentUserId = 9;

            var list = new List<FriendRequestItemDto>
            {
                new FriendRequestItemDto { FriendLinkId = 1 },
                new FriendRequestItemDto { FriendLinkId = 2 }
            }.AsReadOnly();

            _getUserIdFromTokenMock
                .Setup(func => func("TOKEN"))
                .Returns(currentUserId);

            _friendsRepositoryMock
                .Setup(repo => repo.GetOutgoingPendingDetailed(currentUserId))
                .Returns(list);

            IReadOnlyList<FriendRequestItemDto> result =
                _service.GetOutgoingRequests("TOKEN");

            Assert.True(result != null && result.Count == 2);
        }

        [Fact]
        public void TestSearchUsersReturnsRepositoryResult()
        {
            const int currentUserId = 9;

            var list = new List<UserBriefDto>
            {
                new UserBriefDto { UserId = 1 },
                new UserBriefDto { UserId = 2 }
            }.AsReadOnly();

            _getUserIdFromTokenMock
                .Setup(func => func("TOKEN"))
                .Returns(currentUserId);

            _friendsRepositoryMock
                .Setup(repo => repo.SearchUsers("abc", 10, currentUserId))
                .Returns(list);

            IReadOnlyList<UserBriefDto> result =
                _service.SearchUsers("TOKEN", "abc", 10);

            Assert.True(result != null && result.Count == 2);
        }

        #endregion

        #region GetIncomingPending / GetOutgoingPending

        [Fact]
        public void TestGetIncomingPendingFiltersByUserId2()
        {
            const int currentUserId = 5;

            var pending = new List<FriendLinkDto>
            {
                new FriendLinkDto
                {
                    FriendLinkId = 1,
                    UserId1 = 5,
                    UserId2 = 7
                },
                new FriendLinkDto
                {
                    FriendLinkId = 2,
                    UserId1 = 8,
                    UserId2 = 5
                },
                new FriendLinkDto
                {
                    FriendLinkId = 3,
                    UserId1 = 9,
                    UserId2 = 10
                }
            }.AsReadOnly();

            _getUserIdFromTokenMock
                .Setup(func => func("TOKEN"))
                .Returns(currentUserId);

            _friendsRepositoryMock
                .Setup(repo => repo.GetPendingRelated(currentUserId))
                .Returns(pending);

            IReadOnlyList<FriendLinkDto> result =
                _service.GetIncomingPending("TOKEN");

            bool allMatch =
                result.Count == 1
                && result[0].FriendLinkId == 2
                && result[0].UserId2 == currentUserId;

            Assert.True(allMatch);
        }

        [Fact]
        public void TestGetOutgoingPendingFiltersByUserId1()
        {
            const int currentUserId = 5;

            var pending = new List<FriendLinkDto>
            {
                new FriendLinkDto
                {
                    FriendLinkId = 1,
                    UserId1 = 5,
                    UserId2 = 7
                },
                new FriendLinkDto
                {
                    FriendLinkId = 2,
                    UserId1 = 8,
                    UserId2 = 5
                },
                new FriendLinkDto
                {
                    FriendLinkId = 3,
                    UserId1 = 5,
                    UserId2 = 10
                }
            }.AsReadOnly();

            _getUserIdFromTokenMock
                .Setup(func => func("TOKEN"))
                .Returns(currentUserId);

            _friendsRepositoryMock
                .Setup(repo => repo.GetPendingRelated(currentUserId))
                .Returns(pending);

            IReadOnlyList<FriendLinkDto> result =
                _service.GetOutgoingPending("TOKEN");

            bool allMatch =
                result.Count == 2
                && result.All(l => l.UserId1 == currentUserId);

            Assert.True(allMatch);
        }

        #endregion
    }
}
