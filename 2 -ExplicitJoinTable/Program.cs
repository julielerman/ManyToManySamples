using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;

// Same as ReallySimple, but this time with an explicitly-defined entity type for the join table
// Changes from ReallySimple:
// - CommunityPerson type
// - OnModelCreating to configure it

public class Community
{
    public int Id { get; set; }
    public string Name { get; set; }

    public ICollection<Person> Members { get; } = new List<Person>();
}

public class Person
{
    public int Id { get; set; }
    public string Name { get; set; }

    public ICollection<Community> Memberships { get; } = new List<Community>();
}

public class CommunityPerson
{
    public int CommunityId { get; set; }
    public int PersonId { get; set; }
}

public class CommunitiesContext : DbContext
{
    public DbSet<Community> Communities { get; set; }
    public DbSet<Person> People { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // This is the fluent API to configure a join table explicitly
        modelBuilder
            .Entity<Community>()
            .HasMany(e => e.Members)
            .WithMany(e => e.Memberships)
            .UsingEntity<CommunityPerson>(
                e => e.HasOne<Person>().WithMany(),
                e => e.HasOne<Community>().WithMany());
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder
            .LogTo(Console.WriteLine, new[] { RelationalEventId.CommandExecuted })
            .EnableSensitiveDataLogging()
            .UseSqlite("Data Source = test.db");
}

public static class Program
{
    public static void Main()
    {
        using (var context = new CommunitiesContext())
        {
            // Show the EF model
            // Notice that in this case the join table is the CommunityPerson entity type defined above
            Console.WriteLine();
            Console.WriteLine("EF model is:");
            Console.WriteLine(context.Model.ToDebugString(MetadataDebugStringOptions.ShortDefault));
            Console.WriteLine();

            // Create a clean database each time
            // SQL is logged so you can see the join table is defined automatically
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            var arthur = new Person { Name = "Arthur" };
            var wendy = new Person { Name = "Wendy" };
            var julie = new Person { Name = "Julie" };
            var erik = new Person { Name = "Erik" };
            var jeremy = new Person { Name = "Jeremy" };

            var efCommunity = new Community {Name = "EF Community"};
            var dogPeople = new Community {Name = "Dog People"};
            var microsofties = new Community {Name = "Microsofties"};

            // Can add to collection on one side...
            efCommunity.Members.Add(arthur);
            efCommunity.Members.Add(julie);
            efCommunity.Members.Add(erik);
            efCommunity.Members.Add(jeremy);

            dogPeople.Members.Add(arthur);
            dogPeople.Members.Add(jeremy);
            dogPeople.Members.Add(julie);
            dogPeople.Members.Add(wendy);

            // Or the other...
            jeremy.Memberships.Add(microsofties);
            arthur.Memberships.Add(microsofties);

            context.AddRange(efCommunity, dogPeople, microsofties);

            // SQL generated here shows insertion into the join table
            context.SaveChanges();
        }

        using (var context = new CommunitiesContext())
        {
            // Use Include to pull in the many-to-many relationship
            var communities = context.Communities.Include(e => e.Members).ToList();

            // Show what we loaded
            Console.WriteLine();
            Console.WriteLine();
            foreach (var community in communities)
            {
                Console.Write($"Community \"{community.Name}\" has members");

                foreach (var member in community.Members)
                {
                    Console.Write($" '{member.Name}'");
                }

                Console.WriteLine();
            }

            // Show what the state manager is tracking
            // Notice that entities are being tracked for the join table
            Console.WriteLine();
            Console.WriteLine("DbContext is tracking:");
            Console.WriteLine(context.ChangeTracker.ToDebugString(ChangeTrackerDebugStringOptions.LongDefault, 2));
        }
    }
}
