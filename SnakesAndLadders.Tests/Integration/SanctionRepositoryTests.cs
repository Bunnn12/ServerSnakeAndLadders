using System;
using System.Collections.Generic;
using System.Linq;
using SnakeAndLadders.Contracts.Dtos;
using SnakesAndLadders.Data;
using SnakesAndLadders.Data.Repositories;
using SnakesAndLadders.Tests.integration;
using Xunit;

namespace SnakesAndLadders.Tests.Integration
{
    public sealed class SanctionRepositoryTests : IntegrationTestBase
    {
        private const int INVALID_USER_ID_ZERO = 0;
        private const int INVALID_USER_ID_NEGATIVE = -1;

        private const byte STATUS_ACTIVE = 0x01;

        private const string BASE_USERNAME = "SanctionUser";
        private const string BASE_FIRST_NAME = "Sanction";
        private const string BASE_LAST_NAME = "User";

        private SanctionRepository CreateRepository()
        {
            return new SanctionRepository(CreateContext);
        }

        private int CreateTestUser()
        {
            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                string suffix = Guid.NewGuid().ToString("N").Substring(0, 8);

                Usuario user = new Usuario
                {
                    NombreUsuario = $"{BASE_USERNAME}_{suffix}",
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


        [Fact]
        public void TestInsertSanctionWhenDtoIsNullThrowsArgumentNullException()
        {
            SanctionRepository repository = CreateRepository();

            Action action = () => repository.InsertSanction(null);

            Assert.Throws<ArgumentNullException>(action);
        }

        [Theory]
        [InlineData(INVALID_USER_ID_ZERO)]
        [InlineData(INVALID_USER_ID_NEGATIVE)]
        public void TestInsertSanctionWhenUserIdLessOrEqualZeroThrowsArgumentOutOfRangeException(int invalidUserId)
        {
            SanctionRepository repository = CreateRepository();

            SanctionDto dto = new SanctionDto
            {
                UserId = invalidUserId,
                SanctionDateUtc = DateTime.UtcNow,
                SanctionType = "TEST"
            };

            Action action = () => repository.InsertSanction(dto);

            Assert.Throws<ArgumentOutOfRangeException>(action);
        }

        [Fact]
        public void TestInsertSanctionWhenDtoIsValidPersistsEntityAndUpdatesDtoId()
        {
            int userId = CreateTestUser();

            SanctionRepository repository = CreateRepository();

            SanctionDto dto = new SanctionDto
            {
                UserId = userId,
                SanctionDateUtc = DateTime.UtcNow,
                SanctionType = "TEST"
            };

            repository.InsertSanction(dto);

            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                bool isOk =
                    dto.SanctionId > 0 &&
                    db.Sancion.Any(s => s.IdSancion == dto.SanctionId &&
                                        s.UsuarioIdUsuario == userId &&
                                        s.TipoSancion == "TEST");
                Assert.True(isOk);
            }
        }

        [Theory]
        [InlineData(INVALID_USER_ID_ZERO)]
        [InlineData(INVALID_USER_ID_NEGATIVE)]
        public void TestGetLastSanctionForUserWhenUserIdLessOrEqualZeroThrowsArgumentOutOfRangeException(int invalidUserId)
        {
            SanctionRepository repository = CreateRepository();

            Action action = () => repository.GetLastSanctionForUser(invalidUserId);

            Assert.Throws<ArgumentOutOfRangeException>(action);
        }

        [Fact]
        public void TestGetLastSanctionForUserWhenNoSanctionsReturnsEmptySanctionForUser()
        {
            int userId = CreateTestUser();

            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                db.Sancion.RemoveRange(db.Sancion.Where(s => s.UsuarioIdUsuario == userId));
                db.SaveChanges();
            }

            SanctionRepository repository = CreateRepository();

            SanctionDto result = repository.GetLastSanctionForUser(userId);

            bool isOk =
                result != null &&
                result.UserId == userId &&
                result.SanctionId == 0 &&
                result.SanctionDateUtc == default(DateTime) &&
                string.IsNullOrEmpty(result.SanctionType);

            Assert.True(isOk);
        }

        [Fact]
        public void TestGetLastSanctionForUserWhenSanctionsExistReturnsMostRecentByTypeMarker()
        {
            int userId = CreateTestUser();

            DateTime older = DateTime.UtcNow.AddDays(-2);
            DateTime newer = DateTime.UtcNow.AddDays(-1);

            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                db.Sancion.RemoveRange(db.Sancion.Where(s => s.UsuarioIdUsuario == userId));
                db.SaveChanges();

                db.Sancion.Add(new Sancion
                {
                    UsuarioIdUsuario = userId,
                    FechaSancion = older,
                    TipoSancion = "OLD"
                });

                db.Sancion.Add(new Sancion
                {
                    UsuarioIdUsuario = userId,
                    FechaSancion = newer,
                    TipoSancion = "NEW"
                });

                db.SaveChanges();
            }

            SanctionRepository repository = CreateRepository();

            SanctionDto result = repository.GetLastSanctionForUser(userId);

            bool isOk =
                result != null &&
                result.UserId == userId &&
                result.SanctionType == "NEW";

            Assert.True(isOk);
        }


        [Theory]
        [InlineData(INVALID_USER_ID_ZERO)]
        [InlineData(INVALID_USER_ID_NEGATIVE)]
        public void TestGetSanctionsHistoryWhenUserIdLessOrEqualZeroThrowsArgumentOutOfRangeException(int invalidUserId)
        {
            SanctionRepository repository = CreateRepository();

            Action action = () => repository.GetSanctionsHistory(invalidUserId);

            Assert.Throws<ArgumentOutOfRangeException>(action);
        }

        [Fact]
        public void TestGetSanctionsHistoryWhenNoSanctionsReturnsEmptyList()
        {
            int userId = CreateTestUser();

            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                db.Sancion.RemoveRange(db.Sancion.Where(s => s.UsuarioIdUsuario == userId));
                db.SaveChanges();
            }

            SanctionRepository repository = CreateRepository();

            IList<SanctionDto> result = repository.GetSanctionsHistory(userId);

            bool isOk = result != null && result.Count == 0;
            Assert.True(isOk);
        }

        [Fact]
        public void TestGetSanctionsHistoryWhenSanctionsExistReturnsOrderedByMostRecentTypeSequence()
        {
            int userId = CreateTestUser();

            DateTime oldest = DateTime.UtcNow.AddDays(-3);
            DateTime middle = DateTime.UtcNow.AddDays(-2);
            DateTime newest = DateTime.UtcNow.AddDays(-1);

            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                db.Sancion.RemoveRange(db.Sancion.Where(s => s.UsuarioIdUsuario == userId));
                db.SaveChanges();

                db.Sancion.Add(new Sancion
                {
                    UsuarioIdUsuario = userId,
                    FechaSancion = middle,
                    TipoSancion = "MIDDLE"
                });

                db.Sancion.Add(new Sancion
                {
                    UsuarioIdUsuario = userId,
                    FechaSancion = newest,
                    TipoSancion = "NEWEST"
                });

                db.Sancion.Add(new Sancion
                {
                    UsuarioIdUsuario = userId,
                    FechaSancion = oldest,
                    TipoSancion = "OLDEST"
                });

                db.SaveChanges();
            }

            SanctionRepository repository = CreateRepository();

            IList<SanctionDto> result = repository.GetSanctionsHistory(userId);

            bool isOk =
                result != null &&
                result.Count == 3 &&
                result[0].SanctionType == "NEWEST" &&
                result[1].SanctionType == "MIDDLE" &&
                result[2].SanctionType == "OLDEST";

            Assert.True(isOk);
        }
    }
}
