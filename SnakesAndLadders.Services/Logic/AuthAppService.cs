using log4net;
using ServerSnakesAndLadders.Common;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Interfaces;
using SnakesAndLadders.Services.Constants;
using SnakesAndLadders.Services.Logic.Auth;
using System;
using System.Configuration;
using System.Security.Cryptography;

namespace SnakesAndLadders.Services.Logic
{
    public sealed class AuthAppService : IAuthAppService
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(AuthAppService));

        private readonly IRegistrationAuthService _registrationService;
        private readonly ILoginAuthService _loginService;
        private readonly IVerificationAuthService _verificationService;
        private readonly IPasswordChangeAuthService _passwordChangeService;
        private readonly ITokenService _tokenService;

        public AuthAppService(
            IRegistrationAuthService registrationService,
            ILoginAuthService loginService,
            IVerificationAuthService verificationService,
            IPasswordChangeAuthService passwordChangeService,
            ITokenService tokenService)
        {
            _registrationService = registrationService ?? throw new ArgumentNullException(nameof(registrationService));
            _loginService = loginService ?? throw new ArgumentNullException(nameof(loginService));
            _verificationService = verificationService ?? throw new ArgumentNullException(nameof(verificationService));
            _passwordChangeService = passwordChangeService ?? throw new ArgumentNullException(nameof(passwordChangeService));
            _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        }

        public AuthResult RegisterUser(RegistrationDto registration)
        {
            return _registrationService.RegisterUser(registration);
        }

        public AuthResult Login(LoginDto request)
        {
            return _loginService.Login(request);
        }

        public AuthResult Logout(LogoutRequestDto request)
        {
            if (request == null)
            {
                return AuthResultFactory.Fail(AuthConstants.AuthCodeInvalidRequest);
            }

            string normalizedToken = (request.Token ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(normalizedToken))
            {
                return AuthResultFactory.Fail(AuthConstants.AuthCodeInvalidRequest);
            }

            int userId;

            try
            {
                userId = _tokenService.GetUserIdFromToken(normalizedToken);
            }
            catch (ConfigurationErrorsException ex)
            {
                _logger.Error("Configuration error while resolving user id from token in logout.", ex);
                return AuthResultFactory.FailWithErrorType(
                    AuthConstants.AuthCodeServerError,
                    AuthConstants.ErrorTypeConfig);
            }
            catch (CryptographicException ex)
            {
                _logger.Error("Cryptographic error while resolving user id from token in logout.", ex);
                return AuthResultFactory.FailWithErrorType(
                    AuthConstants.AuthCodeServerError,
                    AuthConstants.ErrorTypeCrypto);
            }
            catch (Exception ex)
            {
                _logger.Error("Unexpected error while resolving user id from token in logout.", ex);
                return AuthResultFactory.FailWithErrorType(
                    AuthConstants.AuthCodeServerError,
                    AuthConstants.ErrorTypeUnexpected);
            }

            if (userId < AuthConstants.MinValidUserId)
            {
                return AuthResultFactory.Fail(AuthConstants.AuthCodeInvalidRequest);
            }

            InMemorySessionManager.Logout(userId, normalizedToken);

            _logger.InfoFormat(
                "Logout completed for user {0}.",
                userId);

            return AuthResultFactory.OkWithCustomCode(AuthConstants.AuthCodeOk, userId);
        }

        public AuthResult RequestEmailVerification(string email)
        {
            return _verificationService.RequestEmailVerification(email);
        }

        public AuthResult ConfirmEmailVerification(string email, string code)
        {
            return _verificationService.ConfirmEmailVerification(email, code);
        }

        public AuthResult RequestPasswordChangeCode(string email)
        {
            return _verificationService.RequestPasswordChangeCode(email);
        }

        public AuthResult ChangePassword(ChangePasswordRequestDto request)
        {
            return _passwordChangeService.ChangePassword(request);
        }

        public int GetUserIdFromToken(string token)
        {
            return _tokenService.GetUserIdFromToken(token);
        }
    }
}
