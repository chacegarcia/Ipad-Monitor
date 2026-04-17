import Foundation

/// Minimal protobuf wire helpers matching `windows/shared-protocol/proto/padlink.proto`.
/// WHY not using `swift-protobuf` yet: keeps the vertical slice buildable without local `protoc`.
/// NEXT: replace with generated types + delete this file.
enum MiniProtobuf {
    static func buildSessionHelloEnvelope(clientName: String, protocolVersion: UInt32) -> Data {
        let hello = buildSessionHello(clientName: clientName, protocolVersion: protocolVersion)
        // WireEnvelope.hello = field 1 (length-delimited)
        return concatField(fieldNumber: 1, wireType: 2, payload: hello)
    }

    static func parseSessionAckDisplayMode(_ envelope: Data) throws -> (width: UInt32, height: UInt32) {
        var reader = ProtoReader(data: envelope)
        while let field = reader.readField() {
            if field.number == 2, case .lengthDelimited(let inner) = field.value {
                // SessionAck.negotiated_mode = field 6
                var innerReader = ProtoReader(data: inner)
                while let innerField = innerReader.readField() {
                    if innerField.number == 6, case .lengthDelimited(let modeData) = innerField.value {
                        return try parseDisplayMode(modeData)
                    }
                }
                throw PadLinkProtoError.missingField("negotiated_mode")
            }
        }
        throw PadLinkProtoError.missingField("session_ack")
    }

    static func parseFrameBundleRGBA(_ envelope: Data) throws -> (width: UInt32, height: UInt32, data: Data) {
        var reader = ProtoReader(data: envelope)
        while let field = reader.readField() {
            if field.number == 3, case .lengthDelimited(let bundle) = field.value {
                return try parseFrameBundle(bundle)
            }
        }
        throw PadLinkProtoError.missingField("frame_bundle")
    }

    private static func parseFrameBundle(_ bundle: Data) throws -> (width: UInt32, height: UInt32, data: Data) {
        var headerWidth: UInt32 = 0
        var headerHeight: UInt32 = 0
        var payload = Data()

        var reader = ProtoReader(data: bundle)
        while let field = reader.readField() {
            switch field.number {
            case 1:
                if case .lengthDelimited(let headerData) = field.value {
                    (headerWidth, headerHeight) = try parseFrameHeaderSizes(headerData)
                }
            case 2:
                if case .lengthDelimited(let bytes) = field.value {
                    payload = bytes
                }
            default:
                break
            }
        }

        guard !payload.isEmpty else { throw PadLinkProtoError.missingField("payload") }

        if headerWidth == 0 || headerHeight == 0 {
            throw PadLinkProtoError.missingField("frame_header")
        }

        let expected = Int(headerWidth * headerHeight * 4)
        guard payload.count == expected else {
            throw PadLinkProtoError.badPayloadSize(expected: expected, actual: payload.count)
        }

        return (headerWidth, headerHeight, payload)
    }

    private static func parseFrameHeaderSizes(_ header: Data) throws -> (UInt32, UInt32) {
        var width: UInt32 = 0
        var height: UInt32 = 0
        var reader = ProtoReader(data: header)
        while let field = reader.readField() {
            if field.number == 3, case .varint(let v) = field.value {
                width = UInt32(truncatingIfNeeded: v)
            } else if field.number == 4, case .varint(let v) = field.value {
                height = UInt32(truncatingIfNeeded: v)
            }
        }
        return (width, height)
    }

    private static func parseDisplayMode(_ data: Data) throws -> (UInt32, UInt32) {
        var width: UInt32 = 0
        var height: UInt32 = 0
        var reader = ProtoReader(data: data)
        while let field = reader.readField() {
            if field.number == 1, case .varint(let v) = field.value {
                width = UInt32(truncatingIfNeeded: v)
            } else if field.number == 2, case .varint(let v) = field.value {
                height = UInt32(truncatingIfNeeded: v)
            }
        }
        guard width > 0, height > 0 else { throw PadLinkProtoError.missingField("display_mode") }
        return (width, height)
    }

    private static func buildSessionHello(clientName: String, protocolVersion: UInt32) -> Data {
        var data = Data()
        data.append(concatField(fieldNumber: 1, wireType: 2, payload: Data(clientName.utf8)))
        data.append(concatField(fieldNumber: 2, wireType: 0, varint: UInt64(protocolVersion)))
        return data
    }

    private static func concatField(fieldNumber: Int, wireType: UInt8, payload: Data) -> Data {
        let tag = UInt32(fieldNumber << 3 | Int(wireType))
        var out = Data()
        out.appendVarint(UInt64(tag))
        if wireType == 2 {
            out.appendVarint(UInt64(payload.count))
            out.append(payload)
        }
        return out
    }

    private static func concatField(fieldNumber: Int, wireType: UInt8, varint: UInt64) -> Data {
        let tag = UInt32(fieldNumber << 3 | Int(wireType))
        var out = Data()
        out.appendVarint(UInt64(tag))
        out.appendVarint(varint)
        return out
    }
}

enum PadLinkProtoError: Error {
    case missingField(String)
    case badPayloadSize(expected: Int, actual: Int)
    case imageCreateFailed
}

private enum ProtoValue {
    case varint(UInt64)
    case lengthDelimited(Data)
}

private struct ProtoField {
    let number: Int
    let value: ProtoValue
}

private struct ProtoReader {
    let data: Data
    var offset: Int = 0

    mutating func readField() -> ProtoField? {
        guard offset < data.count else { return nil }
        let tag = readVarint()
        let fieldNumber = Int(tag >> 3)
        let wire = Int(tag & 0x7)

        switch wire {
        case 0:
            let v = readVarint()
            return ProtoField(number: fieldNumber, value: .varint(v))
        case 1:
            guard offset + 8 <= data.count else { return nil }
            offset += 8
            return readField()
        case 2:
            let len = Int(readVarint())
            guard len >= 0, offset + len <= data.count else { return nil }
            let sub = data.subdata(in: offset ..< (offset + len))
            offset += len
            return ProtoField(number: fieldNumber, value: .lengthDelimited(sub))
        case 5:
            guard offset + 4 <= data.count else { return nil }
            offset += 4
            return readField()
        default:
            // Groups (3/4) are deprecated; treat as parse failure for this minimal parser.
            return nil
        }
    }

    mutating func readVarint() -> UInt64 {
        var result: UInt64 = 0
        var shift: UInt64 = 0
        while offset < data.count {
            let byte = data[offset]
            offset += 1
            result |= UInt64(byte & 0x7F) << shift
            if (byte & 0x80) == 0 {
                break
            }
            shift += 7
        }
        return result
    }
}

private extension Data {
    mutating func appendVarint(_ value: UInt64) {
        var v = value
        repeat {
            var byte = UInt8(v & 0x7F)
            v >>= 7
            if v != 0 {
                byte |= 0x80
            }
            append(byte)
        } while v != 0
    }
}
