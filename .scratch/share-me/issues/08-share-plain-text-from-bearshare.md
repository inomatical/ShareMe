# 08 — Share plain text from BearShare

**What to build:** BearShare appears in Android's text share flow, sends selected text to configured open EagleShare, and shows a compact success or failure result.

**Blocked by:** 05 — Establish BearShare target configuration; 07 — Receive and copy text in EagleShare.

**Status:** done

- [x] Android's standard share sheet offers BearShare for plain text.
- [x] A configured target receives the shared text as a selected EagleShare inbox item.
- [x] An unreachable target produces a clear failed delivery status.

**Verification:** `android\BearShare\gradlew.bat -p android\BearShare testDebugUnitTest`
