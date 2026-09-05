# 07 — Receive and copy text in EagleShare

**What to build:** An open EagleShare window accepts a valid text upload, persists and selects it in the inbox, previews its literal text, and copies it to the Windows clipboard.

**Blocked by:** 03 — Establish EagleShare local storage; 04 — Establish EagleShare receiver lifecycle; 06 — Establish transfer contract test harness.

**Status:** done

- [x] A valid text request produces one durable, selected text inbox item only after persistence completes.
- [x] The selected item previews and copies the literal received UTF-8 text.
- [x] The receiver returns success only after the completed item is durable.

**Verification:** `dotnet test ShareMe.slnx`
