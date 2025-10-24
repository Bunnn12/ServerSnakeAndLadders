using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SnakesAndLadders.Data.Repositories;

namespace SnakesAndLadders.Tests
{
    [TestClass]
    public sealed class UserRepositoryTests
    {
        [TestMethod]
        public void GetByUsernameWhenNullThrowsArgumentException()
        {
            
            var repo = new UserRepository();
            try
            {
                repo.GetByUsername(null);
                Assert.Fail("Expected exception was not thrown.");
            }
            catch (ArgumentException ex) 
            {
                StringAssert.Contains(ex.Message, "username");
                Assert.AreEqual("username", ex.ParamName);
            }
        }
    }
}
