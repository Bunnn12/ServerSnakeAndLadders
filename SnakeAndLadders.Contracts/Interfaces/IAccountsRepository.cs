namespace SnakeAndLadders.Contracts.Interfaces
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

        // Devuelve credenciales por usuario O correo
        (int userId, string passwordHash, string displayName)? GetAuthByIdentifier(string identifier);


    }
}
