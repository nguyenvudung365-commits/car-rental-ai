import React, { useState, useEffect } from 'react';
import axiosClient from '../api/axiosClient';

/** Ảnh giấy tờ riêng tư: tải qua API có JWT rồi hiển thị bằng blob URL (không có URL công khai). */
const SecureImage = ({ docId, side = 'front', style }) => {
  const [src, setSrc] = useState(null);
  const [failed, setFailed] = useState(false);

  useEffect(() => {
    let url;
    let alive = true;
    axiosClient.get(`/documents/${docId}/file`, { params: { side }, responseType: 'blob' })
      .then((r) => { if (alive) { url = URL.createObjectURL(r.data); setSrc(url); } })
      .catch(() => alive && setFailed(true));
    return () => { alive = false; if (url) URL.revokeObjectURL(url); };
  }, [docId, side]);

  if (failed) return <span style={{ color: '#b91c1c' }}>Không tải được ảnh</span>;
  if (!src) return <span style={{ color: '#9ca3af' }}>Đang tải...</span>;
  return <img src={src} alt={side === 'back' ? 'Mặt sau' : 'Mặt trước'} style={{ maxWidth: '100%', borderRadius: 8, ...style }} />;
};

export default SecureImage;
