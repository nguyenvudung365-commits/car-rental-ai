// API trả object key; mặc định tải ảnh qua API cùng origin với frontend.
const BASE = import.meta.env.VITE_MEDIA_BASE_URL || '/api/media';

export const imageUrl = (img) => {
  if (!img) return '';
  if (img.url) return img.url;
  const path = img.FilePath ?? img.filePath;
  return path ? `${BASE}/${path.split('/').map(encodeURIComponent).join('/')}` : '';
};

export const isPrimaryImage = (img) => Boolean(img?.isThumbnail ?? img?.IsPrimary ?? img?.isPrimary);

export const primaryImageOf = (car) => {
  const list = car?.Images ?? car?.images ?? [];
  return imageUrl(list.find(isPrimaryImage) ?? list[0]);
};
