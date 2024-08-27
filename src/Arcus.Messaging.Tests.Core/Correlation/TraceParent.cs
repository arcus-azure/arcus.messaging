using Bogus;

namespace Arcus.Messaging.Tests.Core.Correlation
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

        public static TraceParent Parse(string diagnosticId)
        {
            string[] parts = diagnosticId.Split('-');
            string transactionId = parts[1];
            string operationParentId = parts[2];

            return new TraceParent(transactionId, operationParentId);
        }
    }
}
