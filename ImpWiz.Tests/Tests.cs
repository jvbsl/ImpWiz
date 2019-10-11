using System;
using Xunit;

namespace ImpWiz.Tests
{
    public class Tests
    {
        [Fact]
        public void CanCallFunction()
        {
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