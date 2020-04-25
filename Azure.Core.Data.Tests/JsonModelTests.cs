using NUnit.Framework;

namespace Azure.Data.Tests
{
    public class JsonModelTest
    {
        static string s_contact1 = "{ \"First\" : \"John\", \"Last\" : \"Smith\", \"Age\" : 25, \"Address\" : { \"Zip\" : 98052, \"City\" : \"Redmond\" } , \"Phones\" : [ \"425-999-9999\",  \"425-999-9998\" ] }";
        static string s_contact2 = "{ \"First\" : \"Marry\", \"Last\" : \"Smith\", \"Age\" : 30, \"Address\" : { \"Zip\" : 98052, \"City\" : \"Redmond\" } }";

        [Test]
        public void ReadJson()
        {
            Data json = JsonData.Create(s_contact1);
            dynamic contact = json;

            Assert.AreEqual("John", contact.First);
            Assert.AreEqual("Smith", contact.Last);
            Assert.AreEqual(25, contact.Age);
            Assert.AreEqual(98052, contact.Address.Zip);
            Assert.AreEqual("Redmond", contact.Address.City);

            dynamic address = contact.Address;

            Assert.AreEqual(98052, address.Zip);
            Assert.AreEqual("Redmond", address.City);

            dynamic phones = contact.Phones;
            Assert.AreEqual("425-999-9999", phones[0]);
            Assert.AreEqual("425-999-9998", phones[1]);
        }

        [Test]
        public void Deserialize()
        {
            dynamic json = JsonData.Create(s_contact1);
            var contact = (Contact)json;

            Assert.AreEqual("John", contact.First);
            Assert.AreEqual("Smith", contact.Last);
            Assert.AreEqual(25, contact.Age);
            Assert.AreEqual(98052, json.Address.Zip);
            Assert.AreEqual("Redmond", json.Address.City);

            var address = (Address)json.Address;

            Assert.AreEqual(98052, address.Zip);
            Assert.AreEqual("Redmond", address.City);

            var phones = (string[])json.Phones;
            Assert.AreEqual("425-999-9999", phones[0]);
            Assert.AreEqual("425-999-9998", phones[1]);
        }

        [Test]
        public void Dictionary()
        {
            Data contact = JsonData.Create(s_contact1);

            Assert.AreEqual("John", contact["First"]);
            Assert.AreEqual("Smith", contact["Last"]);
            Assert.AreEqual(25, contact["Age"]);

            var address = (Data)contact["Address"];
            Assert.AreEqual(98052, address["Zip"]);
            Assert.AreEqual("Redmond", address["City"]);

            //var phones = (string[])contact["Phones"];
            //Assert.AreEqual("425-999-9999", phones[0]);
            //Assert.AreEqual("425-999-9998", phones[1]);
        }

        [Test]
        public void PrimitiveArray()
        {
            var data = "[5,10,20]";
            dynamic json = JsonData.Create(data);
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

            dynamic json = JsonData.Create(data);
            dynamic contacts = json[0];
            dynamic contact = contacts[0];

            Assert.AreEqual(25, contact.Age);
        }
    }
}