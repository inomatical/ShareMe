# Share Me

## MVP Implementation Specification
## Purpose

Share Me is a personal utility for moving content from an Android phone to a
Windows PC on the same local Wi-Fi/LAN. The user selects **BearShare** from the
standard Android share menu and BearShare sends the content directly to an open
**EagleShare** desktop application.
The MVP deliberately has no cloud service, account, device pairing,
authentication, encryption, background receiver, or offline delivery queue.
It is intended only for a trusted home network.
## MVP Artifacts

### BearShare
An Android application written in Kotlin and distributed as a directly
installable APK.

Responsibilities:
- Register as an Android share target.
- Accept plain text, URLs, images, files, and multiple selected files.
- Provide an editable EagleShare address field, similar to a browser address
	bar. The user enters EagleShare's current LAN IP address and port.
- Upload selected content directly to EagleShare using HTTP.
- Upload multiple selected items sequentially. The next upload starts only
	after the previous item receives a successful completion response.
- Display a compact delivery result for each item.
- Temporarily retain failed outgoing content and provide an explicit `Retry`
	action. The user can also discard the failed content.

### EagleShare
A Windows 10 and Windows 11 desktop application written in .NET/C# using WPF.

Responsibilities:
- Start a local HTTP receiver when its main window opens.
- Stop the receiver when the main window closes. It must not use the system
	tray or receive content in the background.
- Display its current LAN IP address and listening port in the application so
	the user can enter it in BearShare.
- Persist and display an inbox of all received content.
- Bring the active application window to the foreground and select the newly
	received item after a successful upload.
- Show transfer progress for incoming items.
- Provide previews and actions appropriate to each item type.
### DolphineShare

DolphineShare, the originally proposed REST web service, is excluded from the
MVP. BearShare communicates with EagleShare directly.

## Network Model
- BearShare and EagleShare work only when the phone and PC are on the same
	local Wi-Fi/LAN.
- BearShare stores one target address at a time. Changing to another PC is done
	by editing that address.
- EagleShare listens on a fixed, configurable TCP port. The default port should
	be documented in the application UI and included with the IP address.
- The first time EagleShare listens, Windows Firewall may request permission for
	local-network access. The user must allow the application on private
	networks.
- The MVP uses plain HTTP. HTTPS, certificates, accounts, access tokens,
	pairing codes, and QR codes are out of scope.
- EagleShare accepts uploads from any device that can reach its configured LAN
	address and port. This is acceptable only on a trusted home network.

## Transfer Protocol
EagleShare exposes a local HTTP endpoint for receiving content:

```text
POST /api/items
Content-Type: multipart/form-data
```

Each request represents one inbox item and contains:
- `kind`: `text`, `url`, `image`, or `file`.
- `content`: UTF-8 text for `text` and `url` items, or a streamed binary part
	for `image` and `file` items.
- `fileName`: the original filename when available.
- `mimeType`: the Android-provided MIME type when available.
- `sourceLabel`: optional name of the sharing Android application.
- `receivedAt`: optional client timestamp for display only; EagleShare records
	its own authoritative receipt time.

Protocol behavior:
- Both applications must stream binary data. They must not load an entire file
	into memory.
- No individual file-size limit is imposed by the MVP.
- EagleShare writes binary uploads to a staging file and only marks an item as
	received after the complete stream has been stored and finalized.
- EagleShare returns a success response only after persistence completes.
- BearShare sends a multi-item share one request at a time and waits for that
	response before continuing.
- If EagleShare is unreachable, closes during transfer, or returns an error,
	BearShare marks that item as failed and makes `Retry` available.
- EagleShare should return clear HTTP errors for malformed requests, failed disk
	writes, and unavailable receiver state.

## EagleShare Inbox and Storage
### Storage Location

EagleShare permanently stores received content under:

```text
Documents\EagleShare
```

Suggested structure:

```text
Documents\EagleShare\
	Inbox\
	Incoming\
	eagle-share.db

- `Incoming` holds partial uploads until they complete.
- `Inbox` holds completed files, images, text records, and URL records.
- `eagle-share.db` is a local metadata database that reconstructs the inbox
	after an application or Windows restart.

Stored metadata must include an item identifier, kind, display name, original
filename, MIME type, receipt time, local file path, size where applicable, and
upload state. Stored filenames must be collision-proof; original filenames are
display metadata and do not need to be unique.

### Retention

- No received content is deleted automatically, including when EagleShare
	closes or Windows restarts.
- EagleShare provides a deliberate per-item `Delete` command with confirmation.
- Deletion removes the inbox entry, metadata, and the item’s stored content.
- The application does not impose a storage quota or automatic cleanup policy.

### Inbox Experience

The main EagleShare window contains a persistent inbox list and a preview area
for the selected item. New successful uploads are selected immediately.

Item behavior:

| Item type | Inbox presentation | Primary action |
| --- | --- | --- |
| Text | Text preview | Copy literal text to the Windows clipboard |
| URL | URL preview | Copy literal URL to the Windows clipboard |
| Image | Thumbnail and full preview | Save As |
| File | Filename, type, and size | Save As |

All applicable items support a confirmed `Delete` action. Text and URL records
are also retained as files in the inbox storage folder.

## BearShare Experience

### Initial Configuration

BearShare contains one prominent editable target-address field. The user enters
the IP address and port displayed by EagleShare, for example:

```text
192.168.1.42:8765
```

If the PC receives a different LAN IP address, the user edits this value.
BearShare does not discover PCs automatically and does not support multiple
saved targets in the MVP.

### Share Flow

1. The user chooses Share from an Android application.
2. The user selects BearShare from the Android share sheet.
3. BearShare identifies the shared text, URL, image, file, or list of files.
4. BearShare sends each item to the configured EagleShare address, one by one.
5. BearShare displays success or failure for each item.
6. For failure, BearShare retains the item temporarily and offers `Retry` or
	discard.

## Non-Goals

The following are outside the MVP:
- DolphineShare or any cloud/web relay service.
- Sharing across the internet or between different networks.
- Accounts, sign-in, user management, or multiple-user collaboration.
- Device pairing, QR codes, passwords, access keys, or request authorization.
- HTTPS/TLS or encryption in transit.
- Receiving while EagleShare is closed or minimized to the system tray.
- Automatic IP discovery, hostname discovery, or multiple PC selection.
- Automatic retention cleanup, quotas, or a maximum file-size limit.
- Play Store distribution.

## Risks and Operational Notes

- Plain HTTP means content can be read or modified by devices able to observe
	the local network. The MVP must be used only on a trusted home LAN.
- EagleShare accepts any reachable request, so its IP address and port should
	not be exposed on untrusted networks.
- Unlimited permanent storage can eventually consume available disk space. The
	user manages this through individual deletion and direct access to
	`Documents\EagleShare`.
- A changed PC IP address requires updating the BearShare target address.
- The user must keep EagleShare open before sharing from BearShare.

## Acceptance Criteria

1. Android's share menu lists BearShare for text, URLs, images, files, and
	multiple selected files.
2. BearShare sends a selected item to the configured, currently open
	EagleShare over the local LAN using HTTP.
3. EagleShare accepts the upload, streams it to disk, and acknowledges success
	only after the item is persisted.
4. EagleShare stores received content and metadata in `Documents\EagleShare`
	and restores its inbox after application or Windows restart.
5. EagleShare displays incoming progress, selects successful new items, and
	previews selected images.
6. Text and URLs can be copied to the Windows clipboard; images and files can
	be saved to a user-selected location.
7. Multiple Android-selected items are uploaded sequentially and receive
	independent success or failure status.
8. When EagleShare is unavailable or a transfer fails, BearShare clearly shows
	the failure and offers retry without requiring the original Android share
	action again.
9. EagleShare receives nothing while its main window is closed.
10. EagleShare never automatically deletes received content, while a confirmed
	 per-item delete action removes content deliberately.
# Share Me

## Core Idea

A service or an application that you could share items from your phone.
This is the share functionality of the phone. (email, messenger, etc).

The application in android should accept items that you want to share.
It will be available on the list of applications that the user can share to.

Then when the item is shared with the application, the application then accepts it
and then uploads it to a service.

The service then handles the shared item and then routes it in an other application,
a desktop application (windows).

This desktop application then display the item and allows the user some actions.

### Artifacts

**What should be created.**

1. An Android application: "BearShare".
2. A REST Web service: "DolphineShare".
3. A Desktop application: "EagleShare".

## APPLICATIONS

### BearShare

This is a mobile application.

The core concept of the application is that it can accept items through the
"share" feature most of the mobile applications have.

The user press the share button and then a list of available applications appear.
Then the user selects the BearShare and the item is shared it.
Then the BearShare passes it through to the DolphineShare web service.

### DolphineShare

The services accepts the item and routes it to the user's EagleShare instance.

### EagleShare

The EagleShare desktop application accepts it and display the shared item to the user.
Then it provides some actions
The actions can be

* Copy to clipboard
* Save to disk

