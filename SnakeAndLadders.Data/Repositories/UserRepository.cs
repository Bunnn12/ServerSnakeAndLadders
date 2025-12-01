using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Interfaces;
using SnakesAndLadders.Server.Helpers;
using System;
using System.Collections.Generic;
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

        private const byte STATUS_ACTIVE = 0x01;

        private static bool IsActive(byte[] estado)
        {
            return estado != null
                   && estado.Length > 0
                   && estado[0] == STATUS_ACTIVE;
        }

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
                     join ad in db.AvatarDesbloqueado.AsNoTracking()
                         on u.IdAvatarDesbloqueadoActual equals ad.IdAvatarDesbloqueado
                         into avatarGroup
                     from ad in avatarGroup.DefaultIfEmpty()
                     join av in db.Avatar.AsNoTracking()
                         on ad.AvatarIdAvatar equals av.IdAvatar
                         into avatarEntityGroup
                     from av in avatarEntityGroup.DefaultIfEmpty()
                     where u.NombreUsuario == username
                     select new
                     {
                         Usuario = u,
                         AvatarDesbloqueado = ad,
                         Avatar = av
                     })
                    .FirstOrDefault();

                if (row == null || !IsActive(row.Usuario.Estado))
                {
                    return null;
                }

                return MapToAccountDto(row.Usuario, row.AvatarDesbloqueado, row.Avatar);
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

                if (entity == null || !IsActive(entity.Estado))
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

                if (entity == null || !IsActive(entity.Estado))
                {
                    throw new InvalidOperationException("User not found or inactive.");
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
                     join ad in db.AvatarDesbloqueado.AsNoTracking()
                         on u.IdAvatarDesbloqueadoActual equals ad.IdAvatarDesbloqueado
                         into avatarGroup
                     from ad in avatarGroup.DefaultIfEmpty()
                     join av in db.Avatar.AsNoTracking()
                         on ad.AvatarIdAvatar equals av.IdAvatar
                         into avatarEntityGroup
                     from av in avatarEntityGroup.DefaultIfEmpty()
                     where u.IdUsuario == request.UserId
                     select new
                     {
                         Usuario = u,
                         AvatarDesbloqueado = ad,
                         Avatar = av
                     })
                    .FirstOrDefault();

                if (row == null || !IsActive(row.Usuario.Estado))
                {
                    throw new InvalidOperationException("User not found or inactive.");
                }

                return MapToAccountDto(row.Usuario, row.AvatarDesbloqueado, row.Avatar);
            }
        }

        public AccountDto GetByUserId(int userId)
        {
            if (userId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(userId));
            }

            using (var db = new SnakeAndLaddersDBEntities1())
            {
                var user = db.Usuario
                    .AsNoTracking()
                    .SingleOrDefault(u => u.IdUsuario == userId);

                if (user == null)
                {
                    return null;
                }

                AvatarDesbloqueado avatarUnlocked = null;
                Avatar avatar = null;

                if (user.IdAvatarDesbloqueadoActual.HasValue)
                {
                    int unlockedId = user.IdAvatarDesbloqueadoActual.Value;

                    avatarUnlocked = db.AvatarDesbloqueado
                        .AsNoTracking()
                        .Where(ad => ad.IdAvatarDesbloqueado == unlockedId)
                        .OrderByDescending(ad => ad.IdAvatarDesbloqueado)
                        .FirstOrDefault();

                    if (avatarUnlocked != null)
                    {
                        avatar = db.Avatar
                            .AsNoTracking()
                            .Where(av => av.IdAvatar == avatarUnlocked.AvatarIdAvatar)
                            .OrderByDescending(av => av.IdAvatar)
                            .FirstOrDefault();
                    }
                }

                return MapToAccountDto(user, avatarUnlocked, avatar);
            }
        }


        private static AccountDto MapToAccountDto( Usuario usuario, 
            AvatarDesbloqueado avatarDesbloqueado, Avatar avatar)
        {
            if (usuario == null)
            {
                return null;
            }

            string profilePhotoId;

            if (avatar != null &&
                AvatarSpecialMapping.TryGetAvatarCode(avatar.IdAvatar, out string mappedCode))
            {
                profilePhotoId = mappedCode;
            }
            else
            {
                profilePhotoId = NormalizePhotoId(usuario.FotoPerfil);
            }

            string currentSkinCode = null;

            if (avatar != null &&
                !string.IsNullOrWhiteSpace(avatar.CodigoAvatar))
            {
                currentSkinCode = avatar.CodigoAvatar
                    .Trim()
                    .ToUpperInvariant();
            }

            return new AccountDto
            {
                UserId = usuario.IdUsuario,
                Username = usuario.NombreUsuario,
                FirstName = usuario.Nombre,
                LastName = usuario.Apellidos,
                ProfileDescription = usuario.DescripcionPerfil,
                Coins = usuario.Monedas,

                HasProfilePhoto = !string.IsNullOrWhiteSpace(profilePhotoId),
                ProfilePhotoId = profilePhotoId,

                CurrentSkinUnlockedId = usuario.IdAvatarDesbloqueadoActual,
                CurrentSkinId = currentSkinCode
            };
        }


        private static string NormalizePhotoId(string rawPhotoId)
        {
            return AvatarIdHelper.MapFromDb(rawPhotoId);
        }

        public AvatarProfileOptionsDto GetAvatarOptions(int userId)
        {
            ValidateUserId(userId);

            using (var db = new SnakeAndLaddersDBEntities1())
            {
                Usuario user = GetActiveUser(db, userId);

                AvatarOptionsContext context = BuildAvatarOptionsContext(db, user);

                IList<AvatarProfileOptionDto> options = BuildAvatarOptions(context);

                return new AvatarProfileOptionsDto
                {
                    UserId = user.IdUsuario,
                    Avatars = options
                };
            }
        }

        private static void ValidateUserId(int userId)
        {
            if (userId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(userId));
            }
        }

        private static Usuario GetActiveUser(
            SnakeAndLaddersDBEntities1 db,
            int userId)
        {
            Usuario user = db.Usuario
                .AsNoTracking()
                .SingleOrDefault(u => u.IdUsuario == userId);

            if (user == null || !IsActive(user.Estado))
            {
                throw new InvalidOperationException("User not found or inactive.");
            }

            return user;
        }

        private static AvatarOptionsContext BuildAvatarOptionsContext(
            SnakeAndLaddersDBEntities1 db,
            Usuario user)
        {
            IList<int> unlockedAvatarIds = GetUnlockedAvatarIds(db, user.IdUsuario);
            int currentAvatarEntityId = GetCurrentAvatarEntityId(db, user);
            IList<Avatar> avatarEntities = GetAvatarEntities(db);
            string normalizedPhotoId = NormalizePhotoId(user.FotoPerfil);

            return new AvatarOptionsContext
            {
                NormalizedPhotoId = normalizedPhotoId,
                UnlockedAvatarIds = unlockedAvatarIds,
                CurrentAvatarEntityId = currentAvatarEntityId,
                AvatarEntities = avatarEntities
            };
        }

        private static IList<int> GetUnlockedAvatarIds(
            SnakeAndLaddersDBEntities1 db,
            int userId)
        {
            return db.AvatarDesbloqueado
                .AsNoTracking()
                .Where(ad => ad.UsuarioIdUsuario == userId)
                .Select(ad => ad.AvatarIdAvatar)
                .ToList();
        }

        private static int GetCurrentAvatarEntityId(
            SnakeAndLaddersDBEntities1 db,
            Usuario user)
        {
            if (!user.IdAvatarDesbloqueadoActual.HasValue)
            {
                return 0;
            }

            int unlockedId = user.IdAvatarDesbloqueadoActual.Value;

            return db.AvatarDesbloqueado
                .AsNoTracking()
                .Where(ad => ad.IdAvatarDesbloqueado == unlockedId)
                .Select(ad => ad.AvatarIdAvatar)
                .SingleOrDefault();
        }

        private static IList<Avatar> GetAvatarEntities(SnakeAndLaddersDBEntities1 db)
        {
            return db.Avatar
                .AsNoTracking()
                .OrderBy(a => a.IdAvatar)
                .ToList();
        }

        private static IList<AvatarProfileOptionDto> BuildAvatarOptions(
            AvatarOptionsContext context)
        {
            var options = new List<AvatarProfileOptionDto>();

            AddDefaultAvatarOptions(options, context.NormalizedPhotoId);
            AddSpecialAvatarOptions(
                options,
                context.AvatarEntities,
                context.UnlockedAvatarIds,
                context.CurrentAvatarEntityId);

            return options;
        }

        private static void AddDefaultAvatarOptions(
            IList<AvatarProfileOptionDto> options,
            string normalizedPhotoId)
        {
            foreach (string defaultCode in AvatarDefaults.DefaultAvatarCodes)
            {
                bool isCurrentDefault = string.Equals(
                    normalizedPhotoId,
                    defaultCode,
                    StringComparison.OrdinalIgnoreCase);

                options.Add(new AvatarProfileOptionDto
                {
                    AvatarCode = defaultCode,
                    DisplayName = defaultCode,
                    IsUnlocked = true,
                    IsCurrent = isCurrentDefault
                });
            }
        }

        private static void AddSpecialAvatarOptions( IList<AvatarProfileOptionDto> options,
            IList<Avatar> avatarEntities, IList<int> unlockedAvatarIds, int currentAvatarEntityId)
        {
            foreach (Avatar avatar in avatarEntities)
            {
                if (!AvatarSpecialMapping.TryGetAvatarCode(avatar.IdAvatar, out string avatarCode))
                {
                    continue;
                }

                bool isUnlocked = unlockedAvatarIds.Contains(avatar.IdAvatar);
                bool isCurrent = currentAvatarEntityId == avatar.IdAvatar;

                options.Add(new AvatarProfileOptionDto
                {
                    AvatarCode = avatarCode,              
                    DisplayName = avatar.NombreAvatar,    
                    IsUnlocked = isUnlocked,
                    IsCurrent = isCurrent
                });
            }
        }

       
    }
}
