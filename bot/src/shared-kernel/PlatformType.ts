// Only "telegram" is live in Phase 1. Add "discord" here (and nowhere else in
// shared/application code — see modules/messaging-gateway) once a Discord
// adapter is actually built. See docs/analysis-phase.md decision #1.
export enum PlatformType {
  TELEGRAM = "telegram",
}
