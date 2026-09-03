# 19 — Handle failed multi-file deliveries

**What to build:** A mixed-result file batch clearly identifies failed items, retains only failures for retry or discard, and preserves independent completed results.

**Blocked by:** 17 — Send multi-file shares sequentially; 18 — Retry failed outgoing items.

**Status:** ready-for-agent

- [ ] A batch reports success and failure separately for each item.
- [ ] Failed batch items remain individually retryable or discardable after the batch completes.
- [ ] Successfully delivered items are not retransmitted when a failed item is retried.
