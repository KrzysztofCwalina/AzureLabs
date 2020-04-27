using Azure.Core.Data.DataStores;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace Azure.Data.Tests
{
    public class ContractTests
    {
        [Test]
        public void PerfectHash()
        {
            var dictionary = new Dictionary<string, object>();
            dictionary.Add("a", 1);

            DataStore store;

            store = PerfectHashStore.Create(dictionary);
            Assert.IsNotNull(store);

            dictionary.Add("b", 2);
            store = PerfectHashStore.Create(dictionary);
            Assert.IsNotNull(store);
        }

        public void SetProperties()
        {
            dynamic d = new DynamicData();
            d.PString = "hello world";
            d.PInt32 = 32;

            Assert.AreEqual(32, d.PInt32);
            Assert.AreEqual("hello world", d.PString);
        }

        [Test]
        public void Cycle()
        {
            dynamic a = new DynamicData();
            dynamic b = new DynamicData();
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

            dynamic d = new DynamicData();

            // TODO: should this throw?
            Assert.Throws<InvalidOperationException>(() => {
                d.Property = a;
            });
        }

        // TODO (pri 1): fix these
        //[Test]
        //public void PrimitiveArray()
        //{
        //    dynamic a = new DynamicData();
        //    a.Items = new int[] { 1, 2, 3 };
        //}

        //[Test]
        //public void ComplexArray()
        //{
        //    dynamic a = new DynamicData();
        //    a.Items = new Foo[] { new Foo() };
        //}

        class Foo
        {
            public Foo FooProperty { get; set; }
        }
    }
}