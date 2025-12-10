using System.ServiceModel;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Interfaces;
using SnakeAndLadders.Contracts.Services;

namespace SnakesAndLadders.Services.Wcf
{
    [ServiceBehavior(
        InstanceContextMode = InstanceContextMode.Single,
        IncludeExceptionDetailInFaults = false)]
    public sealed class AuthService : IAuthService
    {
        private readonly IAuthAppService _authAppService;

        public AuthService(IAuthAppService authAppService)
        {
            _authAppService = authAppService ?? throw new System.ArgumentNullException(nameof(authAppService));
        }

        public AuthResult Register(RegistrationDto registration)
        {
            return _authAppService.RegisterUser(registration);
        }

        public AuthResult Login(LoginDto request)
        {
            return _authAppService.Login(request);
        }

        public AuthResult Logout(LogoutRequestDto request)
        {
            return _authAppService.Logout(request);
        }
        public AuthResult RequestEmailVerification(string email)
        {
            return _authAppService.RequestEmailVerification(email);
        }

        public AuthResult ConfirmEmailVerification(string email, string code)
        {
            return _authAppService.ConfirmEmailVerification(email, code);
        }

        public AuthResult RequestPasswordChangeCode(string email)
        {
            return _authAppService.RequestPasswordChangeCode(email);
        }

        public AuthResult ChangePassword(ChangePasswordRequestDto request)
        {
            return _authAppService.ChangePassword(request);
        }
        public AuthResult Logout(LogoutRequestDto request)
        {
            return _authAppService.Logout(request);
        }
    }
}
