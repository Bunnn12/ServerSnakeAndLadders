using ServerSnakesAndLadders;
using SnakeAndLadders.Contracts.Dtos;
using System.Linq;

namespace SnakeAndLadders.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly SnakeAndLaddersDBEntities1 _db;

        public UserRepository(SnakeAndLaddersDBEntities1 db)
        {
            _db = db;
        }

        public AccountDto GetByUsername(string username)
        {
            return _db.Usuario
                .Where(u => u.NombreUsuario == username)
                .Select(u => new AccountDto
                {
                    UserId = u.IdUsuario,
                    Username = u.NombreUsuario,
                    FirstName = u.Nombre,
                    LastName = u.Apellidos,
                    ProfileDescription = u.DescripcionPerfil,
                    Coins = u.Monedas,
                    HasProfilePhoto = u.FotoPerfil != null && u.FotoPerfil.Length > 0
                })
                .SingleOrDefault();
        }

        public ProfilePhotoDto GetPhotoByUserId(int userId)
        {
            return _db.Usuario
                .Where(u => u.IdUsuario == userId)
                .Select(u => new ProfilePhotoDto
                {
                    UserId = u.IdUsuario,
                    Photo = u.FotoPerfil
                })
                .SingleOrDefault();
        }
    }
}
