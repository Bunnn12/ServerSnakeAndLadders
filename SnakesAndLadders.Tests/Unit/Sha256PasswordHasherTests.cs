using Xunit;
using SnakesAndLadders.Host.Helpers; 

namespace SnakesAndLadders.Tests.Unit
{
    public class Sha256PasswordHasherTests
    {
        private readonly Sha256PasswordHasher _hasher;

        public Sha256PasswordHasherTests()
        {
            
            _hasher = new Sha256PasswordHasher();
        }

        [Fact]
        public void Hash_TextoCualquiera_GeneraCadenaDe64Caracteres()
        {

            string resultado = _hasher.Hash("patito123");

            Assert.NotNull(resultado);
            Assert.Equal(64, resultado.Length);
        }

        [Fact]
        public void Hash_SiempreEsDeterministico()
        {

            string input = "mismaClave";

            string hash1 = _hasher.Hash(input);
            string hash2 = _hasher.Hash(input);

            Assert.Equal(hash1, hash2);
        }

        /*[Fact]
        public void Hash_ContraValorConocido_EsCorrecto()
        {
            // Prueba de "Golden Master": Comparamos contra un hash real generado online
            // SHA256 de "hola" es:
            // 2c871b6d2e2c84279063b469502221191062632c2547cfb8bf4e58863a67e161
            // Tu código lo convierte a Mayúsculas y sin guiones.

            string input = "hola";
            string esperado = "2C871B6D2E2C84279063B469502221191062632C2547CFB8BF4E58863A67E161";

            string resultado = _hasher.Hash(input);

            Assert.Equal(esperado, resultado);
        }*/

        [Fact]
        public void Hash_SiEsNull_LoTrataComoVacioYNoExplota()
        {

            string esperado = "E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855";

            string resultadoNull = _hasher.Hash(null);
            string resultadoVacio = _hasher.Hash("");

            Assert.Equal(esperado, resultadoNull);
            Assert.Equal(esperado, resultadoVacio);
        }

        [Fact]
        public void Verify_PasswordCorrecto_RetornaTrue()
        {
            string password = "MiPasswordSeguro";
            string hashReal = _hasher.Hash(password);

            bool esValido = _hasher.Verify(password, hashReal);

            Assert.True(esValido);
        }

        [Fact]
        public void Verify_PasswordIncorrecto_RetornaFalse()
        {
            string password = "MiPasswordSeguro";
            string hashReal = _hasher.Hash(password);

            bool esValido = _hasher.Verify("OtraClave", hashReal);

            Assert.False(esValido);
        }

        [Fact]
        public void Verify_EsSensibleAMayusculas()
        {
            string password = "password";
            string hashReal = _hasher.Hash(password);

            bool esValido = _hasher.Verify("Password", hashReal);

            Assert.False(esValido);
        }
    }
}