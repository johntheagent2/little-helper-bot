using LittleHelper.Domain.Users;

namespace LittleHelper.Domain.Messaging;

// Mirrors bot/src/modules/messaging-gateway/domain/BackendGateway.ts's
// BackendMessageRequest exactly — this is the one contract both sides
// hand-wrote independently (see docs/analysis-phase.md decision #6).
public sealed record IncomingMessage(PlatformType Platform, string PlatformUserId, string? DisplayName, string Text);
