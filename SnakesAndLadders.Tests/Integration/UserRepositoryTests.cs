using SnakeAndLadders.Contracts.Dtos;
using SnakesAndLadders.Data;
using SnakesAndLadders.Data.Repositories;
using SnakesAndLadders.Tests.integration;
using System;
using System.Linq;
using Xunit;

namespace SnakesAndLadders.Tests.Integration
{
    public sealed class UserRepositoryTests : IntegrationTestBase
    {
        private const int MAX_USERNAME_LENGTH = 50;
        private const int MAX_FIRST_NAME_LENGTH = 100;
        private const int MAX_LAST_NAME_LENGTH = 255;
        private const int MAX_DESCRIPTION_LENGTH = 500;

        private const byte STATUS_ACTIVE = 0x01;
        private const byte STATUS_INACTIVE = 0x00;

        private const int NON_EXISTENT_USER_ID_1 = 9998;
        private const int NON_EXISTENT_USER_ID_2 = 9999;

        private const string USERNAME_NOT_EXISTS = "NoExisteUser";
        private const string USERNAME_INACTIVE = "InactiveUser";
        private const string USERNAME_PLAYER = "PlayerUser";
        private const string USERNAME_PHOTO = "PhotoUser";
        private const string USERNAME_PROFILE = "ProfileUser";
        private const string USERNAME_NO_AVATAR = "UserNoAvatar";
        private const string USERNAME_INACTIVE_AVATAR = "InactiveAvatarUser";
        private const string USERNAME_AVATAR = "AvatarUser";

        private const string AVATAR_RARITY_COMMON = "COMUN";
        private const string AVATAR_CODE_SPECIAL = "001";

        private const string PHOTO_DB = "DB_PHOTO";
        private const string PHOTO_OLD = "PHOTO_OLD";
        private const string PHOTO_NEW = "PHOTO_NEW";

        private const string AVATAR_NAME_SPECIAL = "AvatarEspecial";

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void TestGetByUsernameWhenUserNameIsNullOrWhiteSpaceThrowsArgumentException(string invalidUserName)
        {
            UserRepository repository = new UserRepository(CreateContext);

            Action action = () => repository.GetByUsername(invalidUserName);

            Assert.Throws<ArgumentException>(action);
        }


        [Fact]
        public void TestGetByUsernameWhenUserNameExceedsMaxLengthThrowsArgumentException()
        {
            UserRepository repository = new UserRepository(CreateContext);

            string username = new string('X', MAX_USERNAME_LENGTH + 1);
            Action action = () => repository.GetByUsername(username);

            Assert.Throws<ArgumentException>(action);
        }


        [Fact]
        public void TestGetByUsernameWhenUserNotFoundReturnsNull()
        {
            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                var existing = db.Usuario.Where(u => u.NombreUsuario == USERNAME_NOT_EXISTS);
                db.Usuario.RemoveRange(existing);
                db.SaveChanges();
            }

            UserRepository repository = new UserRepository(CreateContext);

            AccountDto result = repository.GetByUsername(USERNAME_NOT_EXISTS);

            Assert.Null(result);
        }

        [Fact]
        public void TestGetByUsernameWhenUserInactiveReturnsNull()
        {
            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                var existing = db.Usuario.Where(u => u.NombreUsuario == USERNAME_INACTIVE);
                db.Usuario.RemoveRange(existing);
                db.SaveChanges();

                Usuario user = new Usuario
                {
                    NombreUsuario = USERNAME_INACTIVE,
                    Nombre = "Inactive",
                    Apellidos = "User",
                    DescripcionPerfil = null,
                    FotoPerfil = null,
                    Monedas = 0,
                    Estado = new[] { STATUS_INACTIVE }
                };

                db.Usuario.Add(user);
                db.SaveChanges();
            }

            UserRepository repository = new UserRepository(CreateContext);

            AccountDto result = repository.GetByUsername(USERNAME_INACTIVE);

            Assert.Null(result);
        }


        [Fact]
        public void TestGetByUsernameWhenUserActiveReturnsAccountDtoWithUserId()
        {
            int userId;

            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                var existing = db.Usuario.Where(u => u.NombreUsuario == USERNAME_PLAYER);
                db.Usuario.RemoveRange(existing);
                db.SaveChanges();

                Usuario user = new Usuario
                {
                    NombreUsuario = USERNAME_PLAYER,
                    Nombre = "Juan",
                    Apellidos = "Pérez",
                    DescripcionPerfil = "Desc",
                    Monedas = 100,
                    Estado = new[] { STATUS_ACTIVE },
                    IdAvatarDesbloqueadoActual = null
                };

                db.Usuario.Add(user);
                db.SaveChanges();

                userId = user.IdUsuario;
            }

            UserRepository repository = new UserRepository(CreateContext);

            AccountDto result = repository.GetByUsername(USERNAME_PLAYER);

            Assert.Equal(userId, result.UserId);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void TestGetPhotoByUserIdWhenUserIdLessOrEqualZeroThrowsArgumentOutOfRangeException(int invalidUserId)
        {
            UserRepository repository = new UserRepository(CreateContext);

            Action action = () => repository.GetPhotoByUserId(invalidUserId);

            Assert.Throws<ArgumentOutOfRangeException>(action);
        }


        [Fact]
        public void TestGetPhotoByUserIdWhenUserNotFoundReturnsNull()
        {
            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                db.Usuario.RemoveRange(db.Usuario);
                db.SaveChanges();
            }

            UserRepository repository = new UserRepository(CreateContext);

            ProfilePhotoDto result = repository.GetPhotoByUserId(NON_EXISTENT_USER_ID_2);

            Assert.Null(result);
        }

        [Fact]
        public void TestGetPhotoByUserIdWhenUserActiveReturnsNormalizedPhotoDto()
        {
            int userId;

            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                db.Usuario.RemoveRange(db.Usuario);
                db.SaveChanges();

                Usuario user = new Usuario
                {
                    NombreUsuario = "PhotoUser",
                    Nombre = "Photo",
                    Apellidos = "User",
                    DescripcionPerfil = null,
                    FotoPerfil = "A0001",
                    Monedas = 0,
                    Estado = new[] { STATUS_ACTIVE }
                };

                db.Usuario.Add(user);
                db.SaveChanges();
                userId = user.IdUsuario;
            }

            UserRepository repository = new UserRepository(CreateContext);

            ProfilePhotoDto result = repository.GetPhotoByUserId(userId);

            bool isOk = result != null && result.UserId == userId;
            Assert.True(isOk);
        }


        [Fact]
        public void TestUpdateProfileWhenRequestIsNullThrowsArgumentNullException()
        {
            UserRepository repository = new UserRepository(CreateContext);

            Action action = () => repository.UpdateProfile(null);

            Assert.Throws<ArgumentNullException>(action);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void TestUpdateProfileWhenUserIdLessOrEqualZeroThrowsArgumentOutOfRangeException(int invalidUserId)
        {
            UserRepository repository = new UserRepository(CreateContext);

            UpdateProfileRequestDto request = new UpdateProfileRequestDto
            {
                UserId = invalidUserId
            };

            Action action = () => repository.UpdateProfile(request);

            Assert.Throws<ArgumentOutOfRangeException>(action);
        }

        [Fact]
        public void TestUpdateProfileWhenFirstNameExceedsMaxLengthThrowsArgumentException()
        {
            UserRepository repository = new UserRepository(CreateContext);

            UpdateProfileRequestDto request = new UpdateProfileRequestDto
            {
                UserId = 1,
                FirstName = new string('A', MAX_FIRST_NAME_LENGTH + 1)
            };

            Action action = () => repository.UpdateProfile(request);

            Assert.Throws<ArgumentException>(action);
        }

        [Fact]
        public void TestUpdateProfileWhenLastNameExceedsMaxLengthThrowsArgumentException()
        {
            UserRepository repository = new UserRepository(CreateContext);

            UpdateProfileRequestDto request = new UpdateProfileRequestDto
            {
                UserId = 1,
                LastName = new string('B', MAX_LAST_NAME_LENGTH + 1)
            };

            Action action = () => repository.UpdateProfile(request);

            Assert.Throws<ArgumentException>(action);
        }

        [Fact]
        public void TestUpdateProfileWhenProfileDescriptionExceedsMaxLengthThrowsArgumentException()
        {
            UserRepository repository = new UserRepository(CreateContext);

            UpdateProfileRequestDto request = new UpdateProfileRequestDto
            {
                UserId = 1,
                ProfileDescription = new string('C', MAX_DESCRIPTION_LENGTH + 1)
            };

            Action action = () => repository.UpdateProfile(request);

            Assert.Throws<ArgumentException>(action);
        }

        [Fact]
        public void TestUpdateProfileWhenUserNotFoundOrInactiveThrowsInvalidOperationException()
        {
            int userId;

            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                db.Usuario.RemoveRange(db.Usuario);
                db.SaveChanges();

                Usuario user = new Usuario
                {
                    NombreUsuario = "InactiveUser",
                    Nombre = "Inactive",
                    Apellidos = "User",
                    DescripcionPerfil = null,
                    FotoPerfil = null,
                    Monedas = 0,
                    Estado = new[] { STATUS_INACTIVE }
                };

                db.Usuario.Add(user);
                db.SaveChanges();
                userId = user.IdUsuario;
            }

            UserRepository repository = new UserRepository(CreateContext);

            UpdateProfileRequestDto request = new UpdateProfileRequestDto
            {
                UserId = userId,
                FirstName = "Nuevo"
            };

            Action action = () => repository.UpdateProfile(request);

            Assert.Throws<InvalidOperationException>(action);
        }

        [Fact]
        public void TestUpdateProfileUpdatesFieldsAndReturnsUpdatedAccountDto()
        {
            int userId;

            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                db.Usuario.RemoveRange(db.Usuario);
                db.SaveChanges();

                Usuario user = new Usuario
                {
                    NombreUsuario = "ProfileUser",
                    Nombre = "NombreOriginal",
                    Apellidos = "ApellidoOriginal",
                    DescripcionPerfil = "DescOriginal",
                    FotoPerfil = "A0001",
                    Monedas = 10,
                    Estado = new[] { STATUS_ACTIVE }
                };

                db.Usuario.Add(user);
                db.SaveChanges();
                userId = user.IdUsuario;
            }

            UserRepository repository = new UserRepository(CreateContext);

            UpdateProfileRequestDto request = new UpdateProfileRequestDto
            {
                UserId = userId,
                FirstName = "NuevoNombre",
                LastName = "NuevoApellido",
                ProfileDescription = "NuevaDesc",
                ProfilePhotoId = "A0002"
            };

            AccountDto result = repository.UpdateProfile(request);

            bool isOk = result != null && result.FirstName == "NuevoNombre";
            Assert.True(isOk);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public void TestGetByUserIdWhenUserIdLessOrEqualZeroThrowsArgumentOutOfRangeException(int invalidUserId)
        {
            UserRepository repository = new UserRepository(CreateContext);

            Action action = () => repository.GetByUserId(invalidUserId);

            Assert.Throws<ArgumentOutOfRangeException>(action);
        }

        [Fact]
        public void TestGetByUserIdWhenUserNotFoundReturnsNull()
        {
            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                db.Usuario.RemoveRange(db.Usuario);
                db.SaveChanges();
            }

            UserRepository repository = new UserRepository(CreateContext);

            AccountDto result = repository.GetByUserId(NON_EXISTENT_USER_ID_1);

            Assert.Null(result);
        }

        [Fact]
        public void TestGetByUserIdWhenUserExistsWithoutAvatarReturnsAccountDto()
        {
            int userId;

            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                db.Usuario.RemoveRange(db.Usuario);
                db.SaveChanges();

                Usuario user = new Usuario
                {
                    NombreUsuario = USERNAME_NO_AVATAR,
                    Nombre = "Nombre",
                    Apellidos = "Apellidos",
                    DescripcionPerfil = null,
                    Monedas = 5,
                    Estado = new[] { STATUS_ACTIVE },
                    IdAvatarDesbloqueadoActual = null
                };

                db.Usuario.Add(user);
                db.SaveChanges();
                userId = user.IdUsuario;
            }

            UserRepository repository = new UserRepository(CreateContext);

            AccountDto result = repository.GetByUserId(userId);

            Assert.Equal(userId, result.UserId);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-3)]
        public void TestGetAvatarOptionsWhenUserIdLessOrEqualZeroThrowsArgumentOutOfRangeException(int invalidUserId)
        {
            UserRepository repository = new UserRepository(CreateContext);

            Action action = () => repository.GetAvatarOptions(invalidUserId);

            Assert.Throws<ArgumentOutOfRangeException>(action);
        }

        [Fact]
        public void TestGetAvatarOptionsWhenUserNotFoundThrowsInvalidOperationException()
        {
            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                db.Usuario.RemoveRange(db.Usuario);
                db.SaveChanges();
            }

            UserRepository repository = new UserRepository(CreateContext);

            Action action = () => repository.GetAvatarOptions(60);

            Assert.Throws<InvalidOperationException>(action);
        }

        [Fact]
        public void TestGetAvatarOptionsWhenUserInactiveThrowsInvalidOperationException()
        {
            int userId;

            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                db.Usuario.RemoveRange(db.Usuario);
                db.SaveChanges();

                Usuario user = new Usuario
                {
                    NombreUsuario = USERNAME_INACTIVE_AVATAR,
                    Nombre = "Inactive",
                    Apellidos = "AvatarUser",
                    DescripcionPerfil = null,
                    FotoPerfil = null,
                    Monedas = 0,
                    Estado = new[] { STATUS_INACTIVE }
                };

                db.Usuario.Add(user);
                db.SaveChanges();
                userId = user.IdUsuario;
            }

            UserRepository repository = new UserRepository(CreateContext);

            Action action = () => repository.GetAvatarOptions(userId);

            Assert.Throws<InvalidOperationException>(action);
        }

        [Fact]
        public void TestGetAvatarOptionsReturnsSpecialAvatarAsCurrentWhenUnlocked()
        {
            int userId;

            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                db.AvatarDesbloqueado.RemoveRange(db.AvatarDesbloqueado);
                db.Usuario.RemoveRange(db.Usuario);
                db.Avatar.RemoveRange(db.Avatar);
                db.SaveChanges();

                Avatar avatar = new Avatar
                {
                    NombreAvatar = AVATAR_NAME_SPECIAL,
                    RarezaAvatar = AVATAR_RARITY_COMMON,
                    CajaAvatarIdCajaAvatar = null,
                    Estado = new[] { STATUS_ACTIVE },
                    CodigoAvatar = AVATAR_CODE_SPECIAL
                };

                db.Avatar.Add(avatar);
                db.SaveChanges();

                Usuario user = new Usuario
                {
                    NombreUsuario = USERNAME_AVATAR,
                    Nombre = "Avatar",
                    Apellidos = "User",
                    DescripcionPerfil = null,
                    FotoPerfil = null,
                    Monedas = 0,
                    Estado = new[] { STATUS_ACTIVE }
                };

                db.Usuario.Add(user);
                db.SaveChanges();
                userId = user.IdUsuario;

                AvatarDesbloqueado unlocked = new AvatarDesbloqueado
                {
                    AvatarIdAvatar = avatar.IdAvatar,
                    UsuarioIdUsuario = userId,
                    FechaDesbloqueo = DateTime.UtcNow
                };

                db.AvatarDesbloqueado.Add(unlocked);
                db.SaveChanges();

                user.IdAvatarDesbloqueadoActual = unlocked.IdAvatarDesbloqueado;
                db.SaveChanges();
            }

            UserRepository repository = new UserRepository(CreateContext);

            AvatarProfileOptionsDto result = repository.GetAvatarOptions(userId);

            bool hasCurrentSpecial = result.Avatars.Any(option =>
                option.IsCurrent && !string.IsNullOrWhiteSpace(option.AvatarCode));

            Assert.True(hasCurrentSpecial);
        }

    }
}
