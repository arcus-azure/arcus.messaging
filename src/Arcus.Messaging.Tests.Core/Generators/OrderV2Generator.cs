using System;
using System.Collections.Generic;
using System.Text;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Arcus.Messaging.Tests.Core.Messages.v2;
using Bogus;

namespace Arcus.Messaging.Tests.Core.Generators
{
    public static class OrderV2Generator
    {
        public static OrderV2 Generate()
        {
            Faker<Customer> customerGenerator = new Faker<Customer>()
                .RuleFor(u => u.FirstName, (f, u) => f.Name.FirstName())
                .RuleFor(u => u.LastName, (f, u) => f.Name.LastName());

            Faker<OrderV2> orderGenerator = new Faker<OrderV2>()
                .RuleFor(u => u.Customer, () => customerGenerator)
                .RuleFor(u => u.Id, f => Guid.NewGuid().ToString())
                .RuleFor(u => u.Amount, f => f.Random.Int())
                .RuleFor(u => u.ArticleNumber, f => f.Commerce.Product())
                .RuleFor(u => u.Status, f => f.Random.Int());

            return orderGenerator.Generate();
        }
    }
}
