import React, { useState, useEffect } from 'react';
import axiosClient from '../api/axiosClient';
import HandoverPhoto from './HandoverPhoto';

const LABEL = { 1: 'Biên bản giao xe', 2: 'Biên bản nhận xe' }; // API trả enum dạng số

/** Xem biên bản bàn giao của một đơn (khách chỉ xem đơn của mình, API đã kiểm tra quyền). */
const HandoverView = ({ bookingId }) => {
  const [items, setItems] = useState(null);
  const [error, setError] = useState('');
  useEffect(() => {
    axiosClient.get(`/bookings/${bookingId}/handovers`)
      .then((r) => setItems(r.data))
      .catch(() => setError('Không thể tải biên bản bàn giao.'));
  }, [bookingId]);

  if (error) return <p className="form-error">{error}</p>;
  if (!items) return <p>Đang tải...</p>;
  if (items.length === 0) return <p style={{ color: '#6b7280' }}>Chưa có biên bản bàn giao.</p>;
  return items.map((h) => (
    <div key={h.Id} style={{ marginBottom: 14 }}>
      <strong>{LABEL[h.Type] || h.Type}</strong>
      <p style={{ margin: '4px 0' }}>Số km: {h.Odometer} | Nhiên liệu: {h.FuelLevel}%</p>
      {h.ConditionNote && <p style={{ margin: '4px 0' }}>Ghi chú: {h.ConditionNote}</p>}
      <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>
        {h.Photos.map((p) => <HandoverPhoto key={p.Id} id={p.Id} />)}
      </div>
    </div>
  ));
};

export default HandoverView;
