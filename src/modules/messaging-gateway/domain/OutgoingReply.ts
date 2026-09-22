// Plain text out of the shared handler layer; each adapter decides how to
// render it in its own platform's format (Telegram Markdown, Discord embed, ...).
export interface OutgoingReply {
  text: string;
}
