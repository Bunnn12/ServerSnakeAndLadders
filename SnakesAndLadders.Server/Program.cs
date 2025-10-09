using System;
using System.ServiceModel;
using SnakeAndLadders.Host.Services;

class Program
{
    static void Main()
    {
        using (var userHost = new ServiceHost(typeof(UserService)))
        using (var authHost = new ServiceHost(typeof(AuthService)))
        {
            userHost.Open();
            authHost.Open();

            Console.WriteLine("Servicios levantados:");
            Console.WriteLine($" - {typeof(UserService).FullName}");
            Console.WriteLine($" - {typeof(AuthService).FullName}");
            Console.WriteLine("Presiona Enter para detener...");
            Console.ReadLine();

            authHost.Close();
            userHost.Close();
        }
    }
}
