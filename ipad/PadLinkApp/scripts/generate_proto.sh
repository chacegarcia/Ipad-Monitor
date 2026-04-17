#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PADLINK_ROOT="$(cd "$SCRIPT_DIR/../../.." && pwd)"
PROTO="$PADLINK_ROOT/windows/shared-protocol/proto/padlink.proto"
OUT="$PADLINK_ROOT/ipad/PadLinkApp/Sources/PadLinkProto"

if ! command -v protoc >/dev/null 2>&1; then
  echo "protoc not found. Install Protocol Buffers compiler." >&2
  exit 1
fi

mkdir -p "$OUT"
protoc --swift_out="$OUT" --proto_path="$(dirname "$PROTO")" "$PROTO"
echo "Generated Swift protobuf sources into $OUT"
