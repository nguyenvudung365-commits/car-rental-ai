import React, { useState, useEffect, useCallback, useRef } from 'react';
import axiosClient from '../api/axiosClient';
import { imageUrl, isPrimaryImage } from '../utils/imageUrl';

// API chỉ nhận WebP: ảnh JPG/PNG được đổi sang WebP ngay trên trình duyệt (tối đa 2000px cạnh dài).
const toWebp = async (file) => {
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

/**
 * Quản lý ảnh của một xe ngay trong form Sửa xe.
 * API: GET /cars/:id, POST /cars/:id/images, PUT /cars/:id/images/:imgId/primary, DELETE /cars/:id/images/:imgId
 */
const CarImagesManager = ({ carId }) => {
  const [images, setImages] = useState([]);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState(null);
  const fileRef = useRef(null);

  const load = useCallback(async () => {
    try {
      const { data } = await axiosClient.get(`/cars/${carId}`);
      setImages(data.Images ?? data.images ?? []);
    } catch {
      setError('Không tải được danh sách ảnh.');
    }
  }, [carId]);

  useEffect(() => { load(); }, [load]);

  const run = async (action, fallback) => {
    try {
      setBusy(true);
      setError(null);
      await action();
      await load();
    } catch (err) {
      setError(err.response?.data?.message || err.response?.data?.Message || fallback);
    } finally {
      setBusy(false);
    }
  };

  const onUpload = async (e) => {
    const file = e.target.files?.[0];
    if (!file) return;
    await run(async () => {
      const body = new FormData();
      body.append('file', await toWebp(file), 'car.webp');
      await axiosClient.post(`/cars/${carId}/images`, body, { headers: { 'Content-Type': undefined } }); // bỏ JSON mặc định để trình duyệt tự đặt multipart
    }, 'Tải ảnh lên thất bại (ảnh tối đa 5 MB).');
    if (fileRef.current) fileRef.current.value = '';
  };

  const linkBtn = (color) => ({ border: 0, background: 'none', color, cursor: 'pointer', padding: 0 });

  return (
    <div style={{ marginTop: '18px', paddingTop: '14px', borderTop: '1px solid #e5e7eb' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 10 }}>
        <strong>Ảnh xe ({images.length})</strong>
        <label className="btn btn-secondary btn-sm" style={{ cursor: busy ? 'wait' : 'pointer' }}>
          + Tải ảnh lên
          <input ref={fileRef} type="file" accept="image/*" onChange={onUpload} disabled={busy} style={{ display: 'none' }} />
        </label>
      </div>
      {error && <div role="alert" style={{ color: '#b91c1c', fontSize: '0.85rem', marginBottom: 8 }}>{error}</div>}
      {images.length === 0 ? (
        <p className="text-muted" style={{ fontSize: '0.9rem' }}>Xe chưa có ảnh.</p>
      ) : (
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(150px, 1fr))', gap: 10 }}>
          {images.map((img) => {
            const id = img.Id ?? img.id;
            return (
              <div key={id} style={{ border: '1px solid #e5e7eb', borderRadius: 8, overflow: 'hidden' }}>
                <img src={imageUrl(img)} alt="Ảnh xe" style={{ width: '100%', height: 100, objectFit: 'cover', display: 'block' }} />
                <div style={{ display: 'flex', justifyContent: 'space-between', padding: '6px 8px', fontSize: '0.8rem' }}>
                  {isPrimaryImage(img)
                    ? <span style={{ color: '#047857', fontWeight: 600 }}>Ảnh chính</span>
                    : <button type="button" disabled={busy} style={linkBtn('#047857')} onClick={() => run(() => axiosClient.put(`/cars/${carId}/images/${id}/primary`), 'Không đặt được ảnh chính.')}>Đặt làm chính</button>}
                  <button type="button" disabled={busy} style={linkBtn('#b91c1c')} onClick={() => window.confirm('Xóa ảnh này?') && run(() => axiosClient.delete(`/cars/${carId}/images/${id}`), 'Không xóa được ảnh.')}>Xóa</button>
                </div>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
};

export default CarImagesManager;
