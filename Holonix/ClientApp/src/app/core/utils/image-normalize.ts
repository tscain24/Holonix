export type NormalizedSquareImage = {
  base64: string;
  mimeType: string;
};

function extractBase64(dataUrl: string): string {
  const marker = 'base64,';
  const markerIndex = dataUrl.indexOf(marker);
  if (markerIndex < 0) {
    return '';
  }

  return dataUrl.slice(markerIndex + marker.length).trim();
}

export async function normalizeImageToSquarePngBase64(
  file: File,
  sizePx = 256
): Promise<NormalizedSquareImage> {
  const safeSize = Number.isFinite(sizePx) ? Math.max(16, Math.min(1024, Math.trunc(sizePx))) : 256;

  const imageBitmap = await createImageBitmap(file);
  try {
    const canvas = document.createElement('canvas');
    canvas.width = safeSize;
    canvas.height = safeSize;

    const context = canvas.getContext('2d');
    if (!context) {
      throw new Error('Unable to acquire 2D context.');
    }

    context.clearRect(0, 0, safeSize, safeSize);

    const scale = Math.min(safeSize / imageBitmap.width, safeSize / imageBitmap.height);
    const drawWidth = Math.max(1, Math.round(imageBitmap.width * scale));
    const drawHeight = Math.max(1, Math.round(imageBitmap.height * scale));
    const dx = Math.floor((safeSize - drawWidth) / 2);
    const dy = Math.floor((safeSize - drawHeight) / 2);

    context.drawImage(imageBitmap, dx, dy, drawWidth, drawHeight);

    const dataUrl = canvas.toDataURL('image/png');
    const base64 = extractBase64(dataUrl);
    if (!base64) {
      throw new Error('Unable to serialize image.');
    }

    return { base64, mimeType: 'image/png' };
  } finally {
    imageBitmap.close();
  }
}

