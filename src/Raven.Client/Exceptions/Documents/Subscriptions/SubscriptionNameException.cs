using System;

namespace Raven.Client.Exceptions.Documents.Subscriptions
{
    public class SubscriptionNameException : SubscriptionException
    {
        public SubscriptionNameException(string message) : base(message)
        {
        }

        public SubscriptionNameException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}
