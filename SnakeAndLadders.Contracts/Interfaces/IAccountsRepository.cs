namespace SnakeAndLadders.Contracts.Interfaces
{
    public interface IAccountsRepository
    {
        bool EmailExists(string email);
        bool UserNameExists(string userName);
        int CreateUserWithAccountAndPassword(
            string userName,
            string firstName,
            string lastName,
            string email,
            string passwordHash
        );
        (int userId, string passwordHash, string displayName)? GetAuthByIdentifier(string identifier);


    }
}
