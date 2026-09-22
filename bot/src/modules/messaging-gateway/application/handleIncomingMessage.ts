import type { BackendGateway } from "../domain/BackendGateway.js";
import type { MessageHandler } from "../domain/PlatformAdapter.js";

const FALLBACK_REPLY = "Sorry, I couldn't reach the backend just now — please try again shortly.";

// The one handler every platform adapter calls. It does no interpreting of
// its own: identity resolution, command parsing, and every feature (cycle
// tracking, reminders, ...) live in the .NET backend now. This just forwards
// and relays the reply, catching backend failures so a down/missing .NET API
// degrades to a friendly message instead of an unhandled rejection.
export function createIncomingMessageHandler(backend: BackendGateway): MessageHandler {
  return async (message) => {
    try {
      const response = await backend.handleMessage({
        platform: message.platform,
        platformUserId: message.platformUserId,
        displayName: message.displayName,
        text: message.text,
      });
      return { text: response.text };
    } catch (err) {
      console.error("Backend call failed:", err);
      return { text: FALLBACK_REPLY };
    }
  };
}
