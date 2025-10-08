using System;
using System.ServiceModel;
using SnakeAndLadders.Host.Services;

class Program
{
    public Program()
    {
    }

    static void Main()
    {
        using (var host = new ServiceHost(typeof(UserService)))
        {
            Console.WriteLine(typeof(UserService).FullName);

            host.Open();
            Console.WriteLine("Servidor corriendo...");
            Console.ReadLine();
            host.Close();
        }
    }
}
