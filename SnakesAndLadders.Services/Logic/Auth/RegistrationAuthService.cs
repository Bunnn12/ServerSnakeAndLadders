using log4net;
using ServerSnakesAndLadders.Common;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Interfaces;
using SnakesAndLadders.Services.Constants;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakesAndLadders.Services.Logic.Auth
{
    public sealed class RegistrationAuthService : IRegistrationAuthService
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(RegistrationAuthService));

        private readonly IAccountsRepository _accountsRepository;
        private readonly IPasswordHasher _passwordHasher;

        public RegistrationAuthService(
            IAccountsRepository accountsRepository,
            IPasswordHasher passwordHasher)
        {
            _accountsRepository = accountsRepository ?? throw new ArgumentNullException(nameof(accountsRepository));
            _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        }

        public AuthResult RegisterUser(RegistrationDto registration)
        {
            AuthResult validationError = ValidateRegistrationRequest(registration);
            if (validationError != null)
            {
                return validationError;
            }

            OperationResult<int> createResult = CreateUserAccount(registration);

            if (!createResult.IsSuccess)
            {
                return AuthResultFactory.FailWithErrorType(
                    AuthConstants.AuthCodeServerError,
                    AuthConstants.ErrorTypeSql);
            }

            int newUserId = createResult.Data;
            return AuthResultFactory.OkWithUser(newUserId, registration.UserName);
        }

        private AuthResult ValidateRegistrationRequest(RegistrationDto registration)
        {
            if (registration == null
                || string.IsNullOrWhiteSpace(registration.Email)
                || string.IsNullOrWhiteSpace(registration.Password)
                || string.IsNullOrWhiteSpace(registration.UserName))
            {
                return AuthResultFactory.Fail(AuthConstants.AuthCodeInvalidRequest);
            }

            try
            {
                if (_accountsRepository.IsEmailRegistered(registration.Email))
                {
                    return AuthResultFactory.Fail(AuthConstants.AuthCodeEmailAlreadyExists);
                }

                if (_accountsRepository.IsUserNameTaken(registration.UserName))
                {
                    return AuthResultFactory.Fail(
                        AuthConstants.AuthCodeEmailAlreadyExists.Replace("Email", "UserName"));
                }
            }
            catch (SqlException ex)
            {
                Logger.Error("SQL error while validating registration request.", ex);
                return AuthResultFactory.FailWithErrorType(
                    AuthConstants.AuthCodeServerError,
                    AuthConstants.ErrorTypeSql);
            }
            catch (ConfigurationErrorsException ex)
            {
                Logger.Error("Configuration error while validating registration request.", ex);
                return AuthResultFactory.FailWithErrorType(
                    AuthConstants.AuthCodeServerError,
                    AuthConstants.ErrorTypeConfig);
            }
            catch (Exception ex)
            {
                Logger.Error("Unexpected error while validating registration request.", ex);
                return AuthResultFactory.FailWithErrorType(
                    AuthConstants.AuthCodeServerError,
                    AuthConstants.ErrorTypeUnexpected);
            }

            return null;
        }

        private OperationResult<int> CreateUserAccount(RegistrationDto registration)
        {
            string passwordHash = _passwordHasher.Hash(registration.Password);

            var requestDto = new CreateAccountRequestDto
            {
                Username = registration.UserName,
                FirstName = registration.FirstName,
                LastName = registration.LastName,
                Email = registration.Email,
                PasswordHash = passwordHash
            };

            try
            {
                return _accountsRepository.CreateUserWithAccountAndPassword(requestDto);
            }
            catch (SqlException ex)
            {
                Logger.Error("SQL error while creating user account.", ex);
                return OperationResult<int>.Failure("SQL error while creating user account.");
            }
            catch (ConfigurationErrorsException ex)
            {
                Logger.Error("Configuration error while creating user account.", ex);
                return OperationResult<int>.Failure("Configuration error while creating user account.");
            }
            catch (Exception ex)
            {
                Logger.Error("Unexpected error while creating user account.", ex);
                return OperationResult<int>.Failure("Unexpected error while creating user account.");
            }
        }
    }
}
