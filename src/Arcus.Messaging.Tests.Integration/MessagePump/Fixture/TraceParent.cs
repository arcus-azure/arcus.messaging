using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bogus;

namespace Arcus.Messaging.Tests.Integration.MessagePump.Fixture
{
    public class TraceParent
    {
        private static readonly Faker BogusGenerator = new Faker();

        private TraceParent(string transactionId, string operationParentId)
        {
            TransactionId = transactionId;
            OperationParentId = operationParentId;
        }

        public string TransactionId { get; }
        public string OperationParentId { get; }
        public string DiagnosticId => $"00-{TransactionId}-{OperationParentId}-00";

        public static TraceParent Generate()
        {
            string transactionId = BogusGenerator.Random.Hexadecimal(32, prefix: null);
            string operationParentId = BogusGenerator.Random.Hexadecimal(16, prefix: null);

            return new TraceParent(transactionId, operationParentId);
        }
    }
}
