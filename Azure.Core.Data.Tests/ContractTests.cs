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

            // TODO: should this throw?
            Assert.Throws<InvalidOperationException>(() => {
                d.Property = a;
            });
        }

        [Test]
        public void PrimitiveArray()
        {
            dynamic a = DynamicData.Create();
            a.Items = new int[] { 1, 2, 3 };
        }

        [Test]
        public void ComplexArray()
        {
            dynamic a = DynamicData.Create();
            a.Items = new Foo[] { new Foo() };
        }

        class Foo
        {
            public Foo Other { get; set; }
        }
    }
}