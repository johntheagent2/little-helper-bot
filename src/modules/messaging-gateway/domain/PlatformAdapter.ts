import type { FastifyInstance } from "fastify";
import type { PlatformType } from "../../../shared-kernel/PlatformType.js";
import type { IncomingMessage } from "./IncomingMessage.js";
import type { OutgoingReply } from "./OutgoingReply.js";

// Every platform adapter (Telegram today, Discord/whatever later) calls this
// for each inbound message and sends the result back in its own format. This
// is the "shared handler layer" from requirements.md — the one place message
// handling logic lives, so multi-platform parity is automatic rather than
// something each adapter has to reimplement.
export type MessageHandler = (message: IncomingMessage) => Promise<OutgoingReply>;

// Port. One implementation per platform, each living in its own src/platform/<name>
// folder. server.ts only knows about this interface, never a specific platform's SDK.
export interface PlatformAdapter {
  readonly platform: PlatformType;

  // Wire up whatever this platform needs (a Fastify route for webhook-style
  // platforms, for example) and call `handleMessage` for every inbound message.
  registerRoutes(app: FastifyInstance, handleMessage: MessageHandler): Promise<void>;

  // Tell the platform where to deliver events, if that's done at runtime
  // (Telegram's setWebhook). Platforms configured out-of-band (e.g. Discord's
  // Interactions Endpoint URL, set in its developer portal) can no-op here and
  // just log what the operator needs to paste in manually.
  configureWebhook(publicUrl: string): Promise<void>;

  // Proactive delivery, not in response to an inbound message — used by the
  // /internal/notify route so the backend can push things like due reminders.
  // platformUserId is whatever this adapter put in IncomingMessage.platformUserId.
  sendMessage(platformUserId: string, text: string): Promise<void>;
}
