"""Extract a centered concept-art character and place it on a required flat swatch."""
from pathlib import Path
import sys
import cv2
import numpy as np

source_path = Path(sys.argv[1])
target_path = Path(sys.argv[2])
source = cv2.imread(str(source_path), cv2.IMREAD_COLOR)
if source is None:
    raise SystemExit(f"Cannot read {source_path}")

height, width = source.shape[:2]
mask = np.full((height, width), cv2.GC_BGD, dtype=np.uint8)
# The artwork intentionally has one centered full-body subject. This conservative
# band is probable foreground; GrabCut resolves the exact painted outline.
mask[24:height - 18, 180:840] = cv2.GC_PR_FGD
mask[42:height - 32, 220:800] = cv2.GC_FGD
background_model = np.zeros((1, 65), np.float64)
foreground_model = np.zeros((1, 65), np.float64)
cv2.grabCut(source, mask, None, background_model, foreground_model, 8, cv2.GC_INIT_WITH_MASK)
foreground = np.where((mask == cv2.GC_FGD) | (mask == cv2.GC_PR_FGD), 1, 0).astype(np.uint8)
# Soften the hard segmentation boundary by feathering only the matte.
feathered = cv2.GaussianBlur(foreground.astype(np.float32), (0, 0), 0.7)[..., None]
flat_bgr = np.array([170, 176, 181], dtype=np.float32)  # #B5B0AA in BGR
result = source.astype(np.float32) * feathered + flat_bgr * (1.0 - feathered)
target_path.parent.mkdir(parents=True, exist_ok=True)
cv2.imwrite(str(target_path), np.clip(result, 0, 255).astype(np.uint8))
