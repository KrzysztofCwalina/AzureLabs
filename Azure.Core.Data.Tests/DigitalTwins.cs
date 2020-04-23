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

            var twin = new ReadOnlyJson(json);

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

            UserDefinedTwin udt = (UserDefinedTwin)dynamic;
            Assert.AreEqual(123, udt.service_defined_2);
            Assert.AreEqual("hi", udt.user_defined_1);
        }

        class DigitalTwin
        {
            public string service_defined_1 { get; set; }
            public int service_defined_2 { get; set; }
            public bool service_defined_3 { get; set; }
        }
        class UserDefinedTwin : DigitalTwin
        {
            public string user_defined_1 { get; set; }
        }
    }
}