import type { PlatformType } from "../../../shared-kernel/PlatformType.js";

// Everything an inbound message carries to the .NET backend. The backend, not
// the bot, resolves identity and decides what the message means — the bot
// never stores or interprets any of this beyond forwarding it.
export interface BackendMessageRequest {
  platform: PlatformType;
  platformUserId: string;
  displayName: string | null;
  text: string;
}

export interface BackendMessageResponse {
  text: string;
}

// Port. The one door the bot has into the .NET backend for inbound traffic.
// Implemented by infrastructure/HttpBackendGateway — nothing outside this
// module should call the backend directly.
export interface BackendGateway {
  handleMessage(request: BackendMessageRequest): Promise<BackendMessageResponse>;
}
