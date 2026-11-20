import React, { useState } from 'react';
import axiosClient from '../api/axiosClient';
import { toWebp } from '../utils/toWebp';
import { input, apiMessage } from '../utils/adminUi';

/** Nộp CCCD (type 1) hoặc bằng lái (type 2). Ảnh được đổi sang WebP trước khi gửi. */
const DocumentUploadForm = ({ type, onDone }) => {
  const isLicense = type === 2;
  const [form, setForm] = useState({ number: '', expiry: '', cls: '' });
  const [front, setFront] = useState(null);
  const [back, setBack] = useState(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState(null);

  const submit = async (e) => {
    e.preventDefault();
    if (!front) { setError('Cần ảnh mặt trước.'); return; }
    setBusy(true); setError(null);
    try {
      const body = new FormData();
      body.append('Type', type);
      body.append('Number', form.number.trim());
      if (isLicense && form.expiry) body.append('ExpiryDate', form.expiry);
      if (isLicense && form.cls.trim()) body.append('LicenseClass', form.cls.trim());
      body.append('Front', await toWebp(front), 'front.webp');
      if (back) body.append('Back', await toWebp(back), 'back.webp');
      await axiosClient.post('/me/documents', body, { headers: { 'Content-Type': undefined } });
      onDone?.();
    } catch (err) {
      setError(apiMessage(err, 'Nộp giấy tờ thất bại (ảnh tối đa 5 MB).'));
    } finally { setBusy(false); }
  };

  const set = (k) => (e) => setForm((f) => ({ ...f, [k]: e.target.value }));
  return (
    <form onSubmit={submit} style={{ display: 'grid', gap: 8, maxWidth: 420 }}>
      <input style={input} placeholder={isLicense ? 'Số bằng lái (12 số)' : 'Số CCCD (12 số)'} value={form.number} onChange={set('number')} required />
      {isLicense && <input style={input} type="date" value={form.expiry} onChange={set('expiry')} required title="Ngày hết hạn" />}
      {isLicense && <input style={input} placeholder="Hạng (B1, B2...)" value={form.cls} onChange={set('cls')} />}
      <label>Mặt trước <input type="file" accept="image/*" onChange={(e) => setFront(e.target.files[0])} /></label>
      <label>Mặt sau <input type="file" accept="image/*" onChange={(e) => setBack(e.target.files[0])} /></label>
      {error && <div role="alert" style={{ color: '#b91c1c' }}>{error}</div>}
      <button type="submit" className="btn btn-primary" disabled={busy}>{busy ? 'Đang gửi...' : 'Nộp để xác minh'}</button>
    </form>
  );
};

export default DocumentUploadForm;
