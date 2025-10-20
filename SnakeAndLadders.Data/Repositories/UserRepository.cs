using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Interfaces;

using System;
using System.Data.Entity;
using System.Linq;

namespace SnakesAndLadders.Data.Repositories
{
    public sealed class UserRepository : IUserRepository
    {
        // Si tu clase de contexto NO se llama DataBaseEntities, cámbialo aquí y en ambos using.
        public AccountDto GetByUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("username es obligatorio.", nameof(username));

            using (var db = new SnakeAndLaddersDBEntities1()) 
            {
                var dto = db.Usuario
                    .AsNoTracking()
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

                return dto;
            }
        }

        public ProfilePhotoDto GetPhotoByUserId(int userId)
        {
            if (userId <= 0) throw new ArgumentOutOfRangeException(nameof(userId));

            using (var db = new SnakeAndLaddersDBEntities1())
            {
                var dto = db.Usuario
                    .AsNoTracking()
                    .Where(u => u.IdUsuario == userId)
                    .Select(u => new ProfilePhotoDto
                    {
                        UserId = u.IdUsuario,
                        Photo = u.FotoPerfil
                    })
                    .SingleOrDefault();

                return dto;
            }
        }
    }
}
