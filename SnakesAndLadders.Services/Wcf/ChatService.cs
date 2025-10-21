using System.Collections.Generic;
using System.ServiceModel;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Services;
using SnakeAndLadders.Services.Logic;

namespace SnakesAndLadders.Services.Wcf
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public sealed class ChatService : IChatService
    {
        private readonly ChatAppService app;

        public ChatService(ChatAppService app)
        {
            this.app = app;
        }

        public SendMessageResponse SendMessage(SendMessageRequest request)
        {
            app.Send(request.Message);
            return new SendMessageResponse { Ok = true };
        }

        public IList<ChatMessageDto> GetRecent(int take)
        {
            return app.GetRecent(take);
        }
    }
}
