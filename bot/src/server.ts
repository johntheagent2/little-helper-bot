import { config } from "dotenv";
import Fastify from "fastify";
import { createIncomingMessageHandler } from "./modules/messaging-gateway/application/handleIncomingMessage.js";
import { HttpBackendGateway } from "./modules/messaging-gateway/infrastructure/HttpBackendGateway.js";
import type { PlatformAdapter } from "./modules/messaging-gateway/domain/PlatformAdapter.js";
import type { PushNotification } from "./modules/messaging-gateway/domain/PushNotification.js";
import { TelegramAdapter } from "./platform/telegram/TelegramAdapter.js";

config({ path: ".env.local" });

const {
  TELEGRAM_BOT_TOKEN,
  TELEGRAM_WEBHOOK_SECRET,
  BACKEND_API_BASE_URL,
  BACKEND_API_KEY,
  BACKEND_PUSH_SECRET,
  PUBLIC_URL,
  PORT = "3000",
} = process.env;

if (!TELEGRAM_BOT_TOKEN) {
  throw new Error("TELEGRAM_BOT_TOKEN is required");
}
if (!TELEGRAM_WEBHOOK_SECRET) {
  throw new Error("TELEGRAM_WEBHOOK_SECRET is required");
}
if (!BACKEND_API_BASE_URL) {
  throw new Error("BACKEND_API_BASE_URL is required");
}
if (!BACKEND_PUSH_SECRET) {
  throw new Error("BACKEND_PUSH_SECRET is required");
}

async function main(): Promise<void> {
  const backend = new HttpBackendGateway({
    baseUrl: BACKEND_API_BASE_URL!,
    apiKey: BACKEND_API_KEY,
  });
  const handleMessage = createIncomingMessageHandler(backend);

  // Add a DiscordAdapter (or anything else implementing PlatformAdapter) here
  // later — nothing else in this file, or in handleIncomingMessage, has to change.
  const adapters: PlatformAdapter[] = [
    new TelegramAdapter({
      botToken: TELEGRAM_BOT_TOKEN!,
      webhookSecret: TELEGRAM_WEBHOOK_SECRET!,
    }),
  ];

  const app = Fastify({ logger: true });

  for (const adapter of adapters) {
    await adapter.registerRoutes(app, handleMessage);
  }

  // The other direction: the .NET backend calls this to have the bot deliver
  // something the user didn't ask for right now (a due reminder, etc.). Only
  // the backend should ever call it, hence the shared-secret check.
  app.post("/internal/notify", async (request, reply) => {
    if (request.headers["x-push-secret"] !== BACKEND_PUSH_SECRET) {
      return reply.code(401).send();
    }

    const { platform, platformUserId, text } = request.body as PushNotification;
    const adapter = adapters.find((a) => a.platform === platform);
    if (!adapter) {
      return reply.code(400).send({ error: `Unknown platform: ${platform}` });
    }

    await adapter.sendMessage(platformUserId, text);
    return reply.code(200).send();
  });

  app.get("/health", async () => ({ status: "ok" }));

  const port = Number(PORT);
  await app.listen({ port, host: "0.0.0.0" });

  if (PUBLIC_URL) {
    for (const adapter of adapters) {
      await adapter.configureWebhook(PUBLIC_URL);
      app.log.info(`${adapter.platform} webhook configured against ${PUBLIC_URL}`);
    }
  } else {
    app.log.warn("PUBLIC_URL not set — skipping webhook configuration. Set it once your tunnel is up and restart.");
  }
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
