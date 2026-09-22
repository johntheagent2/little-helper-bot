import { Telegraf } from "telegraf";
import type { Update } from "telegraf/types";
import type { FastifyInstance, FastifyRequest } from "fastify";
import type { PlatformAdapter, MessageHandler } from "../../modules/messaging-gateway/domain/PlatformAdapter.js";
import { PlatformType } from "../../shared-kernel/PlatformType.js";
import { mapToIncomingMessage } from "./mapUpdate.js";

const SECRET_HEADER = "x-telegram-bot-api-secret-token";

export interface TelegramAdapterOptions {
  botToken: string;
  webhookSecret: string;
  webhookPath?: string;
}

export class TelegramAdapter implements PlatformAdapter {
  readonly platform = PlatformType.TELEGRAM;

  private readonly bot: Telegraf;
  private readonly webhookSecret: string;
  private readonly webhookPath: string;

  constructor(options: TelegramAdapterOptions) {
    this.bot = new Telegraf(options.botToken);
    this.webhookSecret = options.webhookSecret;
    this.webhookPath = options.webhookPath ?? "/webhook/telegram";
  }

  async registerRoutes(app: FastifyInstance, handleMessage: MessageHandler): Promise<void> {
    this.bot.on("text", async (ctx) => {
      const incoming = mapToIncomingMessage(ctx);
      if (!incoming) {
        return;
      }

      const reply = await handleMessage(incoming);
      await ctx.reply(reply.text);
    });

    // Fastify already parses the JSON body, so we call bot.handleUpdate()
    // directly instead of Telegraf's own webhookCallback (which expects to
    // own the raw request stream). The secret token is validated by hand
    // for the same reason.
    app.post(this.webhookPath, async (request: FastifyRequest, reply) => {
      const providedSecret = request.headers[SECRET_HEADER];
      if (providedSecret !== this.webhookSecret) {
        return reply.code(401).send();
      }

      await this.bot.handleUpdate(request.body as Update);
      return reply.code(200).send();
    });
  }

  async configureWebhook(publicUrl: string): Promise<void> {
    const webhookUrl = `${publicUrl}${this.webhookPath}`;
    await this.bot.telegram.setWebhook(webhookUrl, { secret_token: this.webhookSecret });
  }

  async sendMessage(platformUserId: string, text: string): Promise<void> {
    // Private chats only: for a 1:1 DM, Telegram's chat id equals the user's
    // id, which is what platformUserId already is (see mapUpdate.ts). This
    // breaks if the bot is ever added to a group, where they diverge.
    await this.bot.telegram.sendMessage(platformUserId, text);
  }
}
