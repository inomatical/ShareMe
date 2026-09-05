# 18 — Retry failed outgoing items

**What to build:** When a single BearShare transfer fails, the user can retain the outgoing item temporarily, retry it after resolving the cause, or deliberately discard it.

**Blocked by:** 08 — Share plain text from BearShare.

**Status:** ready-for-agent

- [x] Network failure, receiver shutdown, and non-success responses retain the current outgoing item as failed.
- [x] Retry resubmits the retained item without requiring the originating Android share action.
- [x] Discard removes the failed item from BearShare's temporary outgoing state.
