import React, { useState } from 'react';
import axiosClient from '../api/axiosClient';
import { toWebp } from '../utils/toWebp';

const MAX_PHOTOS = 10;

/**
 * Lập biên bản bàn giao (type = 'Giao' | 'Nhan'): số km, mức xăng, ghi chú, ảnh.
 * Lưu biên bản rồi tải từng ảnh lên; gọi onDone() khi xong.
 */
const HandoverForm = ({ bookingId, type, onDone }) => {
  const [odometer, setOdometer] = useState('');
  const [fuel, setFuel] = useState(100);
  const [note, setNote] = useState('');
  const [files, setFiles] = useState([]);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');

  const submit = async (e) => {
    e.preventDefault();
    const km = Number(odometer);
    if (!Number.isInteger(km) || km < 0) { setError('Số km phải là số nguyên không âm.'); return; }
    setBusy(true);
    setError('');
    try {
      const { data } = await axiosClient.post(`/bookings/${bookingId}/handovers`, {
        Type: type === 'Giao' ? 1 : 2, Odometer: km, FuelLevel: Number(fuel), ConditionNote: note.trim() || null,
      });
      for (const f of files) {
        const form = new FormData();
        form.append('file', await toWebp(f), 'photo.webp');
        await axiosClient.post(`/handovers/${data.Id}/photos`, form);
      }
      onDone?.(data);
    } catch (err) {
      setError(err.response?.data?.message || err.message || 'Không thể lưu biên bản.');
    } finally {
      setBusy(false);
    }
  };

  return (
    <form onSubmit={submit}>
      <label>Số km hiện tại
        <input type="number" min="0" className="form-input" value={odometer} onChange={(e) => setOdometer(e.target.value)} required />
      </label>
      <label>Mức nhiên liệu: {fuel}%
        <input type="range" min="0" max="100" value={fuel} onChange={(e) => setFuel(e.target.value)} style={{ width: '100%' }} />
      </label>
      <label>Ghi chú tình trạng xe / hư hỏng
        <textarea className="form-input" rows={3} maxLength={1000} value={note} onChange={(e) => setNote(e.target.value)} />
      </label>
      <label>Ảnh (tối đa {MAX_PHOTOS})
        <input type="file" accept="image/*" multiple onChange={(e) => setFiles(Array.from(e.target.files).slice(0, MAX_PHOTOS))} />
      </label>
      {error && <p className="form-error">{error}</p>}
      <button type="submit" className="btn btn-primary" disabled={busy}>
        {busy ? 'Đang lưu...' : type === 'Giao' ? 'Lưu biên bản giao xe' : 'Lưu biên bản nhận xe'}
      </button>
    </form>
  );
};

export default HandoverForm;
