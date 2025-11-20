using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Interfaces;
using System;
using System.Data.Entity;
using System.Linq;

namespace SnakesAndLadders.Data.Repositories
{
    public sealed class UserRepository : IUserRepository
    {
        private const int MAX_USERNAME = 50;
        private const int MAX_FIRST_NAME = 100;
        private const int MAX_LAST_NAME = 255;
        private const int MAX_DESCRIPTION = 500;

        public AccountDto GetByUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException("username es obligatorio.", nameof(username));
            }

            using (var db = new SnakeAndLaddersDBEntities1())
            {
                var dto =
                    (from u in db.Usuario.AsNoTracking()
                     join a in db.AvatarDesbloqueado.AsNoTracking()
                         on u.IdAvatarDesbloqueadoActual equals a.IdAvatarDesbloqueado
                         into avatarGroup
                     from a in avatarGroup.DefaultIfEmpty() // LEFT JOIN
                     where u.NombreUsuario == username
                     select new AccountDto
                     {
                         UserId = u.IdUsuario,
                         Username = u.NombreUsuario,
                         FirstName = u.Nombre,
                         LastName = u.Apellidos,
                         ProfileDescription = u.DescripcionPerfil,
                         Coins = u.Monedas,
                         HasProfilePhoto = u.FotoPerfil != null && u.FotoPerfil.Length > 0,
                         ProfilePhotoId = (u.FotoPerfil ?? "DEF").Trim().ToUpper(),
                         CurrentSkinUnlockedId = u.IdAvatarDesbloqueadoActual,
                         CurrentSkinId = a.AvatarIdAvatar.ToString()
                     })
                    .SingleOrDefault();

                return dto;
            }
        }

        public ProfilePhotoDto GetPhotoByUserId(int userId)
        {
            if (userId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(userId));
            }

            using (var db = new SnakeAndLaddersDBEntities1())
            {
                var dto = db.Usuario
                    .AsNoTracking()
                    .Where(u => u.IdUsuario == userId)
                    .Select(u => new ProfilePhotoDto
                    {
                        UserId = u.IdUsuario,
                        ProfilePhotoId = (u.FotoPerfil ?? "DEF").Trim().ToUpper()
                    })
                    .SingleOrDefault();

                return dto;
            }
        }

        public AccountDto UpdateProfile(UpdateProfileRequestDto request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (request.UserId <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(request),
                    "UserId must be positive.");
            }

            if (!string.IsNullOrWhiteSpace(request.FirstName) && request.FirstName.Length > MAX_FIRST_NAME)
            {
                throw new ArgumentException(
                    $"FirstName exceeds {MAX_FIRST_NAME} characters.",
                    nameof(request));
            }

            if (!string.IsNullOrWhiteSpace(request.LastName) && request.LastName.Length > MAX_LAST_NAME)
            {
                throw new ArgumentException(
                    $"LastName exceeds {MAX_LAST_NAME} characters.",
                    nameof(request));
            }

            if (request.ProfileDescription != null && request.ProfileDescription.Length > MAX_DESCRIPTION)
            {
                throw new ArgumentException(
                    $"ProfileDescription exceeds {MAX_DESCRIPTION} characters.",
                    nameof(request));
            }

            using (var db = new SnakeAndLaddersDBEntities1())
            {
                var entity = db.Usuario.SingleOrDefault(u => u.IdUsuario == request.UserId);
                if (entity == null)
                {
                    throw new InvalidOperationException("User not found.");
                }

                if (!string.IsNullOrWhiteSpace(request.FirstName))
                {
                    entity.Nombre = request.FirstName;
                }

                if (!string.IsNullOrWhiteSpace(request.LastName))
                {
                    entity.Apellidos = request.LastName;
                }

                if (request.ProfileDescription != null)
                {
                    entity.DescripcionPerfil = request.ProfileDescription;
                }

                if (request.ProfilePhotoId != null)
                {
                    entity.FotoPerfil = request.ProfilePhotoId.Length == 0
                        ? null
                        : request.ProfilePhotoId;
                }

                db.SaveChanges();

                var dto =
                    (from u in db.Usuario.AsNoTracking()
                     join a in db.AvatarDesbloqueado.AsNoTracking()
                         on u.IdAvatarDesbloqueadoActual equals a.IdAvatarDesbloqueado
                         into avatarGroup
                     from a in avatarGroup.DefaultIfEmpty()
                     where u.IdUsuario == request.UserId
                     select new AccountDto
                     {
                         UserId = u.IdUsuario,
                         Username = u.NombreUsuario,
                         FirstName = u.Nombre,
                         LastName = u.Apellidos,
                         ProfileDescription = u.DescripcionPerfil,
                         Coins = u.Monedas,
                         HasProfilePhoto = u.FotoPerfil != null && u.FotoPerfil.Length > 0,
                         ProfilePhotoId = (u.FotoPerfil ?? "DEF").Trim().ToUpper(),
                         CurrentSkinUnlockedId = u.IdAvatarDesbloqueadoActual,
                         CurrentSkinId = a.AvatarIdAvatar.ToString()
                     })
                    .Single();

                return dto;
            }
        }

        public static string GetAvatarIdByUserId(int userId) 
        {
            if (userId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(userId));
            }

            using (var db = new SnakeAndLaddersDBEntities1())
            {
                var result =
                    (from u in db.Usuario.AsNoTracking()
                     join a in db.AvatarDesbloqueado.AsNoTracking()
                         on u.IdAvatarDesbloqueadoActual equals a.IdAvatarDesbloqueado
                         into avatarGroup
                     from a in avatarGroup.DefaultIfEmpty()
                     where u.IdUsuario == userId
                     select a.AvatarIdAvatar.ToString() ?? u.FotoPerfil ?? "DEF")
                    .SingleOrDefault();

                return result?.Trim().ToUpper();
            }
        }
    }
}
