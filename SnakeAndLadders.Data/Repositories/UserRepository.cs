using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core;              // 👈 IMPORTANTE
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.Data.SqlClient;
using System.Linq;
using log4net;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Interfaces;
using SnakesAndLadders.Server.Helpers;

namespace SnakesAndLadders.Data.Repositories
{
    public sealed class UserRepository : IUserRepository
    {
        private const int MAX_USERNAME_LENGTH = 50;
        private const int MAX_FIRST_NAME_LENGTH = 100;
        private const int MAX_LAST_NAME_LENGTH = 255;
        private const int MAX_DESCRIPTION_LENGTH = 500;

        private const int MIN_VALID_USER_ID = 1;

        private const byte STATUS_ACTIVE = 0x01;
        private const int STATUS_MIN_LENGTH = 1;
        private const int STATUS_ACTIVE_INDEX = 0;

        private const int CURRENT_AVATAR_ENTITY_ID_NONE = 0;

        private const int COMMAND_TIMEOUT_SECONDS = 30;

        private const string ERROR_USERNAME_REQUIRED = "username es obligatorio.";
        private const string ERROR_USERNAME_MAX_LENGTH_TEMPLATE =
            "username excede la longitud máxima permitida ({0}).";
        private const string ERROR_USER_ID_POSITIVE = "UserId must be positive.";
        private const string ERROR_FIRST_NAME_MAX_LENGTH_TEMPLATE =
            "FirstName exceeds {0} characters.";
        private const string ERROR_LAST_NAME_MAX_LENGTH_TEMPLATE =
            "LastName exceeds {0} characters.";
        private const string ERROR_PROFILE_DESCRIPTION_MAX_LENGTH_TEMPLATE =
            "ProfileDescription exceeds {0} characters.";
        private const string ERROR_USER_NOT_FOUND_OR_INACTIVE = "User not found or inactive.";

        private static readonly ILog Logger = LogManager.GetLogger(typeof(UserRepository));

        private readonly Func<SnakeAndLaddersDBEntities1> _contextFactory;

        public UserRepository(Func<SnakeAndLaddersDBEntities1> contextFactory = null)
        {
            _contextFactory = contextFactory ?? (() => new SnakeAndLaddersDBEntities1());
        }

        public AccountDto GetByUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException(ERROR_USERNAME_REQUIRED, nameof(username));
            }

            if (username.Length > MAX_USERNAME_LENGTH)
            {
                string message = string.Format(
                    ERROR_USERNAME_MAX_LENGTH_TEMPLATE,
                    MAX_USERNAME_LENGTH);

                throw new ArgumentException(message, nameof(username));
            }

            using (SnakeAndLaddersDBEntities1 db = _contextFactory())
            {
                ConfigureContext(db);

                try
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
                catch (DbEntityValidationException ex)
                {
                    Logger.Error("DB entity validation error in GetByUsername.", ex);
                    throw;
                }
                catch (DbUpdateException ex)
                {
                    Logger.Error("DB update error in GetByUsername.", ex);
                    throw;
                }
                catch (EntityException ex)
                {
                    Logger.Error("Entity framework error (connection / provider) in GetByUsername.", ex);
                    throw;
                }
                catch (SqlException ex)
                {
                    Logger.Error("SQL error in GetByUsername.", ex);
                    throw;
                }
                catch (Exception ex)
                {
                    Logger.Error("Unexpected error in GetByUsername.", ex);
                    throw;
                }
            }
        }

        public ProfilePhotoDto GetPhotoByUserId(int userId)
        {
            ValidateUserId(userId);

            using (SnakeAndLaddersDBEntities1 db = _contextFactory())
            {
                ConfigureContext(db);

                try
                {
                    Usuario entity = db.Usuario
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
                catch (DbEntityValidationException ex)
                {
                    Logger.Error("DB entity validation error in GetPhotoByUserId.", ex);
                    throw;
                }
                catch (DbUpdateException ex)
                {
                    Logger.Error("DB update error in GetPhotoByUserId.", ex);
                    throw;
                }
                catch (EntityException ex)
                {
                    Logger.Error("Entity framework error (connection / provider) in GetPhotoByUserId.", ex);
                    throw;
                }
                catch (SqlException ex)
                {
                    Logger.Error("SQL error in GetPhotoByUserId.", ex);
                    throw;
                }
                catch (Exception ex)
                {
                    Logger.Error("Unexpected error in GetPhotoByUserId.", ex);
                    throw;
                }
            }
        }

        public AccountDto UpdateProfile(UpdateProfileRequestDto request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (request.UserId < MIN_VALID_USER_ID)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(request),
                    ERROR_USER_ID_POSITIVE);
            }

            if (!string.IsNullOrWhiteSpace(request.FirstName) &&
                request.FirstName.Length > MAX_FIRST_NAME_LENGTH)
            {
                string message = string.Format(
                    ERROR_FIRST_NAME_MAX_LENGTH_TEMPLATE,
                    MAX_FIRST_NAME_LENGTH);

                throw new ArgumentException(message, nameof(request.FirstName));
            }

            if (!string.IsNullOrWhiteSpace(request.LastName) &&
                request.LastName.Length > MAX_LAST_NAME_LENGTH)
            {
                string message = string.Format(
                    ERROR_LAST_NAME_MAX_LENGTH_TEMPLATE,
                    MAX_LAST_NAME_LENGTH);

                throw new ArgumentException(message, nameof(request.LastName));
            }

            if (request.ProfileDescription != null &&
                request.ProfileDescription.Length > MAX_DESCRIPTION_LENGTH)
            {
                string message = string.Format(
                    ERROR_PROFILE_DESCRIPTION_MAX_LENGTH_TEMPLATE,
                    MAX_DESCRIPTION_LENGTH);

                throw new ArgumentException(message, nameof(request.ProfileDescription));
            }

            using (SnakeAndLaddersDBEntities1 db = _contextFactory())
            {
                ConfigureContext(db);

                try
                {
                    Usuario entity = db.Usuario
                        .SingleOrDefault(u => u.IdUsuario == request.UserId);

                    if (entity == null || !IsActive(entity.Estado))
                    {
                        throw new InvalidOperationException(ERROR_USER_NOT_FOUND_OR_INACTIVE);
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
                        throw new InvalidOperationException(ERROR_USER_NOT_FOUND_OR_INACTIVE);
                    }

                    return MapToAccountDto(row.Usuario, row.AvatarDesbloqueado, row.Avatar);
                }
                catch (DbEntityValidationException ex)
                {
                    Logger.Error("DB entity validation error in UpdateProfile.", ex);
                    throw;
                }
                catch (DbUpdateException ex)
                {
                    Logger.Error("DB update error in UpdateProfile.", ex);
                    throw;
                }
                catch (EntityException ex)
                {
                    Logger.Error("Entity framework error (connection / provider) in UpdateProfile.", ex);
                    throw;
                }
                catch (SqlException ex)
                {
                    Logger.Error("SQL error in UpdateProfile.", ex);
                    throw;
                }
                catch (Exception ex)
                {
                    Logger.Error("Unexpected error in UpdateProfile.", ex);
                    throw;
                }
            }
        }

        public AccountDto GetByUserId(int userId)
        {
            ValidateUserId(userId);

            using (SnakeAndLaddersDBEntities1 db = _contextFactory())
            {
                ConfigureContext(db);

                try
                {
                    Usuario user = db.Usuario
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
                catch (DbEntityValidationException ex)
                {
                    Logger.Error("DB entity validation error in GetByUserId.", ex);
                    throw;
                }
                catch (DbUpdateException ex)
                {
                    Logger.Error("DB update error in GetByUserId.", ex);
                    throw;
                }
                catch (EntityException ex)
                {
                    Logger.Error("Entity framework error (connection / provider) in GetByUserId.", ex);
                    throw;
                }
                catch (SqlException ex)
                {
                    Logger.Error("SQL error in GetByUserId.", ex);
                    throw;
                }
                catch (Exception ex)
                {
                    Logger.Error("Unexpected error in GetByUserId.", ex);
                    throw;
                }
            }
        }

        public AvatarProfileOptionsDto GetAvatarOptions(int userId)
        {
            ValidateUserId(userId);

            using (SnakeAndLaddersDBEntities1 db = _contextFactory())
            {
                ConfigureContext(db);

                try
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
                catch (DbEntityValidationException ex)
                {
                    Logger.Error("DB entity validation error in GetAvatarOptions.", ex);
                    throw;
                }
                catch (DbUpdateException ex)
                {
                    Logger.Error("DB update error in GetAvatarOptions.", ex);
                    throw;
                }
                catch (EntityException ex)
                {
                    Logger.Error("Entity framework error (connection / provider) in GetAvatarOptions.", ex);
                    throw;
                }
                catch (SqlException ex)
                {
                    Logger.Error("SQL error in GetAvatarOptions.", ex);
                    throw;
                }
                catch (Exception ex)
                {
                    Logger.Error("Unexpected error in GetAvatarOptions.", ex);
                    throw;
                }
            }
        }

        // --- helpers igual que antes ---

        private static AccountDto MapToAccountDto(
            Usuario usuario,
            AvatarDesbloqueado avatarDesbloqueado,
            Avatar avatar)
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
                UserName = usuario.NombreUsuario,
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

        private static void ValidateUserId(int userId)
        {
            if (userId < MIN_VALID_USER_ID)
            {
                throw new ArgumentOutOfRangeException(nameof(userId), ERROR_USER_ID_POSITIVE);
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
                throw new InvalidOperationException(ERROR_USER_NOT_FOUND_OR_INACTIVE);
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
                return CURRENT_AVATAR_ENTITY_ID_NONE;
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
            List<AvatarProfileOptionDto> options = new List<AvatarProfileOptionDto>();

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

        private static void AddSpecialAvatarOptions(
            IList<AvatarProfileOptionDto> options,
            IList<Avatar> avatarEntities,
            IList<int> unlockedAvatarIds,
            int currentAvatarEntityId)
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

        private static bool IsActive(byte[] estado)
        {
            return estado != null
                   && estado.Length >= STATUS_MIN_LENGTH
                   && estado[STATUS_ACTIVE_INDEX] == STATUS_ACTIVE;
        }

        private static void ConfigureContext(SnakeAndLaddersDBEntities1 context)
        {
            ((IObjectContextAdapter)context).ObjectContext.CommandTimeout = COMMAND_TIMEOUT_SECONDS;
        }
    }
}
