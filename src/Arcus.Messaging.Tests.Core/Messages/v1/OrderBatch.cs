using System;
using System.Collections.Generic;
using System.Text;

namespace Arcus.Messaging.Tests.Core.Messages.v1
{
    public class OrderBatch
    {
        public Order[] Orders { get; set; }
    }
}
