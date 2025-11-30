using SnakeAndLadders.Contracts.Dtos;

namespace SnakeAndLadders.Contracts.Interfaces
{
    public interface IEmailSender
    {
        void SendVerificationCode(string email, string code);
        void SendGameInvitation(GameInvitationEmailDto request);
    }
}
