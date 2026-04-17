import CoreGraphics
import Foundation

enum RGBAHelpers {
    static func makeCGImage(rgba: Data, width: UInt32, height: UInt32) throws -> CGImage {
        let w = Int(width)
        let h = Int(height)
        let bytesPerRow = w * 4
        guard rgba.count == bytesPerRow * h else {
            throw PadLinkProtoError.badPayloadSize(expected: bytesPerRow * h, actual: rgba.count)
        }

        let provider = CGDataProvider(data: rgba as CFData)!
        let colorSpace = CGColorSpaceCreateDeviceRGB()
        let bitmapInfo = CGBitmapInfo(rawValue: CGImageAlphaInfo.premultipliedLast.rawValue)
            .union(.byteOrder32Big)

        guard let image = CGImage(
            width: w,
            height: h,
            bitsPerComponent: 8,
            bitsPerPixel: 32,
            bytesPerRow: bytesPerRow,
            space: colorSpace,
            bitmapInfo: bitmapInfo,
            provider: provider,
            decode: nil,
            shouldInterpolate: true,
            intent: .defaultIntent
        ) else {
            throw PadLinkProtoError.imageCreateFailed
        }

        return image
    }
}
