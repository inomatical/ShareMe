# 06 — Establish transfer contract test harness

**What to build:** EagleShare has executable contract tests for its public multipart `POST /api/items` receiver behavior, protecting the cross-application seam before BearShare depends on it.

**Blocked by:** 03 — Establish EagleShare local storage; 04 — Establish EagleShare receiver lifecycle.

**Status:** done

- [x] Tests can submit a multipart request to a running EagleShare receiver and observe its HTTP response.
- [x] Tests can observe and distinguish clear client and server error responses at the public receiver boundary.

**Verification:** `dotnet test tests/EagleShare.Tests/EagleShare.Tests.csproj --filter FullyQualifiedName~EagleShareReceiverContractTests`
