# 17 — Send multi-file shares sequentially

**What to build:** An Android user can select multiple files, and BearShare delivers exactly one item at a time while displaying a separate outcome for every item.

**Blocked by:** 16 — Share files from BearShare.

**Status:** ready-for-agent

- [x] Android's multi-file sharing flow offers BearShare for supported selections.
- [x] BearShare does not start the next item until the current item returns a successful completion response.
- [x] Every item has its own visible completed or failed result.
