// swift-tools-version: 5.9
import PackageDescription

let package = Package(
    name: "PadLinkApp",
    platforms: [
        .iOS(.v17)
    ],
    products: [
        .executable(name: "PadLinkApp", targets: ["PadLinkApp"])
    ],
    targets: [
        .executableTarget(
            name: "PadLinkApp",
            path: "Sources/PadLinkApp"
        )
    ]
)
