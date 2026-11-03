# Copy Paste — Analytics Framework
**Studio:** Kajo Games  
**Game:** Copy Paste (PC / Steam)  
**SDK:** GameAnalytics Unity SDK `8.3.1`  
**Namespace:** `KajoGames.CopyPaste.Analytics`

---

## Documentation

The primary documentation and onboarding guides have been organized in the `Documentation/` folder:

1. 📖 **[Team Analytics Guide (ANALYTICS_GUIDE.md)](Documentation/ANALYTICS_GUIDE.md)**:
   - Full onboarding guide for programmers, junior developers, artists, and designers.
   - Explains the event lifecycle, architecture, and class relationships with editable Mermaid diagrams.
   - 10 code-based integration workflows with copy-paste examples.
   - No-code Inspector integration steps for artists using `AnalyticsTracker`.
   - Event naming rules, debugging techniques, and team best practices.
   - One-page quick reference cheat sheet.

2. 📋 **[Event Catalog (EVENT_CATALOG.md)](Documentation/EVENT_CATALOG.md)**:
   - Complete matrix of all implemented and suggested telemetry events in *Copy Paste*.
   - Specifies categories, parameters, trigger locations, and responsible features.

---

## Directory Structure

```
Assets/Scripts/Analytics/
├── Documentation/
│   ├── ANALYTICS_GUIDE.md            # Comprehensive team guide & Mermaid diagrams
│   └── EVENT_CATALOG.md              # Matrix of implemented & suggested events
├── Runtime/
│   ├── GameAnalyticsManager.cs       # Central singleton facade & dispatcher
│   ├── IAnalyticsEvent.cs            # Strongly-typed event contract
│   ├── AnalyticsEventData.cs         # Event data container, formatting & validation
│   ├── AnalyticsEventCategory.cs     # Category enum & domain tags
│   ├── AnalyticsSettings.cs          # ScriptableObject configuration
│   ├── AnalyticsTracker.cs           # Inspector component for no-code telemetry
│   ├── AnalyticsExtensions.cs        # Fluent UI (Button, Slider, Toggle) extensions
│   ├── AnalyticsChannelListener.cs   # Bridge for VoidEventChannelSO and IntEventChannelSO
│   └── Helpers/
│       ├── GameplayAnalytics.cs      # Combat, weapons, cloning, deaths, gates, pickups
│       ├── UIAnalytics.cs            # Buttons, panels, volume, multiplayer lobby
│       ├── ProgressionAnalytics.cs   # Level starts, completions, fails, tutorial, retries
│       └── EconomyAnalytics.cs       # Virtual currency sink/source, item unlocks
└── Editor/
    ├── AnalyticsTrackerEditor.cs     # Custom Inspector with live validation
    ├── AnalyticsSettingsEditor.cs    # Settings inspector with SDK shortcuts
    └── AnalyticsMenuItems.cs         # Tools > Analytics editor menu utilities
```

---

## Quick Code Example

```csharp
using KajoGames.CopyPaste.Analytics;
using KajoGames.CopyPaste.Analytics.Helpers;

// Track simple event
GameAnalyticsManager.Event("combat", "weapon_fired");

// Track with numeric value
GameAnalyticsManager.Event("combat", "damage_dealt", "machine_gun", 25.5f);

// Track progression
ProgressionAnalytics.TrackLevelCompleted("Level_1", timeSeconds: 42.0f, retries: 2);

// Track gameplay action
GameplayAnalytics.TrackPlayerDeath("clone_collision", "Level_1", retryCount: 2);
```
