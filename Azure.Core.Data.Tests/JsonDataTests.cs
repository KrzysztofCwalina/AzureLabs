using NUnit.Framework;

namespace Azure.Data.Tests
{
    public class JsonModelTest
    {
        static string s_contact1 = "{ \"First\" : \"John\", \"Last\" : \"Smith\", \"Age\" : 25, \"Address\" : { \"Zip\" : 98052, \"City\" : \"Redmond\" } }";
        static string s_contact2 = "{ \"First\" : \"Marry\", \"Last\" : \"Smith\", \"Age\" : 30, \"Address\" : { \"Zip\" : 98052, \"City\" : \"Redmond\" } }";

        [Test]
        public void ReadJson()
        {      
            dynamic contact = new ReadOnlyJson(s_contact1);

            Assert.AreEqual("John", contact.First);
            Assert.AreEqual("Smith", contact.Last);
            Assert.AreEqual(25, contact.Age);

            dynamic address = contact.Address;

            Assert.AreEqual(98052, address.Zip);
            Assert.AreEqual("Redmond", address.City);

            Assert.AreEqual(98052, contact.Address.Zip);
            Assert.AreEqual("Redmond", contact.Address.City);
        }

        [Test]
        public void Deserialize()
        {
            dynamic contact = new ReadOnlyJson(s_contact1);
            var deserialized = (Contact)contact;

            Assert.AreEqual(deserialized.First, contact.First);
            Assert.AreEqual(deserialized.Last, contact.Last);
        }

        [Test]
        public void PrimitiveArray()
        {
            var data = "[5,10,20]";
            dynamic json = new ReadOnlyJson(data);
            Assert.AreEqual(5, json[0]);
            Assert.AreEqual(20, json[2]);

            int[] array = (int[])json;
            Assert.AreEqual(5, array[0]);
            Assert.AreEqual(20, array[2]);
        }

        [Test]
        public void ArrayOfArrays()
        {
            var data = $"[[{s_contact1},{s_contact2}]]";

            dynamic json = new ReadOnlyJson(data);

            dynamic contacts = json[0];
            dynamic contact = contacts[0];
            Assert.AreEqual(25, contact.Age);
        }
    }
}