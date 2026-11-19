// API chỉ nhận WebP: ảnh JPG/PNG được đổi sang WebP ngay trên trình duyệt (tối đa 2000px cạnh dài).
export const toWebp = async (file) => {
  if (file.type === 'image/webp') return file;
  const bmp = await createImageBitmap(file);
  const k = Math.min(1, 2000 / Math.max(bmp.width, bmp.height));
  const canvas = document.createElement('canvas');
  canvas.width = Math.round(bmp.width * k);
  canvas.height = Math.round(bmp.height * k);
  canvas.getContext('2d').drawImage(bmp, 0, 0, canvas.width, canvas.height);
  const blob = await new Promise((res) => canvas.toBlob(res, 'image/webp', 0.85));
  if (!blob) throw new Error('Không đọc được ảnh.');
  return blob;
};
