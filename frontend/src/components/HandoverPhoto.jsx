import React, { useState, useEffect } from 'react';
import axiosClient from '../api/axiosClient';

/** Ảnh bàn giao riêng tư: tải qua API có JWT rồi hiển thị bằng blob URL. */
const HandoverPhoto = ({ id }) => {
  const [src, setSrc] = useState(null);
  const [failed, setFailed] = useState(false);
  useEffect(() => {
    let url;
    let alive = true;
    axiosClient.get(`/handover-photos/${id}`, { responseType: 'blob' })
      .then((r) => { if (alive) { url = URL.createObjectURL(r.data); setSrc(url); } })
      .catch(() => alive && setFailed(true));
    return () => { alive = false; if (url) URL.revokeObjectURL(url); };
  }, [id]);
  if (failed) return <span style={{ color: '#b91c1c' }}>Không tải được ảnh</span>;
  if (!src) return <span style={{ color: '#9ca3af' }}>Đang tải...</span>;
  return <img src={src} alt="Ảnh bàn giao" style={{ width: 120, height: 90, objectFit: 'cover', borderRadius: 6 }} />;
};

export default HandoverPhoto;
