using System;
using System.Drawing;
using Bogus;

namespace Arcus.Messaging
{
    public static class Messages
    {
        private static readonly Faker Bogus = new();
        private static readonly Faker<FreshPizzaOrdered> CreatePizzaOrder = new Faker<FreshPizzaOrdered>()
            .RuleFor(o => o.PizzaName, f => f.PickRandom("juicy", "big", "delicious") + " " + f.Commerce.Color() + " " + f.PickRandom("olives", "cheese", "peperoni"))
            .RuleFor(o => o.TotalPrize, f => f.Random.Double(1D, 25D))
            .RuleFor(o => o.DeliveryDate, f => f.Date.RecentOffset());

        private static readonly Faker<HealthyTreePlanted> CreateTreePlanted = new Faker<HealthyTreePlanted>()
            .RuleFor(p => p.TreeName, f => f.Commerce.Color() + " " + f.PickRandom("oak", "maple", "pine"))
            .RuleFor(p => p.RequesterName, f => f.Person.FirstName)
            .RuleFor(p => p.Location, f => new Point(f.Random.Int(), f.Random.Int()));

        private static readonly Faker<NewStarDiscovered> CreateStarDiscovered = new Faker<NewStarDiscovered>()
            .RuleFor(d => d.TemporaryName, f => "star " + f.Random.AlphaNumeric(5))
            .RuleFor(d => d.Coordinate, f => new Point(f.Random.Int(), f.Random.Int()))
            .RuleFor(d => d.DiscoveryTime, f => f.Date.RecentOffset());

        public static FreshPizzaOrdered PizzaOrdered => CreatePizzaOrder.Generate();
        public static HealthyTreePlanted TreePlanted => CreateTreePlanted.Generate();
        public static NewStarDiscovered StarDiscovered => CreateStarDiscovered.Generate();

        public static ITestMessage Any => Bogus.PickRandom<ITestMessage>(PizzaOrdered, TreePlanted, StarDiscovered);
    }

    public interface ITestMessage { }

    public class FreshPizzaOrdered : ITestMessage
    {
        public string PizzaName { get; set; }
        public double TotalPrize { get; set; }
        public DateTimeOffset DeliveryDate { get; set; }
    }

    public class HealthyTreePlanted : ITestMessage
    {
        public string TreeName { get; set; }
        public string RequesterName { get; set; }
        public Point Location { get; set; }
    }

    public class NewStarDiscovered : ITestMessage
    {
        public string TemporaryName { get; set; }
        public DateTimeOffset DiscoveryTime { get; set; }
        public Point Coordinate { get; set; }
    }
}
