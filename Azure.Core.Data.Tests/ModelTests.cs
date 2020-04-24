using NUnit.Framework;
using System;

namespace Azure.Data.Tests
{
    public class ModelTests
    {
        [Test]
        public void Basics()
        {
            dynamic contact = Model.Create();
            contact.First = "John";
            contact.Last = "Smith";
            contact.Age = 25;

            Assert.AreEqual("John", contact.First);
            Assert.AreEqual(25, contact.Age);

            contact.First = "Mark";
            Assert.AreEqual("Mark", contact.First);
        }

        [Test]
        public void Deserialize()
        {
            dynamic address = Model.Create();
            address.Zip = 98052;
            address.City = "Redmond";

            var deserialized = (Address)address;
            Assert.AreEqual("Redmond", deserialized.City);
            Assert.AreEqual(98052, deserialized.Zip);
        }

        [Test]
        public void Serialize()
        {
            var address = new Address();
            address.City = "Redmond";
            address.Zip = 98052;

            dynamic contact = Model.Create();
            contact.Address = address;

            Assert.AreEqual("Redmond", contact.Address.City);
            Assert.AreEqual(98052, contact.Address.Zip);
        }

        [Test]
        public void ComplexArray()
        {
            var data = "[{\"Foo\":10 },{\"Foo\":20}]";
            dynamic array = Model.CreateFromJson(data);
            dynamic first = array[0];

            Assert.AreEqual(10, first.Foo);
            Assert.AreEqual(20, array[1].Foo);

            var dynamicArray = (dynamic[])array;

            dynamic firstNext = dynamicArray[0];
            Assert.AreEqual(10, firstNext.Foo);
            Assert.AreEqual(20, dynamicArray[1].Foo);
        }

        [Test]
        public void AnonymousValue()
        {
            dynamic contact = Model.Create();
            contact.Address = new { Zip = 98052, City = "Redmond" };

            Assert.AreEqual("Redmond", contact.Address.City);
            Assert.AreEqual(98052, contact.Address.Zip);

            dynamic address = contact.Address;
            Assert.AreEqual("Redmond", address.City);
            Assert.AreEqual(98052, address.Zip);

            var addressValue = (Address)address;
            Assert.AreEqual("Redmond", addressValue.City);
            Assert.AreEqual(98052, addressValue.Zip);
        }

        [Test]
        public void ReadOnly()
        {
            ReadOnlyModel data = Model.CreateReadOnly(
                ("First", "John"),
                ("Last", "Smith"),
                ("Age", 25)
            );

            dynamic contact = data;

            Assert.Throws<InvalidOperationException>(() =>
            {
                contact.First = "Mark";
            });

            Assert.AreEqual("John", contact.First);
            Assert.AreEqual("John", data["First"]);
        }

        [Test]
        public void Indexer()
        {
            ReadWriteModel data = Model.Create();
            data["First"] = "John";
            data["Last"] = "Smith";
            data["Age"] = 25;

            Assert.AreEqual("John", data["First"]);

            data["First"] = "Mark";

            dynamic contact = data;
            Assert.AreEqual("Mark", contact.First);
            Assert.AreEqual("Mark", data["First"]);

            Assert.AreEqual(25, contact.Age);
            Assert.AreEqual(25, data["Age"]);
        }
    }
}