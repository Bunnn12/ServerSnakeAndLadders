using log4net;
using log4net.Config;
using ServerSnakesAndLadders;
using SnakeAndLadders.Contracts.Interfaces;
using SnakesAndLadders.Data;
using SnakesAndLadders.Data.Repositories;
using SnakesAndLadders.Host.Helpers;
using SnakesAndLadders.Services.Logic;
using SnakesAndLadders.Services.Wcf;
using System;
using System.Configuration;
using System.IO;
using System.ServiceModel;

internal static class ServerLogBootstrap
{
    public static void Init()
    {
        var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var logsDir = Path.Combine(baseDir, "SnakeAndLadders", "logs");
        Directory.CreateDirectory(logsDir);
        log4net.GlobalContext.Properties["LogFileName"] = Path.Combine(logsDir, "server.log");
    }
}


internal static class Program
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(Program));

    private static void Main()
    {
        ServerLogBootstrap.Init();
        XmlConfigurator.Configure(LogManager.GetRepository());

        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            Log.Fatal("Excepción no controlada.", e.ExceptionObject as Exception);

        ServiceHost authHost = null;
        ServiceHost userHost = null;
        ServiceHost lobbyHost = null;
        ServiceHost chatHost = null;
        ServiceHost gameBoardHost = null;
        ServiceHost playerReportHost = null;
        try
        {
            Log.Info("Iniciando el servidor…");

            var chatPath = Environment.ExpandEnvironmentVariables(
                ConfigurationManager.AppSettings["ChatFilePath"] ??
                @"%LOCALAPPDATA%\SnakesAndLadders\Chat\");

            var accountsRepo = new AccountsRepository();
            var userRepo = new UserRepository();
            var lobbyRepo = new LobbyRepository();
            IChatRepository chatRepo = new FileChatRepository(chatPath);

            var chatApp = new ChatAppService(chatRepo);

            var reportRepo = new ReportRepository();
            var sanctionRepo = new SanctionRepository();

            IPasswordHasher hasher = new Sha256PasswordHasher();
            IEmailSender email = new SmtpEmailSender();
            IAppLogger appLogger = new AppLogger(Log);

            var authApp = new AuthAppService(accountsRepo, hasher, email);
            var userApp = new UserAppService(userRepo);
            var lobbyApp = new LobbyAppService(lobbyRepo, appLogger);
            var playerReportApp = new PlayerReportAppService(reportRepo, sanctionRepo);

            var authSvc = new AuthService(authApp);
            var userSvc = new UserService(userApp);
            var lobbySvc = new LobbyService();
            var chatSvc = new ChatService(chatApp);
            var gameBoardSvc = new GameBoardService();
            var playerReportSvc = new PlayerReportService(playerReportApp);

            authHost = new ServiceHost(authSvc);
            userHost = new ServiceHost(userSvc);
            lobbyHost = new ServiceHost(lobbySvc);
            chatHost = new ServiceHost(chatSvc);
            gameBoardHost = new ServiceHost(gameBoardSvc);
            playerReportHost = new ServiceHost(playerReportSvc);

            authHost.Open();
            userHost.Open();
            lobbyHost.Open();
            chatHost.Open();
            gameBoardHost.Open();
            playerReportHost.Open();

            Log.Info("Servidor iniciado y servicios levantados.");
            Console.WriteLine("Servicios levantados:");
            Console.WriteLine(" - " + typeof(AuthService).FullName);
            Console.WriteLine(" - " + typeof(UserService).FullName);
            Console.WriteLine(" - " + typeof(LobbyService).FullName);
            Console.WriteLine(" - " + typeof(ChatService).FullName);
            Console.WriteLine(" - " + typeof(GameBoardService).FullName);
            Console.WriteLine(" - " + typeof(PlayerReportService).FullName);
            Console.WriteLine("Presiona Enter para detener…");
            Console.ReadLine();
        }
        catch (AddressAccessDeniedException ex)
        {
            Log.Error("Acceso denegado al abrir puertos HTTP/NET.TCP. Ejecuta como admin o cambia puertos.", ex);
            Console.WriteLine("\nPresiona Enter para cerrar…");
            Console.ReadLine();
        }
        catch (AddressAlreadyInUseException ex)
        {
            Log.Error("Puerto/URL en uso. Cambia baseAddress o libera el puerto.", ex);
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
