using System;
using System.Collections.Generic;
using System.Linq;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Enums;
using SnakesAndLadders.Data;
using SnakesAndLadders.Data.Repositories;
using SnakesAndLadders.Tests.integration;
using Xunit;

namespace SnakesAndLadders.Tests.Integration
{
    public sealed class SocialProfileRepositoryTests : IntegrationTestBase
    {
        private const int MAX_PROFILE_LINK_LENGTH = 510;

        private const int INVALID_USER_ID_ZERO = 0;
        private const int INVALID_USER_ID_NEGATIVE = -1;

        private const byte STATUS_ACTIVE = 0x01;

        private const string PROFILE_LINK_BASE = "https://example.com/profile/";
        private const string PROFILE_LINK_UPDATED_SUFFIX = "/updated";

        private const string NETWORK_CODE_INSTAGRAM = "INSTAGRAM";
        private const string NETWORK_CODE_FACEBOOK = "FACEBOOK";
        private const string NETWORK_CODE_TWITTER = "TWITTER";
        private const string NETWORK_CODE_INVALID = "UNKNOWN";

        private const string BASE_USERNAME = "SocialUser";
        private const string BASE_FIRST_NAME = "Social";
        private const string BASE_LAST_NAME = "User";

        private SocialProfileRepository CreateRepository()
        {
            return new SocialProfileRepository(CreateContext);
        }

        private int CreateTestUser()
        {
            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                string uniqueSuffix = Guid.NewGuid().ToString("N").Substring(0, 8);

                Usuario user = new Usuario
                {
                    NombreUsuario = $"{BASE_USERNAME}_{uniqueSuffix}",
                    Nombre = BASE_FIRST_NAME,
                    Apellidos = BASE_LAST_NAME,
                    DescripcionPerfil = null,
                    FotoPerfil = null,
                    Monedas = 0,
                    Estado = new[] { STATUS_ACTIVE },
                    IdAvatarDesbloqueadoActual = null
                };

                db.Usuario.Add(user);
                db.SaveChanges();

                return user.IdUsuario;
            }
        }

        [Theory]
        [InlineData(INVALID_USER_ID_ZERO)]
        [InlineData(INVALID_USER_ID_NEGATIVE)]
        public void TestGetByUserIdWhenUserIdLessOrEqualZeroThrowsArgumentOutOfRangeException(int invalidUserId)
        {
            SocialProfileRepository repository = CreateRepository();

            Action action = () => repository.GetByUserId(invalidUserId);

            Assert.Throws<ArgumentOutOfRangeException>(action);
        }

        [Fact]
        public void TestGetByUserIdWhenUserHasNoSocialProfilesReturnsEmptyList()
        {
            int userId = CreateTestUser();

            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                db.RedesSociales.RemoveRange(db.RedesSociales);
                db.SaveChanges();
            }

            SocialProfileRepository repository = CreateRepository();

            IReadOnlyList<SocialProfileDto> result = repository.GetByUserId(userId);

            bool isOk = result != null && !result.Any();
            Assert.True(isOk);
        }

        [Fact]
        public void TestGetByUserIdWhenUserHasValidProfilesReturnsMappedDtos()
        {
            int userId = CreateTestUser();

            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                db.RedesSociales.RemoveRange(db.RedesSociales);
                db.SaveChanges();

                db.RedesSociales.Add(new RedesSociales
                {
                    UsuarioIdUsuario = userId,
                    TipoRedSocial = NETWORK_CODE_INSTAGRAM,
                    LinkRedSocial = PROFILE_LINK_BASE + "insta"
                });

                db.RedesSociales.Add(new RedesSociales
                {
                    UsuarioIdUsuario = userId,
                    TipoRedSocial = NETWORK_CODE_FACEBOOK,
                    LinkRedSocial = PROFILE_LINK_BASE + "fb"
                });

                db.SaveChanges();
            }

            SocialProfileRepository repository = CreateRepository();

            IReadOnlyList<SocialProfileDto> result = repository.GetByUserId(userId);

            bool isOk =
                result != null &&
                result.Count == 2 &&
                result.Any(p =>
                    p.UserId == userId &&
                    p.Network == SocialNetworkType.Instagram &&
                    p.ProfileLink == PROFILE_LINK_BASE + "insta") &&
                result.Any(p =>
                    p.UserId == userId &&
                    p.Network == SocialNetworkType.Facebook &&
                    p.ProfileLink == PROFILE_LINK_BASE + "fb");

            Assert.True(isOk);
        }

        [Fact]
        public void TestGetByUserIdWhenRowHasUnknownNetworkSkipsThatRow()
        {
            int userId = CreateTestUser();

            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                db.RedesSociales.RemoveRange(db.RedesSociales);
                db.SaveChanges();

                db.RedesSociales.Add(new RedesSociales
                {
                    UsuarioIdUsuario = userId,
                    TipoRedSocial = NETWORK_CODE_INVALID,
                    LinkRedSocial = PROFILE_LINK_BASE + "invalid"
                });

                db.RedesSociales.Add(new RedesSociales
                {
                    UsuarioIdUsuario = userId,
                    TipoRedSocial = NETWORK_CODE_TWITTER,
                    LinkRedSocial = PROFILE_LINK_BASE + "twitter"
                });

                db.SaveChanges();
            }

            SocialProfileRepository repository = CreateRepository();

            IReadOnlyList<SocialProfileDto> result = repository.GetByUserId(userId);

            bool isOk =
                result.Count == 1 &&
                result[0].Network == SocialNetworkType.Twitter &&
                result[0].ProfileLink == PROFILE_LINK_BASE + "twitter";

            Assert.True(isOk);
        }

        [Fact]
        public void TestGetByUserIdWhenNetworkCodesHaveDifferentCasingAreParsedCorrectly()
        {
            int userId = CreateTestUser();

            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                db.RedesSociales.RemoveRange(db.RedesSociales);
                db.SaveChanges();

                db.RedesSociales.Add(new RedesSociales
                {
                    UsuarioIdUsuario = userId,
                    TipoRedSocial = "instagram",
                    LinkRedSocial = PROFILE_LINK_BASE + "insta-lower"
                });

                db.RedesSociales.Add(new RedesSociales
                {
                    UsuarioIdUsuario = userId,
                    TipoRedSocial = " FaCeBoOk  ",
                    LinkRedSocial = PROFILE_LINK_BASE + "fb-mixed"
                });

                db.SaveChanges();
            }

            SocialProfileRepository repository = CreateRepository();

            IReadOnlyList<SocialProfileDto> result = repository.GetByUserId(userId);

            bool isOk =
                result.Count == 2 &&
                result.Any(p =>
                    p.Network == SocialNetworkType.Instagram &&
                    p.ProfileLink == PROFILE_LINK_BASE + "insta-lower") &&
                result.Any(p =>
                    p.Network == SocialNetworkType.Facebook &&
                    p.ProfileLink == PROFILE_LINK_BASE + "fb-mixed");

            Assert.True(isOk);
        }

        [Fact]
        public void TestUpsertWhenRequestIsNullThrowsArgumentNullException()
        {
            SocialProfileRepository repository = CreateRepository();

            Action action = () => repository.Upsert(null);

            Assert.Throws<ArgumentNullException>(action);
        }

        [Theory]
        [InlineData(INVALID_USER_ID_ZERO)]
        [InlineData(INVALID_USER_ID_NEGATIVE)]
        public void TestUpsertWhenUserIdLessOrEqualZeroThrowsArgumentOutOfRangeException(int invalidUserId)
        {
            SocialProfileRepository repository = CreateRepository();

            LinkSocialProfileRequestDto request = new LinkSocialProfileRequestDto
            {
                UserId = invalidUserId,
                Network = SocialNetworkType.Instagram,
                ProfileLink = PROFILE_LINK_BASE + "user"
            };

            Action action = () => repository.Upsert(request);

            Assert.Throws<ArgumentOutOfRangeException>(action);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void TestUpsertWhenProfileLinkIsNullOrWhiteSpaceThrowsArgumentException(string invalidLink)
        {
            int userId = CreateTestUser();

            SocialProfileRepository repository = CreateRepository();

            LinkSocialProfileRequestDto request = new LinkSocialProfileRequestDto
            {
                UserId = userId,
                Network = SocialNetworkType.Instagram,
                ProfileLink = invalidLink
            };

            Action action = () => repository.Upsert(request);

            Assert.Throws<ArgumentException>(action);
        }

        [Fact]
        public void TestUpsertWhenProfileLinkExceedsMaxLengthTruncatesToMaxLength()
        {
            int userId = CreateTestUser();

            SocialProfileRepository repository = CreateRepository();

            string longLink = new string('x', MAX_PROFILE_LINK_LENGTH + 10);

            LinkSocialProfileRequestDto request = new LinkSocialProfileRequestDto
            {
                UserId = userId,
                Network = SocialNetworkType.Instagram,
                ProfileLink = longLink
            };

            SocialProfileDto result = repository.Upsert(request);

            bool isOk =
                result != null &&
                result.UserId == userId &&
                result.Network == SocialNetworkType.Instagram &&
                result.ProfileLink.Length <= MAX_PROFILE_LINK_LENGTH;

            Assert.True(isOk);
        }

        [Fact]
        public void TestUpsertWhenProfileDoesNotExistInsertsNewRow()
        {
            int userId = CreateTestUser();

            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                db.RedesSociales.RemoveRange(db.RedesSociales);
                db.SaveChanges();
            }

            SocialProfileRepository repository = CreateRepository();

            LinkSocialProfileRequestDto request = new LinkSocialProfileRequestDto
            {
                UserId = userId,
                Network = SocialNetworkType.Facebook,
                ProfileLink = PROFILE_LINK_BASE + "new"
            };

            SocialProfileDto result = repository.Upsert(request);

            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                int count = db.RedesSociales.Count(r =>
                    r.UsuarioIdUsuario == userId &&
                    r.TipoRedSocial == NETWORK_CODE_FACEBOOK);

                bool isOk =
                    result != null &&
                    result.UserId == userId &&
                    result.Network == SocialNetworkType.Facebook &&
                    result.ProfileLink == PROFILE_LINK_BASE + "new" &&
                    count == 1;

                Assert.True(isOk);
            }
        }

        [Fact]
        public void TestUpsertWhenProfileExistsUpdatesLinkWithoutDuplicatingRow()
        {
            int userId = CreateTestUser();

            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                db.RedesSociales.RemoveRange(db.RedesSociales);
                db.SaveChanges();

                db.RedesSociales.Add(new RedesSociales
                {
                    UsuarioIdUsuario = userId,
                    TipoRedSocial = NETWORK_CODE_TWITTER,
                    LinkRedSocial = PROFILE_LINK_BASE + "old"
                });

                db.SaveChanges();
            }

            SocialProfileRepository repository = CreateRepository();

            LinkSocialProfileRequestDto request = new LinkSocialProfileRequestDto
            {
                UserId = userId,
                Network = SocialNetworkType.Twitter,
                ProfileLink = PROFILE_LINK_BASE + "old" + PROFILE_LINK_UPDATED_SUFFIX
            };

            SocialProfileDto result = repository.Upsert(request);

            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                List<RedesSociales> rows = db.RedesSociales
                    .Where(r => r.UsuarioIdUsuario == userId &&
                                r.TipoRedSocial == NETWORK_CODE_TWITTER)
                    .ToList();

                bool isOk =
                    result != null &&
                    result.UserId == userId &&
                    result.Network == SocialNetworkType.Twitter &&
                    result.ProfileLink == PROFILE_LINK_BASE + "old" + PROFILE_LINK_UPDATED_SUFFIX &&
                    rows.Count == 1 &&
                    rows[0].LinkRedSocial == PROFILE_LINK_BASE + "old" + PROFILE_LINK_UPDATED_SUFFIX;

                Assert.True(isOk);
            }
        }

        [Fact]
        public void TestDeleteSocialNetworkWhenRequestIsNullThrowsArgumentNullException()
        {
            SocialProfileRepository repository = CreateRepository();

            Action action = () => repository.DeleteSocialNetwork(null);

            Assert.Throws<ArgumentNullException>(action);
        }

        [Theory]
        [InlineData(INVALID_USER_ID_ZERO)]
        [InlineData(INVALID_USER_ID_NEGATIVE)]
        public void TestDeleteSocialNetworkWhenUserIdLessOrEqualZeroThrowsArgumentOutOfRangeException(int invalidUserId)
        {
            SocialProfileRepository repository = CreateRepository();

            UnlinkSocialProfileRequestDto request = new UnlinkSocialProfileRequestDto
            {
                UserId = invalidUserId,
                Network = SocialNetworkType.Facebook
            };

            Action action = () => repository.DeleteSocialNetwork(request);

            Assert.Throws<ArgumentOutOfRangeException>(action);
        }

        [Fact]
        public void TestDeleteSocialNetworkWhenProfileExistsRemovesRow()
        {
            int userId = CreateTestUser();

            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                db.RedesSociales.RemoveRange(db.RedesSociales);
                db.SaveChanges();

                db.RedesSociales.Add(new RedesSociales
                {
                    UsuarioIdUsuario = userId,
                    TipoRedSocial = NETWORK_CODE_INSTAGRAM,
                    LinkRedSocial = PROFILE_LINK_BASE + "to-delete"
                });

                db.SaveChanges();
            }

            SocialProfileRepository repository = CreateRepository();

            UnlinkSocialProfileRequestDto request = new UnlinkSocialProfileRequestDto
            {
                UserId = userId,
                Network = SocialNetworkType.Instagram
            };

            repository.DeleteSocialNetwork(request);

            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                bool exists = db.RedesSociales.Any(r =>
                    r.UsuarioIdUsuario == userId &&
                    r.TipoRedSocial == NETWORK_CODE_INSTAGRAM);

                Assert.False(exists);
            }
        }

        [Fact]
        public void TestDeleteSocialNetworkWhenProfileDoesNotExistDoesNothing()
        {
            int userId = CreateTestUser();

            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                db.RedesSociales.RemoveRange(db.RedesSociales);
                db.SaveChanges();
            }

            SocialProfileRepository repository = CreateRepository();

            UnlinkSocialProfileRequestDto request = new UnlinkSocialProfileRequestDto
            {
                UserId = userId,
                Network = SocialNetworkType.Twitter
            };

            repository.DeleteSocialNetwork(request);

            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                int count = db.RedesSociales.Count();
                Assert.Equal(0, count);
            }
        }
    }
}
