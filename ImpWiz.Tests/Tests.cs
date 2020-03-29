using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Xunit;

namespace ImpWiz.Tests
{
    public class Tests : TestBase
    {

        [Fact]
        public void CanCallFunction()
        {
            var assembly = typeof(Example.Example).Assembly;
            var id = assembly.Modules.First().ModuleVersionId;
            var ptr = Example.Example.GetError();
        }

        [Fact]
        public void ThrowsDllNotFoundException()
        {
            try
            {
                Example.Example.UnavailableDll();
            }
            catch (DllNotFoundException)
            {
                return;
            }

            Assert.True(false);
        }
        
        
        [Fact]
        public void ThrowsSymbolNotFoundException()
        {
            try
            {
                Example.Example.UnavailableSymbol();
            }
            catch (EntryPointNotFoundException)
            {
                return;
            }

            Assert.True(false);
        }


    }
}