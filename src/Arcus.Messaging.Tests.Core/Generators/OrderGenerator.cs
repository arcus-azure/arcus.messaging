using System;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Bogus;

namespace Arcus.Messaging.Tests.Core.Generators
{
    public class OrderGenerator
    {
        public static Order Generate()
        {
            var customerGenerator = new Faker<Customer>()
                .RuleFor(u => u.FirstName, (f, u) => f.Name.FirstName())
                .RuleFor(u => u.LastName, (f, u) => f.Name.LastName());

            var orderGenerator = new Faker<Order>()
                .RuleFor(u => u.Customer, () => customerGenerator)
                .RuleFor(u => u.Id, f => Guid.NewGuid().ToString())
                .RuleFor(u => u.Amount, f => f.Random.Int())
                .RuleFor(u => u.ArticleNumber, f => f.Commerce.Product())
                .RuleFor(u => u.Date, f => f.Date.RecentOffset());

            return orderGenerator.Generate();
        }
    }
}