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

        private const string DEFAULT_PROFILE_PHOTO_ID = "DEF";
        private const byte STATUS_ACTIVE = 0x01;
        private const byte STATUS_INACTIVE = 0x00;

        public AccountDto GetByUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException("username es obligatorio.", nameof(username));
            }

            if (username.Length > MAX_USERNAME)
            {
                throw new ArgumentException(
                    $"username excede la longitud máxima permitida ({MAX_USERNAME}).",
                    nameof(username));
            }

            using (var db = new SnakeAndLaddersDBEntities1())
            {
                var row =
                    (from u in db.Usuario.AsNoTracking()
                     join a in db.AvatarDesbloqueado.AsNoTracking()
                         on u.IdAvatarDesbloqueadoActual equals a.IdAvatarDesbloqueado
                         into avatarGroup
                     from a in avatarGroup.DefaultIfEmpty() // LEFT JOIN
                     where u.NombreUsuario == username
                     select new
                     {
                         Usuario = u,
                         AvatarDesbloqueado = a
                     })
                    .SingleOrDefault();

                if (row == null)
                {
                    return null;
                }

                return MapToAccountDto(row.Usuario, row.AvatarDesbloqueado);
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
                var entity = db.Usuario
                    .AsNoTracking()
                    .SingleOrDefault(u => u.IdUsuario == userId);

                if (entity == null)
                {
                    return null;
                }

                string normalizedPhotoId = NormalizePhotoId(entity.FotoPerfil);

                return new ProfilePhotoDto
                {
                    UserId = entity.IdUsuario,
                    ProfilePhotoId = normalizedPhotoId
                };
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

            if (!string.IsNullOrWhiteSpace(request.FirstName) &&
                request.FirstName.Length > MAX_FIRST_NAME)
            {
                throw new ArgumentException(
                    $"FirstName exceeds {MAX_FIRST_NAME} characters.",
                    nameof(request));
            }

            if (!string.IsNullOrWhiteSpace(request.LastName) &&
                request.LastName.Length > MAX_LAST_NAME)
            {
                throw new ArgumentException(
                    $"LastName exceeds {MAX_LAST_NAME} characters.",
                    nameof(request));
            }

            if (request.ProfileDescription != null &&
                request.ProfileDescription.Length > MAX_DESCRIPTION)
            {
                throw new ArgumentException(
                    $"ProfileDescription exceeds {MAX_DESCRIPTION} characters.",
                    nameof(request));
            }

            using (var db = new SnakeAndLaddersDBEntities1())
            {
                var entity = db.Usuario
                    .SingleOrDefault(u => u.IdUsuario == request.UserId);

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

                var row =
                    (from u in db.Usuario.AsNoTracking()
                     join a in db.AvatarDesbloqueado.AsNoTracking()
                         on u.IdAvatarDesbloqueadoActual equals a.IdAvatarDesbloqueado
                         into avatarGroup
                     from a in avatarGroup.DefaultIfEmpty()
                     where u.IdUsuario == request.UserId
                     select new
                     {
                         Usuario = u,
                         AvatarDesbloqueado = a
                     })
                    .Single();

                return MapToAccountDto(row.Usuario, row.AvatarDesbloqueado);
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
                var row =
                    (from u in db.Usuario.AsNoTracking()
                     join a in db.AvatarDesbloqueado.AsNoTracking()
                         on u.IdAvatarDesbloqueadoActual equals a.IdAvatarDesbloqueado
                         into avatarGroup
                     from a in avatarGroup.DefaultIfEmpty()
                     where u.IdUsuario == userId
                     select new
                     {
                         Usuario = u,
                         AvatarDesbloqueado = a
                     })
                    .SingleOrDefault();

                if (row == null)
                {
                    return DEFAULT_PROFILE_PHOTO_ID;
                }

                if (row.AvatarDesbloqueado != null)
                {
                    // Usamos el ID del avatar desbloqueado
                    return row.AvatarDesbloqueado.AvatarIdAvatar
                        .ToString()
                        .Trim()
                        .ToUpperInvariant();
                }

                // Fallback: FotoPerfil como código
                return NormalizePhotoId(row.Usuario.FotoPerfil);
            }
        }

        public void DeactivateUser(int userId)
        {
            if (userId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(userId));
            }

            using (var db = new SnakeAndLaddersDBEntities1())
            {
                var entity = db.Usuario.SingleOrDefault(u => u.IdUsuario == userId);

                if (entity == null)
                {
                    throw new InvalidOperationException("User not found.");
                }

                // BINARY(1) -> normalmente se mapea como byte[]
                entity.Estado = new[] { STATUS_INACTIVE };

                db.SaveChanges();
            }
        }

        // ===== Helpers privados =====

        private static AccountDto MapToAccountDto(Usuario usuario, AvatarDesbloqueado avatar)
        {
            if (usuario == null)
            {
                return null;
            }

            string normalizedPhotoId = NormalizePhotoId(usuario.FotoPerfil);

            return new AccountDto
            {
                UserId = usuario.IdUsuario,
                Username = usuario.NombreUsuario,
                FirstName = usuario.Nombre,
                LastName = usuario.Apellidos,
                ProfileDescription = usuario.DescripcionPerfil,
                Coins = usuario.Monedas,

                HasProfilePhoto = !string.IsNullOrWhiteSpace(usuario.FotoPerfil),
                ProfilePhotoId = normalizedPhotoId,

                CurrentSkinUnlockedId = usuario.IdAvatarDesbloqueadoActual,
                CurrentSkinId = avatar != null
                    ? avatar.AvatarIdAvatar.ToString()
                    : null
            };
        }

        private static string NormalizePhotoId(string rawPhotoId)
        {
            if (string.IsNullOrWhiteSpace(rawPhotoId))
            {
                return DEFAULT_PROFILE_PHOTO_ID;
            }

            return rawPhotoId.Trim().ToUpperInvariant();
        }
    }
}
