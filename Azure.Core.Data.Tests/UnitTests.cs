using NUnit.Framework;
using System;

namespace Azure.Data.Tests
{
    public class DynamicDataTests
    {
        [Test]
        public void Basics()
        {
            dynamic contact = DynamicData.Create();
            contact.First = "John";
            contact.Last = "Smith";
            contact.Age = 25;

            Assert.AreEqual("John", contact.First);
            Assert.AreEqual(25, contact.Age);

            contact.First = "Mark";
            Assert.AreEqual("Mark", contact.First);
        }

        [Test]
        public void FromJson()
        {
            var data = "{ \"First\" : \"John\", \"Last\" : \"Smith\" }";
            dynamic contact = DynamicData.CreateFromJson(data);
            Assert.AreEqual("John", contact.First);
            Assert.AreEqual("Smith", contact.Last);

            var deserialized = (Contact)contact;

            Assert.AreEqual(deserialized.First, contact.First);
            Assert.AreEqual(deserialized.Last, contact.Last);
        }

        [Test]
        public void Deserialize()
        {
            dynamic address = DynamicData.Create();
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

            dynamic contact = DynamicData.Create();
            contact.Address = address;

            Assert.AreEqual("Redmond", contact.Address.City);
            Assert.AreEqual(98052, contact.Address.Zip);
        }

        [Test]
        public void PrimitiveArray()
        {
            var data = "[5,10,20]";
            dynamic array = DynamicData.CreateFromJson(data);
            Assert.AreEqual(5, array[0]);
            Assert.AreEqual(20, array[2]);

            var intArray = (int[])array;
            Assert.AreEqual(5, intArray[0]);
            Assert.AreEqual(20, intArray[2]);
        }

        [Test]
        public void ComplexArray()
        {
            var data = "[{\"Foo\":10 },{\"Foo\":20}]";
            dynamic array = DynamicData.CreateFromJson(data);
            dynamic first = array[0];

            Assert.AreEqual(10, first.Foo);
            Assert.AreEqual(20, array[1].Foo);

            var dynamicArray = (dynamic[])array;

            dynamic firstNext = dynamicArray[0];
            Assert.AreEqual(10, firstNext.Foo);
            Assert.AreEqual(20, dynamicArray[1].Foo);
        }

        [Test]
        public void ComplexArray2()
        {
            var data = "[{\"Foo\":10 },{\"Foo\":20}]";
            dynamic array = DynamicData.CreateFromJson(data);
            var dynamicArray = (dynamic[])array;

            dynamic firstNext = dynamicArray[0];
            Assert.AreEqual(10, firstNext.Foo);
        }

        [Test]
        public void AnonymousValue()
        {
            dynamic contact = DynamicData.Create();
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
            DynamicData data = DynamicData.CreateReadOnly(
                ("First", "John"),
                ("Last", "Smith"),
                ("Age", 25)
            );

            Assert.Throws<InvalidOperationException>(() =>
            {
                data["First"] = "Mark";
            });

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
            DynamicData data = DynamicData.Create();
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

        public struct Contact
        {
            public string First { get; set; }
            public string Last { get; set; }

            public Address Address { get; set; }
        }

        public struct Address
        {
            public int Zip { get; set; }
            public string City { get; set; }
        }
    }
}