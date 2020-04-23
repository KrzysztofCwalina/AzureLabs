using System;
using System.Collections.Generic;
using System.Text;

namespace Azure.Data.Tests
{
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
