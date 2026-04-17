import SwiftUI
import UIKit

struct TestPatternView: View {
    let image: CGImage?

    var body: some View {
        GeometryReader { geo in
            if let image {
                Image(uiImage: UIImage(cgImage: image))
                    .resizable()
                    .interpolation(.none)
                    .aspectRatio(contentMode: .fit)
                    .frame(width: geo.size.width, height: geo.size.height)
            } else {
                VStack(spacing: 8) {
                    ProgressView()
                    Text("Waiting for frames…")
                        .foregroundStyle(.secondary)
                }
                .frame(maxWidth: .infinity, maxHeight: .infinity)
            }
        }
    }
}
