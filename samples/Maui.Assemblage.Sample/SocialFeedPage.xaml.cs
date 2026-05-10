using System.Diagnostics;

namespace Maui.Assemblage.Sample;

// ─── Feed Post Models ───────────────────────────────────────────

public enum FeedPostKind { Text, Image, Quote, Poll }

public class FeedPost
{
    public FeedPostKind Kind { get; set; }
    public string DisplayName { get; set; } = "";
    public string Handle { get; set; } = "";
    public string Initials { get; set; } = "";
    public Color AvatarColor { get; set; } = Colors.Gray;
    public string TimeAgo { get; set; } = "";
    public string Body { get; set; } = "";
    public int Replies { get; set; }
    public int Reposts { get; set; }
    public int Likes { get; set; }

    // Image post fields
    public Color ImageColor { get; set; } = Colors.Transparent;
    public string ImageEmoji { get; set; } = "";
    public string ImageCaption { get; set; } = "";

    // Quote post fields
    public string QuoteName { get; set; } = "";
    public string QuoteHandle { get; set; } = "";
    public string QuoteInitials { get; set; } = "";
    public Color QuoteAvatarColor { get; set; } = Colors.Gray;
    public string QuoteBody { get; set; } = "";

    // Poll post fields
    public string PollOption1 { get; set; } = "";
    public string PollOption2 { get; set; } = "";
    public string PollOption3 { get; set; } = "";
    public string PollPct1 { get; set; } = "";
    public string PollPct2 { get; set; } = "";
    public string PollPct3 { get; set; } = "";
    public double PollBar1Width { get; set; }
    public double PollBar2Width { get; set; }
    public double PollBar3Width { get; set; }
    public string PollInfo { get; set; } = "";

    /// <summary>Estimated height for layout.</summary>
    public double EstimatedHeight { get; set; } = 160;
}

// ─── Template Selector ──────────────────────────────────────────

public class FeedTemplateSelector : DataTemplateSelector
{
    public DataTemplate? TextPostTemplate { get; set; }
    public DataTemplate? ImagePostTemplate { get; set; }
    public DataTemplate? QuotePostTemplate { get; set; }
    public DataTemplate? PollPostTemplate { get; set; }

    protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
    {
        if (item is FeedPost post)
        {
            return post.Kind switch
            {
                FeedPostKind.Image => ImagePostTemplate!,
                FeedPostKind.Quote => QuotePostTemplate!,
                FeedPostKind.Poll => PollPostTemplate!,
                _ => TextPostTemplate!,
            };
        }
        return TextPostTemplate!;
    }
}

// ─── Page ───────────────────────────────────────────────────────

public partial class SocialFeedPage : ContentPage
{
    private List<FeedPost> _posts = [];

    public SocialFeedPage()
    {
        InitializeComponent();

        var sw = Stopwatch.StartNew();
        _posts = GenerateFeed(80);
        var genTime = sw.ElapsedMilliseconds;

        FeedList.ItemExtentResolver = index =>
            index >= 0 && index < _posts.Count ? _posts[index].EstimatedHeight : 160;

        sw.Restart();
        FeedList.ItemsSource = _posts;
        sw.Stop();

        CountLabel.Text = $"{_posts.Count} posts · gen:{genTime}ms bind:{sw.ElapsedMilliseconds}ms";
    }

    private static List<FeedPost> GenerateFeed(int count)
    {
        var rng = new Random(2026);
        var names = new[]
        {
            ("Sarah Chen", "@sarahc", "SC", "#E74C3C"),
            ("Alex Rivera", "@arivera", "AR", "#3498DB"),
            ("Jordan Kim", "@jkim_dev", "JK", "#2ECC71"),
            ("Morgan Lee", "@morganlee", "ML", "#F39C12"),
            ("Casey Brooks", "@cbrooks", "CB", "#9B59B6"),
            ("Taylor Wang", "@twang", "TW", "#1ABC9C"),
            ("Riley Park", "@rileyp", "RP", "#E67E22"),
            ("Quinn Adams", "@qadams", "QA", "#2980B9"),
            ("Avery Patel", "@averypatel", "AP", "#27AE60"),
            ("Dakota Singh", "@dsingh", "DS", "#8E44AD"),
            ("Jamie Foster", "@jfoster", "JF", "#D35400"),
            ("Reese Turner", "@rturner", "RT", "#16A085"),
        };

        var textBodies = new[]
        {
            "Just shipped a new feature that reduced our API response time by 40%! 🚀 The key was moving to a streaming architecture with backpressure handling.",
            "Hot take: TypeScript's type system is actually a programming language in disguise. Template literal types are Turing complete and nobody can convince me otherwise.",
            "Spent the whole weekend refactoring our auth module. 200 files changed, 0 bugs introduced. That's what I call a productive weekend. 💪",
            "PSA: If your CI pipeline takes more than 10 minutes, you need to fix it. Developer experience matters.",
            "The best code is the code you don't write. Just deleted 3,000 lines of legacy middleware and everything still works perfectly.",
            "Attended an amazing talk on WebAssembly today. The future of cross-platform development is looking incredible. #wasmftw",
            "Debugging a race condition at 2am. Found it. It was a missing await. Always a missing await. 😅",
            "New blog post: \"Why I switched from microservices back to a monolith\" — sometimes simpler is better. Link in bio.",
            "TIL that CSS container queries are now supported in all major browsers. The web platform keeps getting better! 🎨",
            "Pair programming is underrated. Just had a 4-hour session and we solved a problem that had been open for 2 weeks.",
            "Controversial opinion: most design patterns are just workarounds for language limitations. Fight me.",
            "Finally got our test coverage to 95%. The last 5% is the hardest part. Is it even worth it? Discuss. 👇",
            "Our team just adopted trunk-based development and it's been a game changer. Smaller PRs, faster reviews, fewer conflicts.",
            "Remember: premature optimization is the root of all evil. But also, shipping a slow app is the root of user churn.",
            "Just discovered that our production database has been running without indexes on 3 key tables. For 6 months. We're fine. Everything is fine. 🔥",
        };

        var imageData = new[]
        {
            ("#264653", "🏔️", "Mountain summit at sunrise"),
            ("#E76F51", "🌅", "Golden hour at the beach"),
            ("#2A9D8F", "🌿", "New office plant setup"),
            ("#E9C46A", "☕", "Perfect pour-over morning"),
            ("#606C38", "🏕️", "Weekend camping trip"),
            ("#003049", "🌃", "City skyline after dark"),
            ("#D62828", "🎸", "New guitar day!"),
            ("#F4A261", "🍕", "Homemade Detroit-style pizza"),
        };

        var quoteData = new[]
        {
            ("Great thread on async patterns", "Elena Martinez", "@elenadev", "EM", "#C0392B",
                "Here's my guide to async/await patterns in .NET 10. Thread 🧵👇 1. Always use ConfigureAwait(false) in library code..."),
            ("This is exactly right", "Dev Community", "@devcom", "DC", "#2C3E50",
                "Reminder: code reviews are not about finding bugs. They're about sharing knowledge and maintaining standards."),
            ("Couldn't agree more", "Tech Insights", "@techinsights", "TI", "#8E44AD",
                "The #1 skill for senior developers isn't coding — it's knowing when NOT to build something."),
            ("100% this", "Open Source Daily", "@osdaily", "OS", "#27AE60",
                "Open source maintainers deserve way more recognition. Every npm install starts with someone's unpaid labor."),
        };

        var pollData = new[]
        {
            ("What's your preferred state management?", "Redux", "MobX", "Zustand", 35, 28, 37, 2847),
            ("Best language for backend in 2026?", "Rust", "Go", "C#", 32, 41, 27, 5102),
            ("How often do you write tests?", "Every PR", "Sometimes", "What tests?", 45, 38, 17, 3291),
            ("Tabs or spaces?", "Tabs", "Spaces", "Whatever IDE does", 29, 42, 29, 8830),
            ("Favorite IDE?", "VS Code", "JetBrains", "Neovim", 48, 31, 21, 6105),
        };

        var timeAgos = new[] { "2m", "5m", "12m", "28m", "1h", "2h", "3h", "5h", "8h", "12h", "1d", "2d" };

        var posts = new List<FeedPost>();

        for (int i = 0; i < count; i++)
        {
            var (name, handle, initials, colorHex) = names[rng.Next(names.Length)];
            var time = timeAgos[rng.Next(timeAgos.Length)];
            var replies = rng.Next(0, 120);
            var reposts = rng.Next(0, 80);
            var likes = rng.Next(1, 500);

            // Cycle through post types: ~40% text, ~25% image, ~20% quote, ~15% poll
            var roll = rng.NextDouble();
            FeedPostKind kind;
            if (roll < 0.40) kind = FeedPostKind.Text;
            else if (roll < 0.65) kind = FeedPostKind.Image;
            else if (roll < 0.85) kind = FeedPostKind.Quote;
            else kind = FeedPostKind.Poll;

            var post = new FeedPost
            {
                Kind = kind,
                DisplayName = name,
                Handle = handle,
                Initials = initials,
                AvatarColor = Color.FromArgb(colorHex),
                TimeAgo = time,
                Replies = replies,
                Reposts = reposts,
                Likes = likes,
            };

            switch (kind)
            {
                case FeedPostKind.Text:
                    post.Body = textBodies[rng.Next(textBodies.Length)];
                    break;

                case FeedPostKind.Image:
                {
                    var (imgColor, emoji, caption) = imageData[rng.Next(imageData.Length)];
                    var bodyText = textBodies[rng.Next(textBodies.Length)];
                    post.Body = bodyText[..Math.Min(80, bodyText.Length)] + "...";
                    post.ImageColor = Color.FromArgb(imgColor);
                    post.ImageEmoji = emoji;
                    post.ImageCaption = caption;
                    break;
                }

                case FeedPostKind.Quote:
                {
                    var (body, qName, qHandle, qInitials, qColor, qBody) = quoteData[rng.Next(quoteData.Length)];
                    post.Body = body;
                    post.QuoteName = qName;
                    post.QuoteHandle = qHandle;
                    post.QuoteInitials = qInitials;
                    post.QuoteAvatarColor = Color.FromArgb(qColor);
                    post.QuoteBody = qBody;
                    break;
                }

                case FeedPostKind.Poll:
                {
                    var (question, o1, o2, o3, p1, p2, p3, votes) = pollData[rng.Next(pollData.Length)];
                    const double maxBarWidth = 220;
                    var maxPct = Math.Max(p1, Math.Max(p2, p3));
                    post.Body = question;
                    post.PollOption1 = o1;
                    post.PollOption2 = o2;
                    post.PollOption3 = o3;
                    post.PollPct1 = $"{p1}%";
                    post.PollPct2 = $"{p2}%";
                    post.PollPct3 = $"{p3}%";
                    post.PollBar1Width = maxBarWidth * p1 / maxPct;
                    post.PollBar2Width = maxBarWidth * p2 / maxPct;
                    post.PollBar3Width = maxBarWidth * p3 / maxPct;
                    post.PollInfo = $"{votes:N0} votes · {rng.Next(1, 24)}h left";
                    break;
                }
            }

            // Estimate height based on post type and content
            // Grid padding: 12+12=24, author row: ~20, spacing: 6 each, action bar: ~24
            var bodyLines = Math.Max(1, (int)Math.Ceiling(post.Body.Length / 38.0));
            var bodyHeight = Math.Min(bodyLines, kind == FeedPostKind.Text ? 6 : 3) * 19.0;

            post.EstimatedHeight = kind switch
            {
                // padding(24) + author(20) + sp(6) + body + sp(6) + actions(24) + border margin(1)
                FeedPostKind.Text => 81 + bodyHeight,
                // + image(160) + sp(6) + image margin(4)
                FeedPostKind.Image => 91 + bodyHeight + 160,
                // + quoted card(~80) + sp(6) + card margin(4)
                FeedPostKind.Quote => 91 + bodyHeight + 90,
                // + poll options(3×36=108) + sp(6×3=18) + info(16) + sp(6)
                FeedPostKind.Poll => 91 + bodyHeight + 148,
                _ => 150,
            };

            posts.Add(post);
        }

        return posts;
    }
}
