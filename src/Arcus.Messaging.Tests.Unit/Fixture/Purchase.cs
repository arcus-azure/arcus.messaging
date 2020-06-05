using System;
using System.Collections.Generic;
using System.Text;

namespace Arcus.Messaging.Tests.Unit.Fixture
{
    /// <summary>
    /// Test message implementation to test specific message handling scenario's.
    /// </summary>
    public class Purchase
    {
        public string Price { get; set; }

        public string CustomerName { get; set; }
    }
}
