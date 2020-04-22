using NUnit.Framework;
using System;

namespace Azure.Data.Tests
{
    public class ContractTests
    {
        [Test]
        public void DynamicDataCycle()
        {
            dynamic a = DynamicData.Create();
            dynamic b = DynamicData.Create();
            a.B = b;
            b.A = a;
        }

        [Test]
        public void ComplexObjectCycle()
        {
            // graph with a cycle
            var a = new Foo();
            var b = new Foo();
            a.Other = b;
            b.Other = a;

            dynamic d = DynamicData.Create();
            d.Property = a;
        }

        class Foo
        {
            public Foo Other { get; set; }
        }
    }
}