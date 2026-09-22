import type { PlatformType } from "../../../shared-kernel/PlatformType.js";

// Body of POST /internal/notify — how the .NET backend asks the bot to
// proactively deliver something (a due reminder, etc.) to a specific user.
// Not a reply to any IncomingMessage; there may be no inbound message at all.
export interface PushNotification {
  platform: PlatformType;
  platformUserId: string;
  text: string;
}
