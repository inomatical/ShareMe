# Share Me MVP Feature Specification

## Problem Statement

People need a quick way to move content selected in an Android application's
standard share sheet onto their Windows PC without using a cloud service,
account, pairing flow, or an intermediary web service. They need the content to
arrive reliably in a desktop inbox, persist across restarts, and remain
available for the action that suits its type.

## Solution

BearShare is an Android share target that sends selected text, URLs, images,
files, and groups of files directly over a trusted local Wi-Fi/LAN to an open
EagleShare Windows desktop application. EagleShare exposes a local HTTP upload
endpoint, streams each binary upload to persistent storage, records inbox
metadata, and displays the received item in its active window.

The transfer contract is `POST /api/items` with `multipart/form-data`. Each
request carries one item, including its kind, content, and available metadata.
BearShare submits multi-item shares sequentially and treats EagleShare's
successful completion response as confirmation that the item is durable.

### Test Seam Assumption

The primary and highest cross-application test seam is EagleShare's public
local HTTP endpoint. Tests should exercise it as a client would, then observe
the persisted inbox and user-visible result. This specification assumes that
this seam matches the desired boundary; no new integration seam is required.

## User Stories

1. As an Android user, I want BearShare to appear in the standard share sheet for plain text, so that I can send copied or composed text to my PC.
2. As an Android user, I want BearShare to appear when sharing a URL, so that I can move a web address to my PC without retyping it.
3. As an Android user, I want BearShare to appear when sharing an image, so that I can transfer photos and screenshots directly to EagleShare.
4. As an Android user, I want BearShare to appear when sharing a file, so that I can transfer a document or other attachment to my PC.
5. As an Android user, I want BearShare to accept multiple selected files, so that I can transfer a batch in one sharing action.
6. As an Android user, I want to enter EagleShare's LAN address and port in BearShare, so that I can choose the PC that receives my content.
7. As an Android user, I want BearShare to retain one editable target address, so that changing PCs or a changed PC address is straightforward.
8. As an Android user, I want the target-address field to be easy to edit, so that I can correct an address without navigating through settings.
9. As an Android user, I want BearShare to send each shared item directly to EagleShare over HTTP, so that no cloud service or account is involved.
10. As an Android user, I want each item in a multi-item share sent only after the previous item succeeds, so that I receive independent, ordered delivery outcomes.
11. As an Android user, I want to see a compact success result for every shared item, so that I know what reached my PC.
12. As an Android user, I want to see a clear failure result when EagleShare cannot be reached or rejects an item, so that I know delivery did not complete.
13. As an Android user, I want failed outgoing content retained temporarily, so that I can retry it without repeating the originating share action.
14. As an Android user, I want an explicit retry action for a failed item, so that I can deliver it after reopening EagleShare or correcting its address.
15. As an Android user, I want to discard a failed item, so that I can remove an item I no longer need to send.
16. As a Windows user, I want EagleShare to start its local receiver when its main window opens, so that my PC can accept content while I am using the application.
17. As a Windows user, I want EagleShare to stop receiving content when its main window closes, so that it never receives content in the background.
18. As a Windows user, I want EagleShare to show its current LAN IP address and listening port, so that I can configure BearShare correctly.
19. As a Windows user, I want EagleShare to communicate its default listening port in the UI, so that I can identify the address BearShare needs.
20. As a Windows user, I want EagleShare to accept one well-formed multipart request as one inbox item, so that every delivery has a distinct inbox record.
21. As a Windows user, I want binary uploads streamed to disk, so that large files do not require loading their entire contents into memory.
22. As a Windows user, I want EagleShare to acknowledge an upload only after it and its metadata are persisted, so that a BearShare success result is meaningful.
23. As a Windows user, I want malformed uploads, disk-write failures, and unavailable receiver states to receive clear HTTP errors, so that BearShare can present a meaningful failure.
24. As a Windows user, I want partially uploaded binary content held outside the completed inbox, so that incomplete transfers are not shown as received items.
25. As a Windows user, I want every completed item stored under my Documents EagleShare storage area, so that my received content survives application and Windows restarts.
26. As a Windows user, I want a persistent EagleShare inbox reconstructed from local metadata after restart, so that I can continue working with previously received items.
27. As a Windows user, I want collision-proof stored filenames, so that files with the same original name do not overwrite one another.
28. As a Windows user, I want original filenames preserved as display metadata, so that I can identify the source file even when the stored filename differs.
29. As a Windows user, I want EagleShare to record its own receipt time, so that inbox ordering and display use an authoritative local timestamp.
30. As a Windows user, I want incoming transfer progress displayed, so that I can see that a large item is actively being received.
31. As a Windows user, I want EagleShare to foreground itself and select a newly received item, so that I can immediately use it.
32. As a Windows user, I want a persistent inbox list and preview area, so that I can browse received content and inspect a selected item.
33. As a Windows user, I want literal text preview and a copy action, so that I can paste received text into another Windows application.
34. As a Windows user, I want literal URL preview and a copy action, so that I can reuse a received web address without alteration.
35. As a Windows user, I want image thumbnails and full previews, so that I can identify received images before saving them.
36. As a Windows user, I want a Save As action for images, so that I can put an image in a location I choose.
37. As a Windows user, I want filename, type, and size shown for files, so that I can identify received attachments.
38. As a Windows user, I want a Save As action for files, so that I can put an attachment in a location I choose.
39. As a Windows user, I want text and URL records retained as files in EagleShare storage, so that they are durable alongside binary items.
40. As a Windows user, I want to deliberately delete a selected item with confirmation, so that I can reclaim storage without accidental data loss.
41. As a Windows user, I want deletion to remove the inbox entry, persisted metadata, and stored content, so that deleted items do not remain behind.
42. As a privacy-conscious home user, I want no accounts, pairing, tokens, or cloud relay in the MVP, so that sharing remains local and simple.
43. As a home-network user, I want to use plain HTTP only on a trusted LAN, so that the MVP avoids unnecessary setup while clearly limiting its security scope.

## Implementation Decisions

- Deliver two MVP applications: BearShare, a Kotlin Android APK, and EagleShare, a Windows 10/11 WPF application written in C#. DolphineShare is excluded; there is no REST relay or cloud component.
- BearShare registers Android share intent support for plain text, URLs, images, single files, and multiple files. It normalizes each selected share input into one transferable item.
- BearShare maintains one locally editable EagleShare target address, consisting of a LAN IP address and TCP port. Discovery, saved target lists, hostnames, and QR configuration are excluded.
- EagleShare uses one fixed, configurable TCP listening port and starts its HTTP receiver only while its main window is open. It stops the receiver as part of window shutdown and does not use the system tray or a background receiver.
- EagleShare displays the current LAN IP address together with the listening port and documents the default port in the application UI. The Windows Firewall private-network permission prompt is expected operational behavior when the listener first runs.
- The receiver accepts `POST /api/items` requests with `multipart/form-data`; one request represents exactly one inbox item.
- Multipart requests include `kind` with one of `text`, `url`, `image`, or `file`; `content`; and optional `fileName`, `mimeType`, `sourceLabel`, and client `receivedAt` values. Text and URL content is UTF-8 text, while image and file content is a streamed binary part.
- EagleShare uses its own receipt timestamp as authoritative. Any client timestamp is display-only metadata.
- BearShare sends multiple selected items sequentially. It starts the next request only after a success response for the current request. Each item has an independent visible delivery outcome.
- BearShare regards only a successful completion response as delivery confirmation. A network failure, receiver shutdown, or non-success response marks the current item as failed, retains it temporarily, and exposes retry and discard actions.
- Both applications stream binary content. No implementation may materialize a complete file in memory, and the MVP applies no per-file size limit.
- EagleShare writes each binary upload to staging storage and finalizes it only after the complete stream is received. It creates the inbox record and returns success only after durable content and metadata persistence complete.
- EagleShare returns explicit HTTP client or server errors for malformed requests, failed disk writes, and receiver-unavailable conditions.
- EagleShare persists all content beneath the user's Documents EagleShare storage root. Completed content lives in the inbox area, partial uploads live in an incoming/staging area, and a local metadata database reconstructs the inbox after restart.
- Persisted item metadata includes a stable item identifier, kind, display name, original filename, MIME type, EagleShare receipt time, local file path, applicable size, and upload state.
- Stored content filenames are collision-proof. Original filenames are retained for display and are not required to be unique.
- EagleShare has no automatic deletion, quota, or retention-cleanup mechanism. Per-item deletion requires confirmation and removes the inbox record, metadata, and stored content.
- The EagleShare main window presents an inbox list and selected-item preview. A successful new upload foregrounds the active window and selects that item.
- Text and URL items support literal preview and copying their literal value to the Windows clipboard. Image items support thumbnail and full preview plus Save As. File items show filename, type, and size plus Save As. Text and URL records are persisted as files as well as metadata records.
- The MVP assumes a trusted home LAN. It provides no HTTPS/TLS, encryption, authorization, account, pairing, password, access token, or device identity controls. EagleShare accepts requests from reachable devices at its configured address and port.

## Testing Decisions

- A good test observes external behavior: Android share input produces one or more HTTP requests and visible statuses; an HTTP request produces durable EagleShare inbox content and the corresponding user-visible state. Tests must not depend on private implementation methods, internal staging names, or layout details.
- The primary integration suite exercises the `POST /api/items` seam against a running EagleShare receiver. It verifies multipart parsing, response status, streaming persistence, metadata recording, and restart reconstruction through public behavior.
- Receiver integration tests cover valid text, URL, image, and file uploads; optional metadata; malformed requests; interrupted or failed uploads; disk-write failures; and receiver unavailability. They verify that no incomplete item becomes a completed inbox record and that success is emitted only after persistence.
- Streaming tests use inputs large enough to validate the streamed path and should detect full-content buffering through observable resource limits or stream instrumentation, rather than inspecting implementation details.
- EagleShare persistence tests verify durable inbox reconstruction, collision-safe storage of equal original filenames, authoritative receipt time, delete confirmation behavior, and complete deletion of persisted content and metadata.
- EagleShare UI behavior tests verify the public workflows: listener lifetime follows the main window lifetime, current address is visible, incoming progress is shown, successful items are selected, content previews match item type, copy operations copy literal text or URL values, and Save As produces user-selected copies.
- BearShare tests cover share-intent recognition, mapping of Android-provided content to the request contract, editable target-address persistence, one-at-a-time batch ordering, item-level success and failure statuses, retry, and discard. HTTP client behavior is tested through a controllable server at the public receiver contract.
- End-to-end tests cover a BearShare-to-EagleShare transfer over a LAN-equivalent test network for every supported item type and a mixed multi-item selection. They verify independent failure handling where one item cannot complete.
- The workspace contains no implementation or test suite yet, so there is no local testing prior art to follow. New tests should establish the externally focused patterns above and keep the receiver contract as the shared cross-application seam.

## Out of Scope

- DolphineShare, any cloud relay, web service, or sharing across the internet.
- Accounts, sign-in, multi-user features, authorization, pairing, QR codes, passwords, access keys, and tokens.
- HTTPS/TLS, certificates, encryption in transit, and security mechanisms beyond restricting use to a trusted home LAN.
- Automatic PC discovery, hostnames, multiple saved PCs, multiple PC selection, and automatic handling of changed LAN IP addresses.
- Receiving when EagleShare is closed, minimized to a tray application, or otherwise running without its main window.
- Offline delivery queues beyond BearShare's temporary retention of a failed outgoing item, automatic retries, automatic cleanup, quotas, and per-file size limits.
- Play Store distribution.

## Further Notes

- The product must clearly frame the listener as suitable only for a trusted home network: plain HTTP traffic can be observed or modified by devices able to observe that network, and any reachable device can submit uploads.
- Users must keep EagleShare open before sending from BearShare. If the PC's LAN IP changes, they must update BearShare's stored address manually.
- Permanent content can consume disk space over time. Users manage this through confirmed per-item deletion and direct access to the EagleShare Documents storage area.
- This document is kept locally rather than published to an issue tracker because the workspace exposes no issue-tracker integration and the request excludes Git work. When a tracker is available, publish this body as one issue and apply the `ready-for-agent` triage label.