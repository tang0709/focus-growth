# Focus & Growth development conventions

- Maintain this repository and the stable Pages URL https://tang0709.github.io/focus-growth/. Update index.html rather than creating versioned HTML copies.
- Read the latest main branch before changing code. Do not force-push or overwrite concurrent changes.
- Core goal: reduce startup friction. Preserve the three-column layout, top long-term project tracking, four state-specific component collections, customizable soft theme, and vertical midnight-to-midnight timeline with plan/actual/compare and calendar views.
- Task completion is manually confirmed. Task elapsed time updates in seconds and supports pause/resume/stop/complete. Computer activity statistics use fixed 10-minute intervals with at least one input event; never equate elapsed time with activity or task completion.
- Preserve localStorage and JSON import/export compatibility. Implement schema migrations for breaking data changes; never silently discard history.
- Global Windows monitoring, system tray, AI API backend, AI planning and direct chat-to-data sync are future features; distinguish working features from placeholders.
- Do not commit actual personal tasks, work records, API keys, backups, or private databases. Use synthetic demo data only. Keep secrets in a local backend, never public JavaScript.
- Test affected functionality and update CHANGELOG.md for each release. Preserve existing features unless the user requests otherwise.
