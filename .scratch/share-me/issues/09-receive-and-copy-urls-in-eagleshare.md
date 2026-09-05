# 09 — Receive and copy URLs in EagleShare

**What to build:** EagleShare accepts a URL as a distinct durable inbox item with literal preview and clipboard copy.

**Blocked by:** 07 — Receive and copy text in EagleShare.

**Status:** done

- [x] A valid URL request produces a distinct URL inbox item with durable metadata and stored content.
- [x] Preview and clipboard actions preserve the literal URL value.

**Verification:** `dotnet test ShareMe.slnx --no-restore`
