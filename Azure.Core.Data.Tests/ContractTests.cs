using NUnit.Framework;
using System;
using System.Collections;
using System.Dynamic;

namespace Azure.Data.Tests
{
    public class ContractTests
    {
        static IEnumerable Models {
            get {
                yield return new Func<IDynamicMetaObjectProvider>(() => { return new Data(); });
            }
        }

        [TestCaseSource(typeof(ContractTests), "Models")]
        public void SetProperties(Func<IDynamicMetaObjectProvider> dynamicObjectFactory)
        {
            dynamic d = dynamicObjectFactory();
            d.PString = "hello world";
            d.PInt32 = 32;

            Assert.AreEqual(32, d.PInt32);
            Assert.AreEqual("hello world", d.PString);
        }

        [Test]
        public void Cycle()
        {
            dynamic a = new Data();
            dynamic b = new Data();
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

            dynamic d = new Data();

            // TODO: should this throw?
            Assert.Throws<InvalidOperationException>(() => {
                d.Property = a;
            });
        }

        [Test]
        public void PrimitiveArray()
        {
            dynamic a = new Data();
            a.Items = new int[] { 1, 2, 3 };
        }

        [Test]
        public void ComplexArray()
        {
            dynamic a = new Data();
            a.Items = new Foo[] { new Foo() };
        }

        class Foo
        {
            public Foo FooProperty { get; set; }
        }
    }
}