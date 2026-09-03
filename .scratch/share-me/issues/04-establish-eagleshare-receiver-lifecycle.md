# 04 — Establish EagleShare receiver lifecycle

**What to build:** Opening EagleShare starts a configurable local HTTP listener and shows its current LAN IP address and port; closing the main window stops the listener.

**Blocked by:** 01 — Create EagleShare desktop application.

**Status:** ready-for-agent

- [ ] The open main window visibly presents a usable LAN address and listening port, including the default port.
- [ ] Closing the main window makes the local receiver unavailable and does not leave a background receiver running.
- [ ] Private-network Windows Firewall permission is supported as the expected first-listen operating path.
