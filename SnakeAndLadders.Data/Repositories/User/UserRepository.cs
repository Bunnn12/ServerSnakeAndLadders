using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using log4net;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Interfaces;
using SnakesAndLadders.Data.Constants;
using SnakesAndLadders.Data.Helpers;
using SnakesAndLadders.Server.Helpers;

using static SnakesAndLadders.Data.Constants.UserRepositoryConstants;

namespace SnakesAndLadders.Data.Repositories
{
    public sealed class UserRepository : IUserRepository
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(UserRepository));

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

            using (SnakeAndLaddersDBEntities1 dataBase = _contextFactory())
            {
                ConfigureContext(dataBase);

                try
                {
                    var row =
                        (from u in dataBase.Usuario.AsNoTracking()
                         join ad in dataBase.AvatarDesbloqueado.AsNoTracking()
                             on u.IdAvatarDesbloqueadoActual equals ad.IdAvatarDesbloqueado
                             into avatarGroup
                         from ad in avatarGroup.DefaultIfEmpty()
                         join av in dataBase.Avatar.AsNoTracking()
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
                catch (Exception ex)
                {
                    RepositoryExceptionHandler.Handle(ex, _logger);
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
                catch (Exception ex)
                {
                    RepositoryExceptionHandler.Handle(ex, _logger);
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

                    return GetAccountWithAvatar(db, request.UserId);
                }
                catch (Exception ex)
                {
                    RepositoryExceptionHandler.Handle(ex, _logger);
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

                    AvatarDesbloqueado avatarUnlocked = null;
                    Avatar avatar = null;

                    if (user == null)
                    {
                        return null;
                    }

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
                catch (Exception ex)
                {
                    RepositoryExceptionHandler.Handle(ex, _logger);
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
                catch (Exception ex)
                {
                    RepositoryExceptionHandler.Handle(ex, _logger);
                    throw;
                }
            }
        }

        public AccountDto SelectAvatarForProfile(AvatarSelectionRequestDto request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            ValidateUserId(request.UserId);

            if (string.IsNullOrWhiteSpace(request.AvatarCode))
            {
                throw new ArgumentException(
                    ERROR_AVATAR_CODE_REQUIRED,
                    nameof(request.AvatarCode));
            }

            string normalizedCode = request.AvatarCode
                .Trim()
                .ToUpperInvariant();

            using (SnakeAndLaddersDBEntities1 db = _contextFactory())
            {
                ConfigureContext(db);

                try
                {
                    Usuario user = db.Usuario
                        .SingleOrDefault(u => u.IdUsuario == request.UserId);

                    if (user == null || !IsActive(user.Estado))
                    {
                        throw new InvalidOperationException(ERROR_USER_NOT_FOUND_OR_INACTIVE);
                    }

                    if (IsDefaultAvatarCode(normalizedCode))
                    {
                        user.FotoPerfil = normalizedCode;
                        user.IdAvatarDesbloqueadoActual = null;

                        db.SaveChanges();

                        return GetAccountWithAvatar(db, user.IdUsuario);
                    }

                    Avatar targetAvatar = db.Avatar
                        .SingleOrDefault(a =>
                            a.CodigoAvatar != null &&
                            a.CodigoAvatar.Trim().ToUpper() == normalizedCode);

                    if (targetAvatar == null)
                    {
                        throw new InvalidOperationException(ERROR_AVATAR_CODE_UNKNOWN);
                    }

                    AvatarDesbloqueado unlocked = db.AvatarDesbloqueado
                        .SingleOrDefault(ad =>
                            ad.UsuarioIdUsuario == user.IdUsuario &&
                            ad.AvatarIdAvatar == targetAvatar.IdAvatar);

                    if (unlocked == null)
                    {
                        throw new InvalidOperationException(ERROR_AVATAR_NOT_UNLOCKED);
                    }

                    user.IdAvatarDesbloqueadoActual = unlocked.IdAvatarDesbloqueado;

                    db.SaveChanges();

                    return GetAccountWithAvatar(db, user.IdUsuario);
                }
                catch (Exception ex)
                {
                    RepositoryExceptionHandler.Handle(ex, _logger);
                    throw;
                }
            }
        }

        private static AccountDto GetAccountWithAvatar(
            SnakeAndLaddersDBEntities1 db,
            int userId)
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
                 where u.IdUsuario == userId
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
            AddSpecialAvatarOptions(options, context);

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
            AvatarOptionsContext context)
        {
            foreach (Avatar avatar in context.AvatarEntities)
            {
                string skinCode = (avatar.CodigoAvatar ?? string.Empty).Trim();

                if (string.IsNullOrWhiteSpace(skinCode))
                {
                    continue;
                }

                bool isUnlocked = context.UnlockedAvatarIds.Contains(avatar.IdAvatar);
                bool isCurrent = context.CurrentAvatarEntityId == avatar.IdAvatar;

                options.Add(new AvatarProfileOptionDto
                {
                    AvatarCode = skinCode,
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

        private static bool IsDefaultAvatarCode(string avatarCode)
        {
            if (string.IsNullOrWhiteSpace(avatarCode))
            {
                return false;
            }

            string normalizedCode = avatarCode.Trim().ToUpperInvariant();

            foreach (string defaultCode in AvatarDefaults.DefaultAvatarCodes)
            {
                if (string.Equals(defaultCode, normalizedCode, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private sealed class AvatarOptionsContext
        {
            public string NormalizedPhotoId { get; set; }

            public IList<int> UnlockedAvatarIds { get; set; }

            public int CurrentAvatarEntityId { get; set; }

            public IList<Avatar> AvatarEntities { get; set; }
        }
    }
}
