import React, { useState, useEffect, useCallback } from 'react';
import axiosClient from '../api/axiosClient';
import { formatCurrency } from '../utils/formatCurrency';

const STATUS = { ChuaThu: 'Chưa thu', DangGiu: 'Đang giữ', DaQuyetToan: 'Đã quyết toán' };
const METHODS = [[1, 'Tiền mặt'], [2, 'Chuyển khoản'], [3, 'Thẻ nội địa'], [4, 'Ví điện tử']];

/** Tiền cọc của đơn. canManage (Staff/Admin): thu cọc khi DaXacNhan, quyết toán khi DaTraXe/DaHuy. */
const DepositPanel = ({ booking, canManage = false, onChanged }) => {
  const [d, setD] = useState(null);
  const [method, setMethod] = useState(1);
  const [note, setNote] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');

  const load = useCallback(() => {
    axiosClient.get(`/bookings/${booking.Id}/deposit`).then((r) => setD(r.data)).catch(() => setError('Không thể tải thông tin cọc.'));
  }, [booking.Id]);
  useEffect(load, [load]);

  const act = async (path, body) => {
    setBusy(true);
    setError('');
    try {
      const r = await axiosClient.post(`/bookings/${booking.Id}/deposit/${path}`, body);
      setD(r.data);
      onChanged?.();
    } catch (err) {
      setError(err.response?.data?.message || 'Thao tác thất bại.');
    } finally {
      setBusy(false);
    }
  };

  if (!d) return error ? <p className="form-error">{error}</p> : <p>Đang tải...</p>;
  if (d.Required <= 0) return <p style={{ color: '#6b7280' }}>Xe này không yêu cầu đặt cọc.</p>;

  const canCollect = canManage && booking.Status === 'DaXacNhan' && d.Status === 'ChuaThu';
  const canSettle = canManage && (booking.Status === 'DaTraXe' || booking.Status === 'DaHuy') && d.Status === 'DangGiu';
  return (
    <div>
      <p style={{ margin: '4px 0' }}>
        Tiền cọc: <strong>{formatCurrency(d.Required)}</strong> ({STATUS[d.Status] || d.Status})
        {' | '}Đang giữ: {formatCurrency(d.Held)} | Khấu trừ: {formatCurrency(d.Deducted)} | Đã hoàn: {formatCurrency(d.Refunded)}
      </p>
      {(canCollect || canSettle) && (
        <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap', alignItems: 'center' }}>
          <select value={method} onChange={(e) => setMethod(Number(e.target.value))}>
            {METHODS.map(([v, l]) => <option key={v} value={v}>{l}</option>)}
          </select>
          {canSettle && <input className="form-input" style={{ maxWidth: 240 }} placeholder="Ghi chú quyết toán" value={note} onChange={(e) => setNote(e.target.value)} />}
          {canCollect && <button className="btn btn-primary btn-sm" disabled={busy} onClick={() => act('collect', { Method: method })}>Thu cọc</button>}
          {canSettle && <button className="btn btn-primary btn-sm" disabled={busy} onClick={() => act('settle', { RefundMethod: method, Note: note.trim() || null })}>Quyết toán cọc (trừ phí, hoàn phần dư)</button>}
        </div>
      )}
      {error && <p className="form-error">{error}</p>}
    </div>
  );
};

export default DepositPanel;
