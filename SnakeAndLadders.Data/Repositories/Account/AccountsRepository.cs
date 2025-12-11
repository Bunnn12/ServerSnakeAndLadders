using log4net;
using ServerSnakesAndLadders.Common;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Interfaces;
using SnakesAndLadders.Data;
using SnakesAndLadders.Data.Interfaces;
using SnakesAndLadders.Data.Repositories.Account;
using SnakesAndLadders.Server.Helpers;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;

namespace ServerSnakesAndLadders
{
    public sealed class AccountsRepository : IAccountsRepository
    {
        private readonly IAccountIdentityRepository _accountIdentityRepository;
        private readonly IAccountRegistrationRepository _accountRegistrationRepository;
        private readonly IPasswordRepository _passwordRepository;
        private readonly IAccountEmailRepository _accountEmailRepository;

        public AccountsRepository(Func<SnakeAndLaddersDBEntities1> contextFactory = null)
        {
            Func<SnakeAndLaddersDBEntities1> factory =
                contextFactory ?? (() => new SnakeAndLaddersDBEntities1());

            _accountIdentityRepository = new AccountIdentityRepository(factory);
            _accountRegistrationRepository = new AccountRegistrationRepository(factory);
            _passwordRepository = new PasswordRepository(factory);
            _accountEmailRepository = new AccountEmailRepository(factory);
        }

        public bool IsEmailRegistered(string email)
        {
            return _accountIdentityRepository.IsEmailRegistered(email);
        }

        public bool IsUserNameTaken(string userName)
        {
            return _accountIdentityRepository.IsUserNameTaken(userName);
        }

        public OperationResult<int> CreateUserWithAccountAndPassword(CreateAccountRequestDto request)
        {
            return _accountRegistrationRepository.CreateUserWithAccountAndPassword(request);
        }

        public OperationResult<AuthCredentialsDto> GetAuthByIdentifier(string identifier)
        {
            return _accountIdentityRepository.GetAuthByIdentifier(identifier);
        }

        public OperationResult<IReadOnlyList<string>> GetLastPasswordHashes(int userId, int maxCount)
        {
            return _passwordRepository.GetLastPasswordHashes(userId, maxCount);
        }

        public OperationResult<bool> AddPasswordHash(int userId, string passwordHash)
        {
            return _passwordRepository.AddPasswordHash(userId, passwordHash);
        }

        public OperationResult<string> GetEmailByUserId(int userId)
        {
            return _accountEmailRepository.GetEmailByUserId(userId);
        }
    }
}
