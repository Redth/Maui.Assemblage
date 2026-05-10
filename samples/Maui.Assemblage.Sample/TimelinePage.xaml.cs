namespace Maui.Assemblage.Sample;

public partial class TimelinePage : ContentPage
{
    public TimelinePage()
    {
        InitializeComponent();

        var events = new[]
        {
            new TimelineEvent("Jan 5", "Project Kickoff", "Initial planning meeting with stakeholders. Defined scope and timeline.", "#E74C3C"),
            new TimelineEvent("Jan 12", "Design Sprint", "UX wireframes and visual design completed for core flows.", "#3498DB"),
            new TimelineEvent("Jan 20", "Backend API v1", "REST endpoints for auth, users, and data models deployed to staging.", "#2ECC71"),
            new TimelineEvent("Feb 1", "Frontend Prototype", "React app scaffolded with routing, state management, and component library.", "#9B59B6"),
            new TimelineEvent("Feb 10", "Database Migration", "Migrated from SQLite to PostgreSQL. Schema v2 with indexes optimized.", "#F39C12"),
            new TimelineEvent("Feb 18", "Alpha Release", "Internal alpha deployed. 12 testers onboarded for feedback cycle.", "#E74C3C"),
            new TimelineEvent("Feb 25", "Performance Audit", "Lighthouse score improved from 62 to 94. Bundle size reduced 40%.", "#1ABC9C"),
            new TimelineEvent("Mar 3", "Security Review", "Penetration testing completed. 3 medium issues fixed, 0 critical.", "#E67E22"),
            new TimelineEvent("Mar 10", "Beta Launch", "Public beta with 500 signups. Crash-free rate: 99.2%.", "#2980B9"),
            new TimelineEvent("Mar 18", "Analytics Integration", "Mixpanel events and funnels configured. A/B test framework ready.", "#8E44AD"),
            new TimelineEvent("Mar 25", "Load Testing", "Sustained 10K concurrent users. P99 latency: 180ms.", "#27AE60"),
            new TimelineEvent("Apr 1", "iOS App Submitted", "App Store review submitted. TestFlight beta updated.", "#C0392B"),
            new TimelineEvent("Apr 8", "Android Launch", "Play Store listing published. 1,000 installs first week.", "#2ECC71"),
            new TimelineEvent("Apr 15", "v1.0 GA Release", "General availability. All platforms live. Press release distributed.", "#F1C40F"),
            new TimelineEvent("Apr 22", "Post-Launch Fixes", "Hotfix for OAuth token refresh. Memory leak in image cache resolved.", "#E74C3C"),
            new TimelineEvent("May 1", "v1.1 Planning", "Feature requests triaged. Roadmap for Q2 finalized with team.", "#3498DB"),
            new TimelineEvent("May 10", "Dark Mode", "Full dark theme support across all platforms. Dynamic switching.", "#34495E"),
            new TimelineEvent("May 18", "Offline Support", "Service worker caching. IndexedDB sync for offline-first experience.", "#16A085"),
            new TimelineEvent("May 25", "i18n Complete", "12 languages supported. RTL layout tested on Arabic and Hebrew.", "#D35400"),
            new TimelineEvent("Jun 2", "v1.2 Released", "Offline mode, dark theme, and localization shipped to production.", "#2ECC71"),
        };

        TimelineList.ItemsSource = events;
    }
}

public record TimelineEvent(string Date, string Title, string Description, string ColorHex)
{
    public Color Color => Color.FromArgb(ColorHex);
}
