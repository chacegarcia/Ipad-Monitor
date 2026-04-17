# PadLink iPad app (SwiftPM)

## Open in Xcode

1. **File → Open…** and select this folder (`ipad/PadLinkApp`).
2. Wait for SwiftPM to resolve.
3. Choose the **`PadLinkApp`** executable scheme and a simulator or device.
4. Set the **Host IP** to your Windows PC LAN address (same network as the desktop host).

## Local network (iOS)

Add `NSLocalNetworkUsageDescription` and `NSBonjourServices` to the generated app Info if you later add Bonjour discovery. For raw TCP to an IP, iOS 14+ typically allows LAN access without Bonjour, but App Store review still expects usage strings if you scan the network.

## Protocol notes

The vertical slice uses **protobuf** on the wire (`windows/shared-protocol/proto/padlink.proto`). Swift uses `MiniProtobuf.swift` so the package builds without installing `protoc`. Replace with **`swift-protobuf` generated code** when the schema stabilizes (`docs/PROTOBUF.md`).
