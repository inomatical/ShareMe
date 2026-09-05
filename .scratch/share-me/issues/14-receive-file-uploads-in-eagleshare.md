# 14 — Receive file uploads in EagleShare

**What to build:** EagleShare streams a file to durable storage, retains original filename, MIME type, and size metadata, and exposes the completed file in its inbox.

**Blocked by:** 03 — Establish EagleShare local storage; 04 — Establish EagleShare receiver lifecycle; 06 — Establish transfer contract test harness.

**Status:** ready-for-agent

- [x] File data is streamed and finalized before it appears as an inbox item.
- [x] The completed item records original filename, available MIME type, local path, and size.
- [x] Equal original filenames never overwrite each other's stored content.
