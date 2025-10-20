using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.ServiceModel;
using log4net;
using log4net.Config;
using ServerSnakesAndLadders;
using SnakeAndLadders.Contracts;
using SnakesAndLadders.Contracts.Interfaces;
using SnakesAndLadders.Data.Repositories;
using SnakesAndLadders.Host.Helpers;
using SnakesAndLadders.Services.Logic;
using SnakesAndLadders.Services.Wcf;


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
        // Logging
        ServerLogBootstrap.Init();
        XmlConfigurator.Configure(LogManager.GetRepository());

        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            Log.Fatal("Excepción no controlada.", e.ExceptionObject as Exception);

        ServiceHost userHost = null;
        ServiceHost authHost = null;

        try
        {
            Log.Info("Iniciando el servidor…");

            // 1) Infra/Repos (Data)
            var accountsRepo = new AccountsRepository();
            var userRepo = new UserRepository();

            // 2) Helpers (Host)
            IPasswordHasher hasher = new Sha256PasswordHasher();
            IEmailSender email = new SmtpEmailSender();

            // 3) Lógica de aplicación (Services.Logic)
            var authApp = new AuthAppService(accountsRepo, hasher, email);
            var userApp = new UserAppService(userRepo);

            // 4) Adaptadores WCF (Services.Wcf) — hostear por instancia
            var authSvc = new AuthService(authApp);
            var userSvc = new UserService(userApp);

            authHost = new ServiceHost(authSvc);
            userHost = new ServiceHost(userSvc);

            authHost.Open();
            userHost.Open();

            Log.Info("Servidor iniciado y servicios levantados.");
            Console.WriteLine("Servicios levantados:");
            Console.WriteLine(" - " + typeof(AuthService).FullName);
            Console.WriteLine(" - " + typeof(UserService).FullName);
            Console.WriteLine("Presiona Enter para detener…");
            Console.ReadLine();
        }
        catch (AddressAccessDeniedException ex)
        {
            Log.Error("Acceso denegado al abrir puertos HTTP/NET.TCP. Ejecuta como admin o cambia puertos.", ex);
        }
        catch (AddressAlreadyInUseException ex)
        {
            Log.Error("Puerto/URL en uso. Cambia baseAddress o libera el puerto.", ex);
        }
        catch (CommunicationException ex)
        {
            Log.Error("Fallo de comunicación al abrir los hosts WCF.", ex);
        }
        catch (TimeoutException ex)
        {
            Log.Error("Timeout al abrir los hosts WCF.", ex);
        }
        catch (ConfigurationErrorsException ex)
        {
            Log.Error("Error en App.config (secciones, bindings, endpoints).", ex);
        }
        catch (Exception ex)
        {
            Log.Error("Error inesperado al iniciar el servidor.", ex);
        }
        finally
        {
            CloseSafely(authHost, "AuthService");
            CloseSafely(userHost, "UserService");
            Log.Info("Servidor detenido.");
        }
    }

    private static void CloseSafely(ServiceHost host, string name)
    {
        if (host == null) return;

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
