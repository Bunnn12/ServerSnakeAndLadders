namespace SnakeAndLadders.Infrastructure.Repositories
{
    /// <summary>
    /// Repository for user registration and authentication data.
    /// </summary>
    public interface IAccountsRepository
    {
        bool EmailExists(string email);
        bool UserNameExists(string userName);

        // Creates Usuario + Cuenta + Contrasenia in a single transaction.
        int CreateUserWithAccountAndPassword(
            string userName,
            string firstName,
            string lastName,
            string email,
            string passwordHash
        );

        // Returns userId + hash + display name for login by email.
        (int userId, string passwordHash, string displayName)? GetAuthByEmail(string email);
    }
}
