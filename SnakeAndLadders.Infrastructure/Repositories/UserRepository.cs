using ServerSnakesAndLadders;

namespace SnakeAndLadders.Infrastructure.Repositories
{
    public class UserRepository
    {
        public int AddUser(string username, string nombre, string apellidos)
        {
            using (var db = new SnakeAndLaddersDBEntities1())
            {
                var nuevo = new Usuario
                {
                    NombreUsuario = username,
                    Nombre = nombre,
                    Apellidos = apellidos,
                    Monedas = 0,
                    Estado = new byte[] { 1 }
                };
                db.Usuario.Add(nuevo);
                db.SaveChanges();
                return nuevo.IdUsuario;
            }
        }
    }
}
