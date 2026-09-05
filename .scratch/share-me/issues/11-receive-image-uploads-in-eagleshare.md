# 11 — Receive image uploads in EagleShare

**What to build:** EagleShare streams an image to durable collision-safe storage, shows incoming progress, and creates a selected image inbox item only when complete.

**Blocked by:** 03 — Establish EagleShare local storage; 04 — Establish EagleShare receiver lifecycle; 06 — Establish transfer contract test harness.

**Status:** done

- [x] Image bytes are streamed into incoming storage and do not become an inbox item until finalized.
- [x] A completed item retains its original filename and available MIME type while using collision-safe stored content.
- [x] The main window shows incoming transfer progress and selects the completed image.

**Verification:** `dotnet test ShareMe.slnx --no-restore`
