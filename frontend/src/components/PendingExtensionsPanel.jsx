import React, { useState, useEffect, useCallback } from 'react';
import axiosClient from '../api/axiosClient';
import { formatCurrency } from '../utils/formatCurrency';

const day = (s) => String(s).slice(0, 10);

/** Staff/Admin: danh sách yêu cầu gia hạn chờ duyệt, duyệt hoặc từ chối (cần lý do). */
const PendingExtensionsPanel = ({ onChanged }) => {
  const [items, setItems] = useState([]);
  const [error, setError] = useState('');
  const load = useCallback(() => {
    axiosClient.get('/booking-extensions/pending').then((r) => setItems(r.data)).catch(() => setError('Không thể tải yêu cầu gia hạn.'));
  }, []);
  useEffect(load, [load]);

  const act = async (id, action, body) => {
    setError('');
    try {
      await axiosClient.post(`/booking-extensions/${id}/${action}`, body);
      load();
      onChanged?.();
    } catch (err) {
      setError(err.response?.data?.message || 'Thao tác thất bại.');
    }
  };
  const reject = (id) => {
    const reason = window.prompt('Lý do từ chối:');
    if (reason?.trim()) act(id, 'reject', { Reason: reason.trim() });
  };

  if (items.length === 0 && !error) return null;
  return (
    <div style={{ background: '#fffbeb', border: '1px solid #fde68a', borderRadius: 12, padding: 16, marginBottom: 20 }}>
      <strong>Yêu cầu gia hạn chờ duyệt ({items.length})</strong>
      {error && <p className="form-error">{error}</p>}
      {items.map((x) => (
        <div key={x.Id} style={{ display: 'flex', gap: 10, alignItems: 'center', flexWrap: 'wrap', marginTop: 8 }}>
          <span>#{x.BookingId} {x.CustomerName} - {x.CarName}: {day(x.OldEndDate)} → {day(x.NewEndDate)} (+{x.ExtraDays} ngày, {formatCurrency(x.ExtraAmount)})</span>
          <button className="btn btn-primary btn-sm" onClick={() => act(x.Id, 'approve')}>Duyệt</button>
          <button className="btn btn-secondary btn-sm" onClick={() => reject(x.Id)}>Từ chối</button>
        </div>
      ))}
    </div>
  );
};

export default PendingExtensionsPanel;
