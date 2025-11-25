using System;
using System.Transactions;
using SnakesAndLadders.Data;

namespace SnakesAndLadders.Tests.integration
{
    public abstract class IntegrationTestBase : IDisposable
    {
        private readonly TransactionScope _transactionScope;

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
            return new SnakeAndLaddersDBEntities1(CadenaConexionPruebas);
        }
    }
}