using log4net;
using log4net.Config;
using ServerSnakesAndLadders;
using SnakeAndLadders.Contracts.Interfaces;
using SnakesAndLadders.Data;
using SnakesAndLadders.Data.Repositories;
using SnakesAndLadders.Host.Helpers;
using SnakesAndLadders.Server.Helpers;
using SnakesAndLadders.Services;
using SnakesAndLadders.Services.Logic;
using SnakesAndLadders.Services.Logic.Auth;
using SnakesAndLadders.Services.Logic.Gameboard;
using SnakesAndLadders.Services.Wcf;
using SnakesAndLadders.Services.Wcf.Gameplay.SnakesAndLadders.Services.Logic.Gameplay;
using SnakesAndLadders.Services.Wcf.Lobby;
using System;
using System.Configuration;
using System.IO;
using System.ServiceModel;

internal static class ServerLogBootstrap
{
    public static void Init()
    {
        string baseDir = Environment.GetFolderPath(
            Environment.SpecialFolder.CommonApplicationData);

        string logsDir = Path.Combine(baseDir, "SnakeAndLadders", "logs");

        Directory.CreateDirectory(logsDir);

        GlobalContext.Properties["LogFileName"] =
            Path.Combine(logsDir, "server.log");
    }
}

internal static class Program
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(Program));

    private static void Main()
    {
        ServerLogBootstrap.Init();
        XmlConfigurator.Configure(LogManager.GetRepository());

        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            Log.Fatal("Excepción no controlada.", args.ExceptionObject as Exception);

        ServiceHost authHost = null;
        ServiceHost userHost = null;
        ServiceHost lobbyHost = null;
        ServiceHost chatHost = null;
        ServiceHost gameBoardHost = null;
        ServiceHost playerReportHost = null;
        ServiceHost statsHost = null;
        ServiceHost friendsHost = null;
        ServiceHost gameplayHost = null;
        ServiceHost shopHost = null;
        ServiceHost inventoryHost = null;
        ServiceHost matchInvitationHost = null;
        ServiceHost socialProfileHost = null;

        try
        {
            Log.Info("Iniciando el servidor…");

            string chatPath = Environment.ExpandEnvironmentVariables(
                ConfigurationManager.AppSettings["ChatFilePath"] ??
                @"%LOCALAPPDATA%\SnakesAndLadders\Chat\");

            // Infraestructura base
            var tokenService = new TokenService();
            var verificationCodeStore = new VerificationCodeStore();

            IAccountsRepository accountsRepository = new AccountsRepository();
            IUserRepository userRepository = new UserRepository();
            ILobbyRepository lobbyRepository = new LobbyRepository();
            IFriendsRepository friendsRepository = new FriendsRepository();
            IChatRepository chatRepository = new FileChatRepository(chatPath);
            IReportRepository reportRepository = new ReportRepository();
            ISanctionRepository sanctionRepository = new SanctionRepository();
            IAccountStatusRepository accountStatusRepository =
                new AccountStatusRepository();
            IStatsRepository statsRepository = new StatsRepository();
            IShopRepository shopRepository = new ShopRepository();
            IInventoryRepository inventoryRepository = new InventoryRepository();
            ISocialProfileRepository socialProfileRepository =
                new SocialProfileRepository();
            IGameResultsRepository gameResultsRepository =
                new GameResultsRepository();

            IPasswordHasher passwordHasher = new Sha256PasswordHasher();
            IEmailSender emailSender = new SmtpEmailSender();
            IAppLogger appLogger = new AppLogger(Log);

            // App services base
            var chatAppService = new ChatAppService(chatRepository);

            // Infraestructura de lobby en memoria
            ILobbyStore lobbyStore = new InMemoryLobbyStore();
            ILobbyNotification lobbyNotificationHub =
                new LobbyNotification(lobbyStore);
            ILobbyIdGenerator lobbyIdGenerator = new LobbyIdGenerator();

            var lobbyAppService = new LobbyAppService(lobbyRepository, appLogger);
            var lobbySvc = new LobbyService(
                lobbyAppService,
                lobbyStore,
                lobbyNotificationHub,
                lobbyIdGenerator);

            var playerSessionManager = new PlayerSessionManager(lobbySvc);
            var playerReportAppService = new PlayerReportAppService(
                reportRepository,
                sanctionRepository,
                accountStatusRepository,
                playerSessionManager);



            // Servicios de autenticación (lógica separada)
            IRegistrationAuthService registrationAuthService =
                new RegistrationAuthService(accountsRepository, passwordHasher);

            ILoginAuthService loginAuthService =
                new LoginAuthService(
                    accountsRepository,
                    passwordHasher,
                    playerReportAppService,
                    userRepository,
                    tokenService);

            IVerificationAuthService verificationAuthService =
                new VerificationAuthService(
                    accountsRepository,
                    emailSender,
                    verificationCodeStore);

            IPasswordChangeAuthService passwordChangeAuthService =
                new PasswordChangeAuthService(
                    accountsRepository,
                    passwordHasher,
                    verificationCodeStore);

            IAuthAppService authApp = new AuthAppService(
                registrationAuthService,
                loginAuthService,
                verificationAuthService,
                passwordChangeAuthService,
                tokenService);

            Func<string, int> getUserId = token => authApp.GetUserIdFromToken(token);

            var userAppService = new UserAppService(
                userRepository,
                accountStatusRepository);

            IStatsAppService statsAppService =
                new StatsAppService(statsRepository);

            var friendsAppService = new FriendsAppService(
                friendsRepository,
                getUserId);

            var shopAppService = new ShopAppService(shopRepository, getUserId);
            var inventoryAppService = new InventoryAppService(inventoryRepository);

            var matchInvitationAppService = new MatchInvitationAppService(
                friendsRepository,
                userRepository,
                accountsRepository,
                emailSender,
                getUserId);

            ISocialProfileAppService socialProfileAppService =
                new SocialProfileAppService(
                    socialProfileRepository,
                    userRepository);

            IGameSessionStore gameSessionStore = new InMemoryGameSessionStore();

            var boardLayoutBuilder = new BoardLayoutBuilder();
            var specialCellsAssigner = new SpecialCellsAssigner();
            var snakesAndLaddersPlacer = new SnakesAndLaddersPlacer();

            var gameBoardBuilder = new GameBoardBuilder(
                boardLayoutBuilder,
                specialCellsAssigner,
                snakesAndLaddersPlacer);
            var gameplayAppService = new GameplayAppService(gameSessionStore);

            // Servicios WCF
            var authSvc = new AuthService(authApp);
            var userSvc = new UserService(userAppService);
            var chatSvc = new ChatService(chatAppService);
            var gameBoardSvc = new GameBoardService(
                gameSessionStore,
                gameBoardBuilder);
            var playerReportSvc = new PlayerReportService(playerReportAppService);
            var statsSvc = new StatsService(statsAppService);
            var friendsSvc = new FriendsService(friendsAppService);
            var gameplaySvc = new GameplayService(
                gameSessionStore,
                inventoryRepository,
                gameResultsRepository,
                gameplayAppService,
                appLogger);
            var shopSvc = new ShopService(shopAppService);
            var inventorySvc = new InventoryService(inventoryAppService);
            var matchInvitationSvc =
                new MatchInvitationService(matchInvitationAppService);
            var socialProfileSvc =
                new SocialProfileService(socialProfileAppService);

            // Hosts
            lobbyHost = new ServiceHost(lobbySvc);
            authHost = new ServiceHost(authSvc);
            userHost = new ServiceHost(userSvc);
            chatHost = new ServiceHost(chatSvc);
            gameBoardHost = new ServiceHost(gameBoardSvc);
            playerReportHost = new ServiceHost(playerReportSvc);
            statsHost = new ServiceHost(statsSvc);
            friendsHost = new ServiceHost(friendsSvc);
            gameplayHost = new ServiceHost(gameplaySvc);
            shopHost = new ServiceHost(shopSvc);
            inventoryHost = new ServiceHost(inventorySvc);
            matchInvitationHost = new ServiceHost(matchInvitationSvc);
            socialProfileHost = new ServiceHost(socialProfileSvc);

            authHost.Open();
            userHost.Open();
            lobbyHost.Open();
            chatHost.Open();
            gameBoardHost.Open();
            playerReportHost.Open();
            statsHost.Open();
            friendsHost.Open();
            gameplayHost.Open();
            shopHost.Open();
            inventoryHost.Open();
            matchInvitationHost.Open();
            socialProfileHost.Open();

            Log.Info("Servidor iniciado y servicios levantados.");

            Console.WriteLine("Servicios levantados:");
            Console.WriteLine(" - " + typeof(AuthService).FullName);
            Console.WriteLine(" - " + typeof(UserService).FullName);
            Console.WriteLine(" - " + typeof(LobbyService).FullName);
            Console.WriteLine(" - " + typeof(ChatService).FullName);
            Console.WriteLine(" - " + typeof(GameBoardService).FullName);
            Console.WriteLine(" - " + typeof(PlayerReportService).FullName);
            Console.WriteLine(" - " + typeof(StatsService).FullName);
            Console.WriteLine(" - " + typeof(FriendsService).FullName);
            Console.WriteLine(" - " + typeof(GameplayService).FullName);
            Console.WriteLine(" - " + typeof(ShopService).FullName);
            Console.WriteLine(" - " + typeof(InventoryService).FullName);
            Console.WriteLine(" - " + typeof(MatchInvitationService).FullName);
            Console.WriteLine(" - " + typeof(SocialProfileService).FullName);
            Console.WriteLine("Presiona Enter para detener…");
            Console.ReadLine();
        }
        catch (AddressAccessDeniedException ex)
        {
            Log.Error(
                "Acceso denegado al abrir puertos HTTP/NET.TCP. " +
                "Ejecuta como admin o cambia puertos.",
                ex);

            Console.WriteLine("\nPresiona Enter para cerrar…");
            Console.ReadLine();
        }
        catch (AddressAlreadyInUseException ex)
        {
            Log.Error(
                "Puerto/URL en uso. Cambia baseAddress o libera el puerto.",
                ex);

            Console.WriteLine("\nPresiona Enter para cerrar…");
            Console.ReadLine();
        }
        catch (CommunicationException ex)
        {
            Log.Error("Fallo de comunicación al abrir los hosts WCF.", ex);
            Console.WriteLine("\nPresiona Enter para cerrar…");
            Console.ReadLine();
        }
        catch (TimeoutException ex)
        {
            Log.Error("Timeout al abrir los hosts WCF.", ex);
            Console.WriteLine("\nPresiona Enter para cerrar…");
            Console.ReadLine();
        }
        catch (ConfigurationErrorsException ex)
        {
            Log.Error("Error en App.config (secciones, bindings, endpoints).", ex);
            Console.WriteLine("\nPresiona Enter para cerrar…");
            Console.ReadLine();
        }
        catch (Exception ex)
        {
            Log.Error("Error inesperado al iniciar el servidor.", ex);
            Console.WriteLine("\nPresiona Enter para cerrar…");
            Console.ReadLine();
        }
        finally
        {
            CloseSafely(lobbyHost, "LobbyService");
            CloseSafely(authHost, "AuthService");
            CloseSafely(userHost, "UserService");
            CloseSafely(chatHost, "ChatService");
            CloseSafely(gameBoardHost, "GameBoardService");
            CloseSafely(playerReportHost, "PlayerReportService");
            CloseSafely(statsHost, "StatsService");
            CloseSafely(friendsHost, "FriendsService");
            CloseSafely(gameplayHost, "GameplayService");
            CloseSafely(shopHost, "ShopService");
            CloseSafely(inventoryHost, "InventoryService");
            CloseSafely(matchInvitationHost, "MatchInvitationService");
            CloseSafely(socialProfileHost, "SocialProfileService");

            Log.Info("Servidor detenido.");
        }
    }

    private static void CloseSafely(ServiceHost host, string name)
    {
        if (host == null)
        {
            return;
        }

        try
        {
            if (host.State == CommunicationState.Faulted)
            {
                Log.Warn(name + " en estado Faulted. Abortando…");
                host.Abort();
            }
            else
            {
                host.Close();
            }
        }
        catch (Exception ex)
        {
            Log.Warn("Error al cerrar " + name + ". Se aborta.", ex);
            host.Abort();
        }
    }
}
