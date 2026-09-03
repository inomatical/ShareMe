# 16 — Share files from BearShare

**What to build:** An Android user can share one file through BearShare and sees delivery success or failure after EagleShare completes or rejects durable persistence.

**Blocked by:** 05 — Establish BearShare target configuration; 08 — Share plain text from BearShare; 14 — Receive file uploads in EagleShare.

**Status:** ready-for-agent

- [ ] Android's standard share sheet offers BearShare for supported files.
- [ ] BearShare sends one file using the agreed multipart contract without loading the whole file into memory.
- [ ] EagleShare's durable completion controls the success result shown to the user.
