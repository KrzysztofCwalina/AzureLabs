using NUnit.Framework;

namespace Azure.Data.Tests
{
    public class DigitalTwinsTests
    {
        [Test]
        public void DigitalTwins()
        {
            var json =
            "{" +
            "\"service_defined_1\" : \"some value\"," +
            "\"service_defined_2\" : 123," +
            "\"service_defined_3\" : true," +
            "\"user_defined_1\" : \"hi\"," +
            "\"user_defined_2\" : \"hello\"" +
            "}";

            var twin = new UserDefinedTwin(json);

            string original = twin.ToString();
            Assert.IsTrue(ReferenceEquals(json, original));

            Assert.AreEqual(123, twin["service_defined_2"]);
            Assert.AreEqual("hi", twin["user_defined_1"]);

            int numberOfProperties = 0;
            foreach(string property in twin.PropertyNames)
            {
                numberOfProperties++;
            }
            Assert.AreEqual(5, numberOfProperties);

            dynamic dynamic = new ReadOnlyJson(json);
            Assert.AreEqual(123, dynamic.service_defined_2);
            Assert.AreEqual("hi", dynamic.user_defined_1);

            Assert.AreEqual(123, twin.service_defined_2);
            Assert.AreEqual("hi", twin.user_defined_1);
        }

        class DigitalTwin : ReadOnlyJson
        {
            public DigitalTwin(string json) : base(json) { }

            public string service_defined_1 => (string)this["service_defined_1"];
            public byte service_defined_2 => (byte)this["service_defined_2"];
            public bool service_defined_3 => (bool)this["service_defined_3"];
        }
        class UserDefinedTwin : DigitalTwin
        {
            public UserDefinedTwin(string json) : base(json) { }

            public string user_defined_1 => (string)this["user_defined_1"];
        }
    }
}