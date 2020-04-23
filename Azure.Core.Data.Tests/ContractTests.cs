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
            a.FooProperty = b;
            b.FooProperty = a;

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

        [Test]
        public void ArrayOfArrays()
        {
            var data = "[[{\"Foo\":10 },{\"Foo\":20}]]";
            dynamic array = DynamicData.CreateFromJson(data);
            var arrayOfFoo = array[0];
            var foo = arrayOfFoo[0];
            Assert.AreEqual(10, foo.Foo);
        }

        class Foo
        {
            public Foo FooProperty { get; set; }
        }
    }
}