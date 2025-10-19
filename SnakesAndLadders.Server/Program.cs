using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Repository;
using SnakeAndLadders.Host.Services;
using SnakesAndLadders.Host.Services;
using System;
using System.Configuration;
using System.IO;
using System.Linq;              // Para OfType(), FirstOrDefault()
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


class Program
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(Program));

    private static void Main()
    {
        
        ServerLogBootstrap.Init();
        XmlConfigurator.Configure(LogManager.GetRepository());

        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            Log.Fatal("Excepción no controlada.", e.ExceptionObject as Exception);

        ServiceHost userHost = null;
        ServiceHost authHost = null;

        try
        {
            Log.Info("Iniciando el servidor…");

            userHost = new ServiceHost(typeof(UserService));
            authHost = new ServiceHost(typeof(AuthService));

            userHost.Open();
            authHost.Open();

            Log.Info("Servidor iniciado y servicios levantados.");
            Console.WriteLine("Servicios levantados:");
            Console.WriteLine($" - {typeof(UserService).FullName}");
            Console.WriteLine($" - {typeof(AuthService).FullName}");
            Console.WriteLine("Presiona Enter para detener…");
            Console.ReadLine();
        }
        
        catch (AddressAccessDeniedException ex)
        {
            Log.Error("Acceso denegado al abrir puertos HTTP/NET.TCP. Ejecuta como admin o cambia puertos.", ex);
        }
        catch (AddressAlreadyInUseException ex)
        {
            Log.Error("Puerto/URL en uso (¿8085/8095 ocupados?). Cambia baseAddress o cierra quien lo use.", ex);
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
            Log.Error("Error en App.config (secciones duplicadas, bindings, endpoints).", ex);
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
                Log.Warn($"{name} en estado Faulted. Abortando…");
                host.Abort();
            }
            else
            {
                host.Close();
            }
        }
        catch (Exception ex)
        {
            Log.Warn($"Error al cerrar {name}. Se aborta.", ex);
            host.Abort();
        }
    }
}
