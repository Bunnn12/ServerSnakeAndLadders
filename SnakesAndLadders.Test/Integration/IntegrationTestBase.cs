using System;
using System.Transactions;
using SnakesAndLadders.Data;

namespace SnakesAndLadders.Test.Integration
{
    public abstract class IntegrationTestBase : IDisposable
    {
        private readonly TransactionScope _transactionScope;

        // AQUÍ PONEMOS LA CADENA DIRECTAMENTE (Sin depender de App.config)
        // Asegúrate de poner tu contraseña real donde dice PASSWORD_AQUI
        private const string CadenaConexionPruebas =
            "metadata=res://*/DataBase.csdl|res://*/DataBase.ssdl|res://*/DataBase.msl;provider=System.Data.SqlClient;provider connection string=\"data source=laptopirene\\sqlexpress;initial catalog=TestSnakeAndLaddersDB;user id=UsuarioProyecto1;password=C0ntraseñaFuerte!2025;multipleactiveresultsets=True;encrypt=True;trustservercertificate=True;application name=EntityFramework\"";

        public IntegrationTestBase()
        {
            _transactionScope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
        }

        public void Dispose()
        {
            _transactionScope.Dispose();
        }

        protected SnakeAndLaddersDBEntities1 CreateContext()
        {
            // Usamos el constructor que acabamos de crear en el Paso 1
            return new SnakeAndLaddersDBEntities1(CadenaConexionPruebas);
        }
    }
}