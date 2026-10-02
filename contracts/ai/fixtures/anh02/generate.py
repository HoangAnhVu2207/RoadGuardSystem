"""Developer-only synthetic assets; OpenCV generator/decoder, not a live AI provider.

Run from repository root: python contracts/ai/fixtures/anh02/generate.py
Existing versions must not be regenerated silently: compare hashes before changing
fixtureVersion. Runtime consumes checked-in bytes and does not require Python.
"""
from pathlib import Path
import hashlib
import json
import cv2
import numpy as np

root = Path(__file__).resolve().parent
video = root / "synthetic-road-v1.mp4"
writer = cv2.VideoWriter(str(video), cv2.VideoWriter_fourcc(*"mp4v"), 2.0, (64, 48))
if not writer.isOpened():
    raise RuntimeError("OpenCV mp4v encoder unavailable")
for index in range(4):
    frame = np.full((48, 64, 3), 96, dtype=np.uint8)
    cv2.rectangle(frame, (24, 16), (40, 32), (8, 8, 8), -1)
    cv2.line(frame, (index * 4, 0), (index * 4, 47), (180, 180, 180), 1)
    writer.write(frame)
writer.release()
capture = cv2.VideoCapture(str(video))
fps = capture.get(cv2.CAP_PROP_FPS)
decoded = []
while True:
    ok, frame = capture.read()
    if not ok:
        break
    decoded.append(frame)
capture.release()
if fps != 2 or len(decoded) != 4:
    raise RuntimeError("Synthetic duration not verified by decoder")
image = root / "synthetic-road-v1-frame-1.png"
if not cv2.imwrite(str(image), decoded[1]):
    raise RuntimeError("Frame PNG encoder unavailable")
def facts(path):
    raw = path.read_bytes()
    return {"name": path.name, "sha256": hashlib.sha256(raw).hexdigest(), "sizeBytes": len(raw)}
manifest = {
    "fixtureVersion": "synthetic-road-v1", "mode": "MOCK", "source": "SYNTHETIC",
    "generator": "OpenCV " + cv2.__version__ + " mp4v", "decoder": "OpenCV " + cv2.__version__,
    "fps": fps, "decodedFrameCount": len(decoded), "durationMilliseconds": 2000,
    "frameIndex": 1, "timestampMilliseconds": 500, "positionStatus": "UNKNOWN",
    "video": facts(video), "frame": facts(image),
    "detection": {"typeCode": "CRACK", "confidence": 0.8, "bbox": [0.375, 1/3, 0.25, 1/3]}
}
(root / "asset-manifest.json").write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
print(json.dumps(manifest))
