export function mapPoint(x, y, box, geometry, clamp = false) {
  const { width: w, height: h } = geometry;
  if (!(w > 0 && h > 0 && box.width > 0 && box.height > 0)) return null;
  const scale = Math.min(box.width / w, box.height / h);
  const left = box.left + (box.width - w * scale) / 2;
  const top = box.top + (box.height - h * scale) / 2;
  const nx = (x - left) / (w * scale), ny = (y - top) / (h * scale);
  if (!clamp && (nx < 0 || nx >= 1 || ny < 0 || ny >= 1)) return null;
  const normalizedX = Math.max(0, Math.min(1, nx));
  const normalizedY = Math.max(0, Math.min(1, ny));
  return { x: Math.min(w - 1, Math.floor(normalizedX * w)),
    y: Math.min(h - 1, Math.floor(normalizedY * h)), normalizedX, normalizedY };
}
export function sameGeometry(a, b) {
  return a.width === b.width && a.height === b.height && a.orientation === b.orientation;
}
export function makeTouch(event) {
  return { touchEvent: { touches: [{ x: event.x, y: event.y,
    identifier: event.pointerId, pressure: ['up', 'cancel'].includes(event.phase) ? 0 : Math.max(1, Math.round(event.pressure * 1024)),
    expiration: 1 }], display: 0 } };
}
