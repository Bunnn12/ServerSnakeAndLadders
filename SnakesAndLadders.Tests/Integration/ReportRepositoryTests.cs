using System;
using System.Collections.Generic;
using System.Linq;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Interfaces;
using SnakesAndLadders.Data;
using SnakesAndLadders.Data.Repositories;
using SnakesAndLadders.Tests.integration;
using Xunit;

namespace SnakesAndLadders.Tests.Integration
{
    public sealed class ReportRepositoryTests : IntegrationTestBase
    {
        private const byte STATUS_ACTIVE = 0x01;
        private const int INITIAL_COINS = 0;

        private const int INVALID_USER_ID_ZERO = 0;
        private const int INVALID_USER_ID_NEGATIVE = -1;

        private const string BASE_USERNAME = "ReportUser";
        private const string BASE_FIRST_NAME = "Report";
        private const string BASE_LAST_NAME = "User";

        private ReportRepository CreateRepository()
        {
            return new ReportRepository(CreateContext);
        }

        private int CreateUser(string suffix)
        {
            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                Usuario user = new Usuario
                {
                    NombreUsuario = $"{BASE_USERNAME}_{suffix}",
                    Nombre = BASE_FIRST_NAME,
                    Apellidos = BASE_LAST_NAME,
                    DescripcionPerfil = null,
                    FotoPerfil = null,
                    Monedas = INITIAL_COINS,
                    Estado = new[] { STATUS_ACTIVE },
                    IdAvatarDesbloqueadoActual = null
                };

                db.Usuario.Add(user);
                db.SaveChanges();

                return user.IdUsuario;
            }
        }

        private int CreateUser()
        {
            string suffix = Guid.NewGuid().ToString("N").Substring(0, 8);
            return CreateUser(suffix);
        }

        [Fact]
        public void TestInsertReportWhenDtoIsNullThrowsArgumentNullException()
        {
            ReportRepository repository = CreateRepository();

            Action action = () => repository.InsertReport(null);

            bool throws =
                Assert.Throws<ArgumentNullException>(action) != null;

            Assert.True(throws);
        }

        [Theory]
        [InlineData(INVALID_USER_ID_ZERO)]
        [InlineData(INVALID_USER_ID_NEGATIVE)]
        public void TestInsertReportWhenReportedUserIdNotPositiveThrowsArgumentOutOfRange(
            int invalidReportedUserId)
        {
            int reporterUserId = CreateUser();

            ReportDto dto = new ReportDto
            {
                ReportReason = "Spam",
                ReportedUserId = invalidReportedUserId,
                ReporterUserId = reporterUserId
            };

            ReportRepository repository = CreateRepository();

            Action action = () => repository.InsertReport(dto);

            bool throws =
                Assert.Throws<ArgumentOutOfRangeException>(action) != null;

            Assert.True(throws);
        }

        [Theory]
        [InlineData(INVALID_USER_ID_ZERO)]
        [InlineData(INVALID_USER_ID_NEGATIVE)]
        public void TestInsertReportWhenReporterUserIdNotPositiveThrowsArgumentOutOfRange(
            int invalidReporterUserId)
        {
            int reportedUserId = CreateUser();

            ReportDto dto = new ReportDto
            {
                ReportReason = "Abuse",
                ReportedUserId = reportedUserId,
                ReporterUserId = invalidReporterUserId
            };

            ReportRepository repository = CreateRepository();

            Action action = () => repository.InsertReport(dto);

            bool throws =
                Assert.Throws<ArgumentOutOfRangeException>(action) != null;

            Assert.True(throws);
        }

        [Fact]
        public void TestInsertReportWithValidDtoInsertsRow()
        {
            int reportedUserId = CreateUser("Reported");
            int reporterUserId = CreateUser("Reporter");
            const string reason = "Mal comportamiento en partida";

            ReportRepository repository = CreateRepository();

            ReportDto dto = new ReportDto
            {
                ReportReason = reason,
                ReportedUserId = reportedUserId,
                ReporterUserId = reporterUserId
            };

            repository.InsertReport(dto);

            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                Reporte stored = db.Reporte.SingleOrDefault(r =>
                    r.IdUsuarioReportado == reportedUserId &&
                    r.IdUsuarioQueReporta == reporterUserId &&
                    r.RazonReporte == reason);

                bool isOk = stored != null;
                Assert.True(isOk);
            }
        }

        [Fact]
        public void TestReporterHasActiveReportWhenCriteriaIsNullThrowsArgumentNullException()
        {
            ReportRepository repository = CreateRepository();

            Action action = () => repository.ReporterHasActiveReport(null);

            bool throws =
                Assert.Throws<ArgumentNullException>(action) != null;

            Assert.True(throws);
        }

        [Theory]
        [InlineData(INVALID_USER_ID_ZERO)]
        [InlineData(INVALID_USER_ID_NEGATIVE)]
        public void TestReporterHasActiveReportWhenReportedUserIdNotPositiveThrowsArgumentOutOfRange(
            int invalidReportedUserId)
        {
            int reporterUserId = CreateUser();

            ActiveReportSearchCriteriaDto criteria =
                new ActiveReportSearchCriteriaDto
                {
                    ReportedUserId = invalidReportedUserId,
                    ReporterUserId = reporterUserId,
                    LastSanctionDateUtc = null
                };

            ReportRepository repository = CreateRepository();

            Action action = () => repository.ReporterHasActiveReport(criteria);

            bool throws =
                Assert.Throws<ArgumentOutOfRangeException>(action) != null;

            Assert.True(throws);
        }

        [Theory]
        [InlineData(INVALID_USER_ID_ZERO)]
        [InlineData(INVALID_USER_ID_NEGATIVE)]
        public void TestReporterHasActiveReportWhenReporterUserIdNotPositiveThrowsArgumentOutOfRange(
            int invalidReporterUserId)
        {
            int reportedUserId = CreateUser();

            ActiveReportSearchCriteriaDto criteria =
                new ActiveReportSearchCriteriaDto
                {
                    ReportedUserId = reportedUserId,
                    ReporterUserId = invalidReporterUserId,
                    LastSanctionDateUtc = null
                };

            ReportRepository repository = CreateRepository();

            Action action = () => repository.ReporterHasActiveReport(criteria);

            bool throws =
                Assert.Throws<ArgumentOutOfRangeException>(action) != null;

            Assert.True(throws);
        }

        [Fact]
        public void TestReporterHasActiveReportWhenNoReportsReturnsFalse()
        {
            int reportedUserId = CreateUser("Reported");
            int reporterUserId = CreateUser("Reporter");

            ActiveReportSearchCriteriaDto criteria =
                new ActiveReportSearchCriteriaDto
                {
                    ReportedUserId = reportedUserId,
                    ReporterUserId = reporterUserId,
                    LastSanctionDateUtc = null
                };

            ReportRepository repository = CreateRepository();

            bool hasActive = repository.ReporterHasActiveReport(criteria);

            bool isOk = hasActive == false;
            Assert.True(isOk);
        }

        [Fact]
        public void TestReporterHasActiveReportWhenReportExistsAndNoLastSanctionReturnsTrue()
        {
            int reportedUserId = CreateUser("Reported");
            int reporterUserId = CreateUser("Reporter");

            ReportRepository repository = CreateRepository();

            ReportDto dto = new ReportDto
            {
                ReportReason = "Lenguaje ofensivo",
                ReportedUserId = reportedUserId,
                ReporterUserId = reporterUserId
            };

            repository.InsertReport(dto);

            ActiveReportSearchCriteriaDto criteria =
                new ActiveReportSearchCriteriaDto
                {
                    ReportedUserId = reportedUserId,
                    ReporterUserId = reporterUserId,
                    LastSanctionDateUtc = null
                };

            bool hasActive = repository.ReporterHasActiveReport(criteria);

            bool isOk = hasActive;
            Assert.True(isOk);
        }

        [Fact]
        public void TestReporterHasActiveReportWhenLastSanctionDateInFutureReturnsFalse()
        {
            int reportedUserId = CreateUser("Reported");
            int reporterUserId = CreateUser("Reporter");

            ReportRepository repository = CreateRepository();

            ReportDto dto = new ReportDto
            {
                ReportReason = "Spam en chat",
                ReportedUserId = reportedUserId,
                ReporterUserId = reporterUserId
            };

            repository.InsertReport(dto);

            ActiveReportSearchCriteriaDto criteria =
                new ActiveReportSearchCriteriaDto
                {
                    ReportedUserId = reportedUserId,
                    ReporterUserId = reporterUserId,
                    LastSanctionDateUtc = DateTime.UtcNow.AddDays(1)
                };

            bool hasActive = repository.ReporterHasActiveReport(criteria);

            bool isOk = hasActive == false;
            Assert.True(isOk);
        }


        [Theory]
        [InlineData(INVALID_USER_ID_ZERO)]
        [InlineData(INVALID_USER_ID_NEGATIVE)]
        public void TestCountActiveReportsAgainstUserWhenReportedUserIdNotPositiveThrowsArgumentOutOfRange(
            int invalidReportedUserId)
        {
            ReportRepository repository = CreateRepository();

            Action action =
                () => repository.CountActiveReportsAgainstUser(
                    invalidReportedUserId,
                    null);

            bool throws =
                Assert.Throws<ArgumentOutOfRangeException>(action) != null;

            Assert.True(throws);
        }

        [Fact]
        public void TestCountActiveReportsAgainstUserWhenNoReportsReturnsZero()
        {
            int reportedUserId = CreateUser("Reported");

            ReportRepository repository = CreateRepository();

            int count = repository.CountActiveReportsAgainstUser(
                reportedUserId,
                null);

            bool isOk = count == 0;
            Assert.True(isOk);
        }

        [Fact]
        public void TestCountActiveReportsAgainstUserCountsAllWhenNoLastSanctionDate()
        {
            int reportedUserId = CreateUser("Reported");
            int reporter1 = CreateUser("Reporter1");
            int reporter2 = CreateUser("Reporter2");
            int reporter3 = CreateUser("Reporter3");

            ReportRepository repository = CreateRepository();

            var reports = new List<ReportDto>
            {
                new ReportDto
                {
                    ReportReason = "Motivo 1",
                    ReportedUserId = reportedUserId,
                    ReporterUserId = reporter1
                },
                new ReportDto
                {
                    ReportReason = "Motivo 2",
                    ReportedUserId = reportedUserId,
                    ReporterUserId = reporter2
                },
                new ReportDto
                {
                    ReportReason = "Motivo 3",
                    ReportedUserId = reportedUserId,
                    ReporterUserId = reporter3
                }
            };

            foreach (ReportDto dto in reports)
            {
                repository.InsertReport(dto);
            }

            int count = repository.CountActiveReportsAgainstUser(
                reportedUserId,
                null);

            bool isOk = count == reports.Count;
            Assert.True(isOk);
        }

        [Fact]
        public void TestCountActiveReportsAgainstUserWhenLastSanctionDateInFutureReturnsZero()
        {
            int reportedUserId = CreateUser("Reported");
            int reporter = CreateUser("Reporter");

            ReportRepository repository = CreateRepository();

            ReportDto dto = new ReportDto
            {
                ReportReason = "Motivo X",
                ReportedUserId = reportedUserId,
                ReporterUserId = reporter
            };

            repository.InsertReport(dto);

            int count = repository.CountActiveReportsAgainstUser(
                reportedUserId,
                DateTime.UtcNow.AddDays(1));

            bool isOk = count == 0;
            Assert.True(isOk);
        }
    }
}
