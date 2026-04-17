import Foundation
import Network

final class PadLinkSessionModel: ObservableObject {
    @Published var host: String = "192.168.1.10"
    @Published var isConnected = false
    @Published var transportName = "None"
    @Published var fps: Double = 0
    @Published var lastFrame: CGImage?
    @Published var lastError: String?

    private var task: Task<Void, Never>?
    private var connection: NWConnection?

    func toggle() async {
        if isConnected {
            await disconnect()
        } else {
            connect()
        }
    }

    private func connect() {
        Task { @MainActor in
            self.lastError = nil
            self.transportName = "Wi‑Fi (TCP)"
            self.isConnected = true
        }

        task = Task { [weak self] in
            await self?.runSession()
        }
    }

    private func disconnect() async {
        task?.cancel()
        task = nil
        connection?.cancel()
        connection = nil

        await MainActor.run {
            self.isConnected = false
            self.transportName = "None"
        }
    }

    private func runSession() async {
        let port = PadLinkHostingDefaults.tcpPort
        let endpoint = NWEndpoint.hostPort(
            host: NWEndpoint.Host(host),
            port: NWEndpoint.Port(integerLiteral: port)
        )

        let conn = NWConnection(to: endpoint, using: .tcp)
        connection = conn

        do {
            try await waitUntilReady(conn: conn)

            try await sendHello(connection: conn)

            let ackData = try await recvFramedProtobuf(connection: conn)
            _ = try MiniProtobuf.parseSessionAckDisplayMode(ackData)

            var frameCount = 0
            var lastTick = Date()

            while !Task.isCancelled {
                let frameData = try await recvFramedProtobuf(connection: conn)
                let rgba = try MiniProtobuf.parseFrameBundleRGBA(frameData)

                let image = try RGBAHelpers.makeCGImage(rgba: rgba.data, width: rgba.width, height: rgba.height)

                await MainActor.run {
                    self.lastFrame = image
                }

                frameCount += 1
                let now = Date()
                if now.timeIntervalSince(lastTick) >= 1.0 {
                    let fpsLocal = Double(frameCount) / now.timeIntervalSince(lastTick)
                    frameCount = 0
                    lastTick = now
                    await MainActor.run {
                        self.fps = fpsLocal
                    }
                }
            }
        } catch {
            await MainActor.run {
                self.lastError = String(describing: error)
            }
        }

        await disconnect()
    }

    private func waitUntilReady(conn: NWConnection) async throws {
        try await withCheckedThrowingContinuation { (cont: CheckedContinuation<Void, Error>) in
            let gate = OneShotContinuationGate()
            conn.stateUpdateHandler = { state in
                switch state {
                case .ready:
                    if gate.tryConsume() {
                        cont.resume()
                    }
                case .failed(let error):
                    if gate.tryConsume() {
                        cont.resume(throwing: error)
                    }
                case .cancelled:
                    if gate.tryConsume() {
                        cont.resume(throwing: PadLinkError.connectionClosed)
                    }
                default:
                    break
                }
            }
            conn.start(queue: .global(qos: .userInitiated))
        }
    }

    private func sendHello(connection: NWConnection) async throws {
        let payload = MiniProtobuf.buildSessionHelloEnvelope(clientName: "PadLink iPad", protocolVersion: 1)
        try await sendFramed(connection: connection, payload: payload)
    }

    private func sendFramed(connection: NWConnection, payload: Data) async throws {
        var packet = Data()
        packet.append(contentsOf: [0x50, 0x4C, 0x4B, 0x31])
        let len = UInt32(payload.count)
        packet.append(contentsOf: [
            UInt8((len >> 24) & 0xFF),
            UInt8((len >> 16) & 0xFF),
            UInt8((len >> 8) & 0xFF),
            UInt8(len & 0xFF)
        ])
        packet.append(payload)
        try await sendAll(connection: connection, data: packet)
    }

    private func recvFramedProtobuf(connection: NWConnection) async throws -> Data {
        let header = try await recvExact(connection: connection, count: 8)
        guard header.starts(with: [0x50, 0x4C, 0x4B, 0x31]) else {
            throw PadLinkError.badMagic
        }
        let len =
            UInt32(header[4]) << 24 |
            UInt32(header[5]) << 16 |
            UInt32(header[6]) << 8 |
            UInt32(header[7])
        guard len < 50_000_000 else { throw PadLinkError.frameTooLarge }
        return try await recvExact(connection: connection, count: Int(len))
    }

    private func recvExact(connection: NWConnection, count: Int) async throws -> Data {
        var buffer = Data()
        while buffer.count < count {
            let remaining = count - buffer.count
            let chunk: Data = try await withCheckedThrowingContinuation { cont in
                connection.receive(minimumIncompleteLength: 1, maximumLength: remaining) { data, _, isComplete, error in
                    if let error {
                        cont.resume(throwing: error)
                        return
                    }
                    if let data, !data.isEmpty {
                        cont.resume(returning: data)
                    } else if isComplete {
                        cont.resume(throwing: PadLinkError.connectionClosed)
                    }
                }
            }

            if chunk.count <= remaining {
                buffer.append(chunk)
            } else {
                buffer.append(chunk.prefix(remaining))
                // TODO(PadLink): push back overflow if NWConnection doesn't retain it — unlikely for TCP.
            }
        }
        return buffer
    }

    private func sendAll(connection: NWConnection, data: Data) async throws {
        try await withCheckedThrowingContinuation { (cont: CheckedContinuation<Void, Error>) in
            connection.send(content: data, completion: .contentProcessed { error in
                if let error {
                    cont.resume(throwing: error)
                } else {
                    cont.resume()
                }
            })
        }
    }
}

/// Thread-safe single consume for NWConnection `stateUpdateHandler` + `withCheckedThrowingContinuation`
/// (Swift 6 disallows mutating a captured `var` from concurrent network callbacks).
private final class OneShotContinuationGate: @unchecked Sendable {
    private let lock = NSLock()
    private var consumed = false

    func tryConsume() -> Bool {
        lock.lock()
        defer { lock.unlock() }
        guard !consumed else { return false }
        consumed = true
        return true
    }
}

enum PadLinkError: Error {
    case badMagic
    case frameTooLarge
    case connectionClosed
}

enum PadLinkHostingDefaults {
    static let tcpPort: UInt16 = 39777
}
