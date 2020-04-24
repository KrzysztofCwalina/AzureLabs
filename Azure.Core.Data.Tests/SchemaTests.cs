using NUnit.Framework;
using System;

namespace Azure.Data.Tests
{
    public class SchemaTests
    {
        [Test]
        public void Basics()
        {
            dynamic contact = Model.CreateWithJsonSchema("ContactSchema.json");
            contact.First = "John";
            contact.Last = "Smith";
            contact.Age = 25;

            Assert.AreEqual("John", contact.First);
            Assert.AreEqual(25, contact.Age);

            contact.First = "Mark";
            Assert.AreEqual("Mark", contact.First);

            Assert.Throws<InvalidOperationException>(() => {
                contact.First = 20;
            });

            Assert.Throws<InvalidOperationException>(() => {
                contact.Middle = "Mark";
            });
        }
    }
}