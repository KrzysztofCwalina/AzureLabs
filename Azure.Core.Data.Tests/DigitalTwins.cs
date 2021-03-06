using Azure.Core.Data.DataStores;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace Azure.Data.Tests
{
    public class DigitalTwinsTests
    {
        static string s_demo_payload =
            "{" +
            "\"Id\" : \"ID0001\"," +        // service defined
            "\"CreatedAt\" : 123," +        // service defined
            "\"Decomissioned\" : true," +   // service defined

            "\"Temperature\" : 72," +       // user defined
            "\"Unit\" : \"F\"" +            // user defined
            "}";

        [Test]
        public void ClientDemo()
        {
            var client = new DigitalTwinsClient();

            DigitalTwin twin = client.GetTwin(); // creates DigitalTwin instance and stores the JSON payload in it, i.e. very cheap
            string json = twin.ToString();       // gets the origin JSON payload.
            Assert.IsTrue(ReferenceEquals(s_demo_payload, json)); // the payload is really the same string as was passed into DigitalTwin ctor

            Assert.AreEqual("ID0001", twin.Id);  // this is where the JSON string is parsed (lazily)
            Assert.AreEqual(123, twin.CreatedAt);
            Assert.AreEqual(true, twin.Decomissioned);

            // Temperature and Unit are not on DigitaTwin (they are user defined properties), so let's use dynamic APIs.
            dynamic dynamic = twin;

            Assert.AreEqual(72, dynamic.Temperature); 
            Assert.AreEqual("F", dynamic.Unit);
            Assert.AreEqual(123, dynamic.CreatedAt); // the service defined properties are also avaliable through dynamic calls.

            // the client also has strongly typed APIs
            TemperatureSensor sensor = client.GetTwin<TemperatureSensor>();

            Assert.AreEqual("F", sensor.Unit);
            Assert.AreEqual(72, sensor.Temperature);
            Assert.AreEqual("ID0001", sensor.Id);
            Assert.AreEqual(123, sensor.CreatedAt);
            Assert.AreEqual(true, sensor.Decomissioned);

            // Interestingly, the base twin type can be converted to user defined type
            sensor = twin.As<TemperatureSensor>();

            Assert.AreEqual("F", sensor.Unit);
            Assert.AreEqual(72, sensor.Temperature);

            Assert.AreEqual("ID0001", sensor.Id);
            Assert.AreEqual(123, sensor.CreatedAt);
            Assert.AreEqual(true, sensor.Decomissioned);
        }

        [Test]
        public void Dtdl()
        {
            var schema = DtDlParser.ParseFile("Thermostat.dtdl");
            dynamic model = new DynamicData(schema);

            model.setPointTemp = 97.5;

            // temp is read-only
            Assert.Throws<InvalidOperationException>(() => {
                model.temp = 99.8;
            });
        }

        [Test]
        public void DynamicTypeSystemDemo()
        {
            DynamicData twin = JsonData.Create(s_demo_payload);

            string original = twin.ToString();
            Assert.IsTrue(ReferenceEquals(s_demo_payload, original));

            Assert.AreEqual(123, twin["CreatedAt"]);
            Assert.AreEqual(72, twin["Temperature"]);
            Assert.AreEqual("F", twin["Unit"]);

            int numberOfProperties = 0;
            foreach(string property in twin.PropertyNames)
            {
                numberOfProperties++;
            }
            Assert.AreEqual(5, numberOfProperties);

            dynamic dynamicTwin = twin;
            Assert.AreEqual(72, dynamicTwin.Temperature);
            Assert.AreEqual("F", dynamicTwin.Unit);
            Assert.AreEqual("ID0001", dynamicTwin.Id);
        }

        // DigitalTwin Library Type
        public class DigitalTwinsClient
        {
            public DigitalTwin GetTwin() => new DigitalTwin(s_demo_payload);

            public T GetTwin<T>() => (T)Activator.CreateInstance(typeof(T), new object[] { s_demo_payload });
        }

        // DigitalTwin Library Type
        public class DigitalTwin : DynamicData
        {
            public DigitalTwin(string json) : base(JsonData.CreateStore(json)) { }

            public T As<T>() where T: DigitalTwin => (T)Activator.CreateInstance(typeof(T), new object[] { (DigitalTwin)this });

            public string Id => (string)this["Id"];
            public int CreatedAt => (int)(double)this["CreatedAt"];
            public bool Decomissioned => (bool)this["Decomissioned"];
        }

        // User Defined Type
        class TemperatureSensor : DigitalTwin
        {
            public TemperatureSensor(string json) : base(json) { }

            public TemperatureSensor(DigitalTwin twin) : base(twin.ToString()) { }

            public string Unit => (string)this["Unit"];
            public double Temperature => (double)this["Temperature"];
        }
    }
}