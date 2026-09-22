namespace LittleHelper.Domain.Users;

// Only Telegram is live in Phase 1 (see docs/analysis-phase.md decision #1).
// Wire value from the bot is lowercase ("telegram") — parsed case-insensitively.
public enum PlatformType
{
    Telegram,
}
