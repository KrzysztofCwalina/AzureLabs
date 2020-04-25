using NUnit.Framework;
using System;
using System.Collections;
using System.Dynamic;

namespace Azure.Data.Tests
{
    public class ReferenceImplementationTests
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
    }
}