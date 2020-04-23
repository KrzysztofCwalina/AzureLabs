using NUnit.Framework;

namespace Azure.Data.Tests
{
    public class JsonModelTest
    {
        static string s_contact1 = "{ \"First\" : \"John\", \"Last\" : \"Smith\", \"Age\" : 25 }";
        static string s_contact2 = "{ \"First\" : \"Marry\", \"Last\" : \"Smith\", \"Age\" : 30 }";

        [Test]
        public void FromJson()
        {      
            dynamic contact = new ReadOnlyJson(s_contact1);
            Assert.AreEqual("John", contact.First);
            Assert.AreEqual("Smith", contact.Last);

            var deserialized = (Contact)contact;

            Assert.AreEqual(deserialized.First, contact.First);
            Assert.AreEqual(deserialized.Last, contact.Last);
        }

        [Test]
        public void PrimitiveArray()
        {
            var data = "[5,10,20]";
            dynamic array = new ReadOnlyJson(data);
            Assert.AreEqual(5, array[0]);
            Assert.AreEqual(20, array[2]);

            var intArray = (int[])array;
            Assert.AreEqual(5, intArray[0]);
            Assert.AreEqual(20, intArray[2]);
        }

        [Test]
        public void ArrayOfArrays()
        {
            var data = $"[[{s_contact1},{s_contact2}]]";

            dynamic array = new ReadOnlyJson(data);

            dynamic contactsArray = array[0];
            dynamic contact = contactsArray[0];
            Assert.AreEqual(25, contact.Age);
        }
    }
}