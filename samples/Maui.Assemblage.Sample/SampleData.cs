namespace Maui.Assemblage.Sample;

// ─── Shared Models ──────────────────────────────────────────────

public record Contact(string Name, string Email, string Avatar, string Department, string Phone);

public record Product(string Name, string Category, string Price, string ImageUrl, string Description, double Rating);

public record ChatMessage(string Sender, string Text, DateTime Timestamp, bool IsMe);

public record PhotoItem(string Title, string Photographer, string ColorHex, double AspectRatio)
{
    public Color Color => Color.FromArgb(ColorHex);
}

// ─── Data Generators ────────────────────────────────────────────

public static class SampleData
{
    private static readonly string[] FirstNames =
    [
        "Alice", "Bob", "Carol", "David", "Emma", "Frank", "Grace", "Henry",
        "Ivy", "Jack", "Kate", "Liam", "Mia", "Noah", "Olivia", "Pete",
        "Quinn", "Rose", "Sam", "Tara", "Uma", "Vic", "Wendy", "Xander",
        "Yara", "Zane", "Ava", "Ben", "Cleo", "Dan"
    ];

    private static readonly string[] LastNames =
    [
        "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller",
        "Davis", "Rodriguez", "Martinez", "Anderson", "Taylor", "Thomas",
        "Jackson", "White", "Harris", "Martin", "Thompson", "Moore", "Allen"
    ];

    private static readonly string[] Departments =
    [
        "Engineering", "Design", "Marketing", "Sales", "Support", "HR", "Finance", "Product", "Legal", "Operations"
    ];

    private static readonly string[] Avatars = ["👤", "👩", "👨", "🧑", "👩‍💻", "👨‍💻", "🧑‍💼", "👩‍🔬", "👨‍🎨", "🧑‍🚀"];

    private static readonly string[] ProductCategories =
    [
        "Electronics", "Books", "Clothing", "Home", "Sports", "Toys", "Food", "Health"
    ];

    private static readonly string[] ProductColors =
    [
        "#E74C3C", "#3498DB", "#2ECC71", "#F39C12", "#9B59B6",
        "#1ABC9C", "#E67E22", "#2980B9", "#27AE60", "#8E44AD"
    ];

    private static readonly string[] PhotoColors =
    [
        "#264653", "#2A9D8F", "#E9C46A", "#F4A261", "#E76F51",
        "#606C38", "#283618", "#DDA15E", "#BC6C25", "#FEFAE0",
        "#003049", "#D62828", "#F77F00", "#FCBF49", "#EAE2B7"
    ];

    public static List<Contact> GenerateContacts(int count)
    {
        var rng = new Random(42);
        return Enumerable.Range(0, count).Select(i =>
        {
            var first = FirstNames[rng.Next(FirstNames.Length)];
            var last = LastNames[rng.Next(LastNames.Length)];
            return new Contact(
                $"{first} {last}",
                $"{first.ToLower()}.{last.ToLower()}@example.com",
                Avatars[rng.Next(Avatars.Length)],
                Departments[rng.Next(Departments.Length)],
                $"+1 ({rng.Next(200, 999)}) {rng.Next(100, 999)}-{rng.Next(1000, 9999)}");
        }).ToList();
    }

    public static List<List<Contact>> GenerateGroupedContacts(int count)
    {
        return GenerateContacts(count)
            .GroupBy(c => c.Department)
            .OrderBy(g => g.Key)
            .Select(g => g.OrderBy(c => c.Name).ToList())
            .ToList();
    }

    public static List<Product> GenerateProducts(int count)
    {
        var rng = new Random(123);
        var adjectives = new[] { "Premium", "Classic", "Ultra", "Pro", "Slim", "Smart", "Eco", "Mini", "Max", "Elite" };
        var nouns = new[] { "Widget", "Gadget", "Device", "Tool", "Kit", "Pack", "Set", "Bundle", "System", "Unit" };

        return Enumerable.Range(0, count).Select(i =>
        {
            var cat = ProductCategories[rng.Next(ProductCategories.Length)];
            return new Product(
                $"{adjectives[rng.Next(adjectives.Length)]} {nouns[rng.Next(nouns.Length)]}",
                cat,
                $"${rng.Next(5, 500)}.{rng.Next(0, 99):D2}",
                ProductColors[rng.Next(ProductColors.Length)],
                $"A {cat.ToLower()} item with great features and excellent build quality.",
                Math.Round(3.0 + rng.NextDouble() * 2.0, 1));
        }).ToList();
    }

    public static List<ChatMessage> GenerateChatMessages(int count)
    {
        var rng = new Random(77);
        var phrases = new[]
        {
            "Hey, how's it going?", "Did you see the new update?", "Looks great! 👍",
            "Can we schedule a meeting?", "I'll send the docs over.", "Thanks for the quick response!",
            "Let me check and get back to you.", "Sure, sounds good!", "Almost done with the review.",
            "The build is passing now.", "I pushed the fix.", "LGTM, merging now.",
            "Can you take a look at this PR?", "Deployment went smoothly.",
            "Need help with this issue.", "On it! 🚀", "Great work everyone!",
            "The tests are all green.", "I'll handle the refactor.",
            "Let's discuss in standup.", "Happy Friday! 🎉"
        };

        var start = new DateTime(2025, 1, 15, 9, 0, 0);
        return Enumerable.Range(0, count).Select(i =>
        {
            var isMe = rng.NextDouble() > 0.45;
            return new ChatMessage(
                isMe ? "Me" : FirstNames[rng.Next(FirstNames.Length)],
                phrases[rng.Next(phrases.Length)],
                start.AddMinutes(i * rng.Next(1, 30)),
                isMe);
        }).ToList();
    }

    public static List<PhotoItem> GeneratePhotos(int count)
    {
        var rng = new Random(99);
        var subjects = new[]
        {
            "Mountain Sunrise", "City Lights", "Ocean Breeze", "Forest Path",
            "Desert Dunes", "Autumn Leaves", "Snow Peak", "River Bend",
            "Night Sky", "Spring Bloom", "Coastal Cliff", "Misty Valley",
            "Sunset Harbor", "Wildflower Field", "Tropical Beach"
        };
        var photographers = new[] { "A. Chen", "B. Davis", "C. Evans", "D. Flores", "E. Gonzalez" };
        var ratios = new[] { 0.75, 1.0, 1.2, 1.5, 0.66 };

        return Enumerable.Range(0, count).Select(i => new PhotoItem(
            subjects[rng.Next(subjects.Length)],
            photographers[rng.Next(photographers.Length)],
            PhotoColors[rng.Next(PhotoColors.Length)],
            ratios[rng.Next(ratios.Length)]
        )).ToList();
    }
}
