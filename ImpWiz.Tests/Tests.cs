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
        public void CanGetSimpleInt32()
        {
            Assert.Equal(11, Example.Example.GetInt32());
        }
        
        [Fact]
        public void CanPassMultipleInt32()
        {
            Assert.Equal(24, Example.Example.Add(11, 13));
        }
        
        [Fact]
        public void CanMarshalStringParameter()
        {
            Assert.Equal(1234, Example.Example.ParseInt32("1234"));
        }
        
        [Fact]
        public void CanMarshalStringReturn()
        {
            Assert.Equal("ASCII Test String", Example.Example.GetLPSTR());
        }
        
        [Fact]
        public void CanMarshalMultiStringParameters()
        {
            string a = "ASCII";
            string b = " Test String";
            Assert.Equal(a + b, Example.Example.Combine(a, b));
        }


        [Fact]
        public void CanCallFunction()
        {
            var assembly = typeof(Example.Example).Assembly;
            var id = assembly.Modules.First().ModuleVersionId;
            var ptr = Example.Example.GetLPSTR();
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