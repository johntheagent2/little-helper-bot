import type { PlatformType } from "../../../shared-kernel/PlatformType.js";

// What every platform adapter normalizes its native event into before handing
// off. Nothing downstream of this type should need to know which platform a
// message came from beyond the `platform` tag itself.
export interface IncomingMessage {
  platform: PlatformType;
  platformUserId: string;
  displayName: string | null;
  text: string;
}
