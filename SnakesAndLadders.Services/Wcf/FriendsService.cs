using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using log4net;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Interfaces;
using SnakeAndLadders.Contracts.Services;

namespace SnakesAndLadders.Services.Wcf
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public sealed class FriendsService : IFriendsService
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(FriendsService));
        private readonly IFriendsAppService app;

        public FriendsService(IFriendsAppService app)
        {
            this.app = app ?? throw new ArgumentNullException(nameof(app));
        }

        public FriendLinkDto SendFriendRequest(string token, int targetUserId) => app.SendFriendRequest(token, targetUserId);
        public void AcceptFriendRequest(string token, int friendLinkId) => app.AcceptFriendRequest(token, friendLinkId);
        public void RejectFriendRequest(string token, int friendLinkId) => app.RejectFriendRequest(token, friendLinkId);
        public void CancelFriendRequest(string token, int friendLinkId) => app.CancelFriendRequest(token, friendLinkId);
        public void RemoveFriend(string token, int friendLinkId) => app.RemoveFriend(token, friendLinkId);
        public FriendLinkDto GetStatus(string token, int otherUserId) => app.GetStatus(token, otherUserId);

        public List<int> GetFriendsIds(string token) => new List<int>(app.GetFriendsIds(token));
        public List<FriendLinkDto> GetIncomingPending(string token) => new List<FriendLinkDto>(app.GetIncomingPending(token));
        public List<FriendLinkDto> GetOutgoingPending(string token) => new List<FriendLinkDto>(app.GetOutgoingPending(token));
    }
}
