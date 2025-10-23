using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Services;
using SnakesAndLadders.Services.Logic;
using System.ServiceModel;

namespace SnakesAndLadders.Services.Wcf
{
    [ServiceBehavior(
        InstanceContextMode = InstanceContextMode.Single,
        IncludeExceptionDetailInFaults = false
    )]
    public sealed class AuthService : IAuthService
    {
        private readonly IAuthAppService _app;
        public AuthService(IAuthAppService app) { _app = app; }

        public AuthResult Register(RegistrationDto r) => _app.Register(r);
        public AuthResult Login(LoginDto r) => _app.Login(r);
        public AuthResult RequestEmailVerification(string email) => _app.RequestEmailVerification(email);
        public AuthResult ConfirmEmailVerification(string email, string code) => _app.ConfirmEmailVerification(email, code);
    }
}
