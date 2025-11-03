using LibHub.CatalogService.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LibHub.CatalogService.Data;

public static class BookSeeder
{
    public static async Task SeedBooksAsync(CatalogDbContext context)
    {
        if (await context.Books.AnyAsync())
        {
            return;
        }

        var books = new List<Book>
        {
            new Book(
                "9780134685991",
                "Effective Java",
                "Joshua Bloch",
                "Programming",
                "A comprehensive guide to best practices in Java programming, covering topics from object creation to concurrency.",
                5
            ),
            new Book(
                "9780132350884",
                "Clean Code",
                "Robert C. Martin",
                "Software Engineering",
                "A handbook of agile software craftsmanship with practical advice on writing clean, maintainable code.",
                8
            ),
            new Book(
                "9780201633610",
                "Design Patterns",
                "Erich Gamma, Richard Helm, Ralph Johnson, John Vlissides",
                "Software Engineering",
                "Elements of reusable object-oriented software. The classic Gang of Four design patterns book.",
                4
            ),
            new Book(
                "9780137081073",
                "The Clean Coder",
                "Robert C. Martin",
                "Professional Development",
                "A code of conduct for professional programmers, covering professionalism, time management, and career development.",
                6
            ),
            new Book(
                "9781617294136",
                "Microservices Patterns",
                "Chris Richardson",
                "Software Architecture",
                "Comprehensive guide to building microservices with patterns for decomposition, communication, and data management.",
                3
            ),
            new Book(
                "9780134494166",
                "Clean Architecture",
                "Robert C. Martin",
                "Software Architecture",
                "A craftsman's guide to software structure and design, focusing on creating maintainable and flexible architectures.",
                7
            ),
            new Book(
                "9781449373320",
                "Designing Data-Intensive Applications",
                "Martin Kleppmann",
                "Distributed Systems",
                "The big ideas behind reliable, scalable, and maintainable systems. Essential reading for backend engineers.",
                5
            ),
            new Book(
                "9780321125217",
                "Domain-Driven Design",
                "Eric Evans",
                "Software Design",
                "Tackling complexity in the heart of software through strategic design and tactical patterns.",
                4
            ),
            new Book(
                "9780596007126",
                "Head First Design Patterns",
                "Eric Freeman, Elisabeth Robson",
                "Programming",
                "A brain-friendly guide to design patterns with visual learning and engaging examples.",
                10
            ),
            new Book(
                "9780135957059",
                "The Pragmatic Programmer",
                "David Thomas, Andrew Hunt",
                "Professional Development",
                "Your journey to mastery. Timeless advice for software developers on craftsmanship and career growth.",
                6
            ),
            new Book(
                "9781491950357",
                "Building Microservices",
                "Sam Newman",
                "Software Architecture",
                "Designing fine-grained systems with practical guidance on service boundaries and deployment.",
                5
            ),
            new Book(
                "9780321127426",
                "Patterns of Enterprise Application Architecture",
                "Martin Fowler",
                "Software Architecture",
                "Essential patterns for enterprise application development including layering, domain logic, and data source patterns.",
                3
            ),
            new Book(
                "9781492040347",
                "Fundamentals of Software Architecture",
                "Mark Richards, Neal Ford",
                "Software Architecture",
                "An engineering approach to software architecture covering architectural thinking, modularity, and soft skills.",
                4
            ),
            new Book(
                "9781617293290",
                "Spring in Action",
                "Craig Walls",
                "Programming",
                "Comprehensive guide to Spring Framework covering Spring Boot, Spring MVC, and Spring Data.",
                8
            ),
            new Book(
                "9781484239506",
                "Pro ASP.NET Core 3",
                "Adam Freeman",
                "Web Development",
                "The expert guide to ASP.NET Core development with MVC, Razor Pages, and Web API.",
                5
            )
        };

        await context.Books.AddRangeAsync(books);
        await context.SaveChangesAsync();
    }
}

