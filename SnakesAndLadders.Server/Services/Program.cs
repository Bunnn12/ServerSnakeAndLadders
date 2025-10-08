using System;
using System.ServiceModel;
using SnakeAndLadders.Host.Services;
using SnakesAndLadders.Server.Services;

namespace SnakesAndLadders.Host
{
    internal static class Program
    {
        private static void Main()
        {
            using (var userHost = new ServiceHost(typeof(UserService)))
            using (var authHost = new ServiceHost(typeof(AuthService)))
            {
                userHost.Open();
                authHost.Open();

                Console.WriteLine("Services online:");
                foreach (var ep in userHost.Description.Endpoints)
                    Console.WriteLine($" - {ep.Contract.Name} @ {ep.Address.Uri}");
                foreach (var ep in authHost.Description.Endpoints)
                    Console.WriteLine($" - {ep.Contract.Name} @ {ep.Address.Uri}");

                Console.WriteLine("Press ENTER to stop...");
                Console.ReadLine();

                authHost.Close();
                userHost.Close();
            }
        }
    }
}
