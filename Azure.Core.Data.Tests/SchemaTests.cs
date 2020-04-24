using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Azure.Data.Tests
{
    public class SchemaTests
    {
        [Test]
        public void Basics()
        {
            dynamic contact = Model.Create(new ContactSchema());
            contact.First = "John";
            contact.Last = "Smith";
            contact.Age = 25;

            Assert.AreEqual("John", contact.First);
            Assert.AreEqual(25, contact.Age);

            contact.First = "Mark";
            Assert.AreEqual("Mark", contact.First);

            Assert.Throws<Exception>(() => {
                contact.First = 20;
            });

            Assert.Throws<Exception>(() => {
                contact.Middle = "Mark";
            });
        }

        class ContactSchema : ModelSchema
        {
            Dictionary<string, PropertySchema> _properties = new Dictionary<string, PropertySchema>();

            public ContactSchema()
            {
                _properties.Add("First", new PropertySchema(typeof(string), "First", false));
                _properties.Add("Last", new PropertySchema(typeof(string), "Last", false));
                _properties.Add("Age", new PropertySchema(typeof(int), "Age", false));
            }
            public override IEnumerable<string> PropertyNames => _properties.Keys;

            public override bool TryGetSchema(string propertyName, out PropertySchema schema)
            {
                return _properties.TryGetValue(propertyName, out schema);
            }
        }
    }
}