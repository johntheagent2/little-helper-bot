import type { Context } from "telegraf";
import type { IncomingMessage } from "../../modules/messaging-gateway/domain/IncomingMessage.js";
import { PlatformType } from "../../shared-kernel/PlatformType.js";

export function mapToIncomingMessage(ctx: Context): IncomingMessage | null {
  const message = ctx.message;
  if (!message || !("text" in message) || !ctx.from) {
    return null;
  }

  return {
    platform: PlatformType.TELEGRAM,
    platformUserId: String(ctx.from.id),
    displayName: ctx.from.first_name ?? ctx.from.username ?? null,
    text: message.text,
  };
}
