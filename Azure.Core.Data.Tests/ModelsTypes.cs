using System;

namespace Azure.Data.Tests
{
    public struct Contact
    {
        public string First { get; set; }
        public string Last { get; set; }
        public int Age { get; set; }
        public Address Address { get; set; }

        public string[] Phones { get; set; }
    }

    public struct Address
    {
        public int Zip { get; set; }
        public string City { get; set; }
    }
}
