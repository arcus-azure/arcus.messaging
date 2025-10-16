using System;
using System.Drawing;
using Bogus;

namespace Arcus.Messaging
{
    /// <summary>
    /// Represents a set of example message implementations used throughout message routing tests.
    /// </summary>
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

        public static FreshPizzaOrdered FreshPizzaOrdered => CreatePizzaOrder.Generate();
        public static HealthyTreePlanted HealthyTreePlanted => CreateTreePlanted.Generate();
        public static NewStarDiscovered NewStarDiscovered => CreateStarDiscovered.Generate();

        public static object Any => Bogus.PickRandom<object>(FreshPizzaOrdered, HealthyTreePlanted, NewStarDiscovered);

        /// <summary>
        /// Creates a message of type <typeparamref name="T"/>.
        /// </summary>
        public static object Create<T>()
        {
            if (typeof(T) == typeof(FreshPizzaOrdered))
            {
                return FreshPizzaOrdered;
            }

            if (typeof(T) == typeof(HealthyTreePlanted))
            {
                return HealthyTreePlanted;
            }

            if (typeof(T) == typeof(NewStarDiscovered))
            {
                return NewStarDiscovered;
            }

            throw new ArgumentOutOfRangeException(nameof(T), typeof(T), "unknown message type");
        }

        /// <summary>
        /// Pattern match against a given <paramref name="message"/>.
        /// </summary>
        public static TResult Match<TResult>(
            object message,
            Func<FreshPizzaOrdered, TResult> ifPizza,
            Func<HealthyTreePlanted, TResult> ifTree,
            Func<NewStarDiscovered, TResult> ifStar)
        {
            return message switch
            {
                FreshPizzaOrdered x => ifPizza(x),
                HealthyTreePlanted x => ifTree(x),
                NewStarDiscovered x => ifStar(x),
                _ => throw new ArgumentOutOfRangeException(nameof(message), message, "unknown message type")
            };
        }
    }

    public class FreshPizzaOrdered
    {
        public string PizzaName { get; set; }
        public double TotalPrize { get; set; }
        public DateTimeOffset DeliveryDate { get; set; }
    }

    public class HealthyTreePlanted
    {
        public string TreeName { get; set; }
        public string RequesterName { get; set; }
        public Point Location { get; set; }
    }

    public class NewStarDiscovered
    {
        public string TemporaryName { get; set; }
        public DateTimeOffset DiscoveryTime { get; set; }
        public Point Coordinate { get; set; }
    }
}
