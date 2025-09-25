using System;
using Arcus.Messaging.Abstractions.MessageHandling;
using Xunit;

namespace Arcus.Messaging
{
    public static class AssertResult
    {
        public static void RoutePassed(MessageProcessingResult actual)
        {
            Assert.True(actual.IsSuccessful, $"message processing result should represent a successful operation, but was a failure => {actual}");
        }

        public static void RouteFailed(MessageProcessingError expectedError, MessageProcessingResult actual, params string[] errorParts)
        {
            Assert.False(actual.IsSuccessful, "message processing result should represent a failure, but was successful");
            Assert.Equal(expectedError, actual.Error);
            Assert.All(errorParts, part => Assert.Contains(part, actual.ErrorMessage, StringComparison.CurrentCultureIgnoreCase));
        }
    }
}
