// API trả FilePath dạng object key "cars/<guid>.webp"; ghép với địa chỉ công khai của MinIO.
const BASE = import.meta.env.VITE_MEDIA_BASE_URL || 'http://localhost:9100/car-rental';

export const imageUrl = (img) => {
  if (!img) return '';
  if (img.url) return img.url;
  const path = img.FilePath ?? img.filePath;
  return path ? `${BASE}/${path}` : '';
};

export const isPrimaryImage = (img) => Boolean(img?.isThumbnail ?? img?.IsPrimary ?? img?.isPrimary);

export const primaryImageOf = (car) => {
  const list = car?.Images ?? car?.images ?? [];
  return imageUrl(list.find(isPrimaryImage) ?? list[0]);
};
