# 23 — Enforce receiver completion and lifecycle rules

**What to build:** EagleShare receives only while its main window is open, reports malformed, unavailable, and persistence-failure requests clearly, and never exposes partial uploads as completed inbox items.

**Blocked by:** 09 — Receive and copy URLs in EagleShare; 11 — Receive image uploads in EagleShare; 14 — Receive file uploads in EagleShare; 20 — Restore the EagleShare inbox.

**Status:** ready-for-agent

- [x] Malformed requests, failed disk writes, and unavailable receiver state return clear non-success HTTP responses.
- [x] Interrupted binary uploads leave no completed item and no restored inbox item after restart.
- [x] A success response is returned only after the content and its metadata are durably persisted.
- [x] Closing EagleShare prevents all new receiver activity without a tray or background mode.