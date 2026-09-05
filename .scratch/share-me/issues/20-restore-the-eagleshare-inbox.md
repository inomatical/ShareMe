# 20 — Restore the EagleShare inbox

**What to build:** After EagleShare or Windows restarts, all completed text, URL, image, and file items return from durable content and metadata to the inbox.

**Blocked by:** 09 — Receive and copy URLs in EagleShare; 11 — Receive image uploads in EagleShare; 14 — Receive file uploads in EagleShare.

**Status:** ready-for-agent

- [x] Restarting EagleShare rebuilds the inbox with completed items of every supported kind.
- [x] Restored item metadata and stored content remain usable for their type-specific actions.
- [x] Partial incoming uploads never appear in the restored inbox.
