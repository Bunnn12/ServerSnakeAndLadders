using SnakeAndLadders.Contracts.Services;
using SnakeAndLadders.Infrastructure.Repositories;

namespace SnakeAndLadders.Host.Services
{
    public class UserService : IUserService
    {
        private readonly UserRepository repo = new UserRepository();

        public int AddUser(string username, string nombre, string apellidos)
        {
            return repo.AddUser(username, nombre, apellidos);
        }
    }
}

