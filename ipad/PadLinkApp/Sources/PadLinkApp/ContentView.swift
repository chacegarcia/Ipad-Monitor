import SwiftUI

struct ContentView: View {
    @StateObject private var model = PadLinkSessionModel()

    var body: some View {
        NavigationStack {
            VStack(alignment: .leading, spacing: 12) {
                HStack {
                    TextField("Host IP", text: $model.host)
                        .textInputAutocapitalization(.never)
                        .autocorrectionDisabled()
                        .keyboardType(.numbersAndPunctuation)
                    Button(model.isConnected ? "Disconnect" : "Connect") {
                        Task { await model.toggle() }
                    }
                    .buttonStyle(.borderedProminent)
                }

                HStack {
                    Text("Transport: \(model.transportName)")
                    Spacer()
                    Text("FPS: \(model.fps, specifier: "%.1f")")
                }
                .font(.footnote)
                .foregroundStyle(.secondary)

                Divider()

                TestPatternView(image: model.lastFrame)
                    .frame(maxWidth: .infinity, maxHeight: .infinity)
                    .background(Color.black.opacity(0.9))

                if let err = model.lastError {
                    Text(err)
                        .foregroundStyle(.red)
                        .font(.footnote)
                }
            }
            .padding()
            .navigationTitle("PadLink")
        }
    }
}

#Preview {
    ContentView()
}
