import React, { useState, useEffect, useCallback } from 'react';
import axiosClient from '../api/axiosClient';
import { formatCurrency } from '../utils/formatCurrency';

const ST = { 1: 'Chờ duyệt', 2: 'Đã duyệt', 3: 'Từ chối' }; // API trả enum dạng số
const day = (s) => String(s).slice(0, 10);

/** Gia hạn đơn: chọn ngày trả mới, xem báo giá/kiểm tra lịch, gửi yêu cầu chờ Staff/Admin duyệt. */
const ExtendBookingModal = ({ booking, onClose, onChanged }) => {
  const [newEnd, setNewEnd] = useState('');
  const [quote, setQuote] = useState(null);
  const [history, setHistory] = useState([]);
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  const loadHistory = useCallback(() => {
    axiosClient.get(`/bookings/${booking.Id}/extensions`).then((r) => setHistory(r.data)).catch(() => {});
  }, [booking.Id]);
  useEffect(loadHistory, [loadHistory]);

  const fail = (err) => setError(err.response?.data?.message || 'Thao tác thất bại.');

  const check = async () => {
    setError('');
    setQuote(null);
    try {
      setQuote((await axiosClient.get(`/bookings/${booking.Id}/extend/check`, { params: { newEndDate: newEnd } })).data);
    } catch (err) { fail(err); }
  };

  const send = async () => {
    setBusy(true);
    setError('');
    try {
      await axiosClient.post(`/bookings/${booking.Id}/extend`, { NewEndDate: newEnd });
      setQuote(null);
      setNewEnd('');
      loadHistory();
      onChanged?.();
    } catch (err) { fail(err); } finally { setBusy(false); }
  };

  return (
    <div onClick={onClose} style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,.4)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 1000 }}>
      <div onClick={(e) => e.stopPropagation()} style={{ background: '#fff', borderRadius: 12, padding: 24, width: 'min(520px, 94vw)', maxHeight: '90vh', overflow: 'auto' }}>
        <h3 style={{ marginTop: 0 }}>Gia hạn đơn #{booking.Id}</h3>
        <p>Ngày trả hiện tại: {day(booking.EndDate)}</p>
        <input type="date" className="form-input" value={newEnd} min={day(booking.EndDate)} onChange={(e) => { setNewEnd(e.target.value); setQuote(null); }} />
        <button className="btn btn-secondary btn-sm" style={{ marginTop: 8 }} disabled={!newEnd} onClick={check}>Kiểm tra</button>
        {quote && (
          <div style={{ margin: '10px 0' }}>
            <p style={{ margin: '4px 0' }}>Thêm {quote.ExtraDays} ngày, phụ thu <strong>{formatCurrency(quote.ExtraAmount)}</strong>; tổng mới {formatCurrency(quote.NewTotalPrice)}.</p>
            {quote.Available
              ? <button className="btn btn-primary btn-sm" disabled={busy} onClick={send}>Gửi yêu cầu gia hạn</button>
              : <p className="form-error">Xe đã có lịch thuê khác trong khoảng này.</p>}
          </div>
        )}
        {error && <p className="form-error">{error}</p>}
        {history.length > 0 && (
          <>
            <h4>Yêu cầu đã gửi</h4>
            {history.map((x) => (
              <p key={x.Id} style={{ margin: '4px 0' }}>
                {day(x.OldEndDate)} → {day(x.NewEndDate)} (+{formatCurrency(x.ExtraAmount)}): {ST[x.Status] || x.Status}
                {x.RejectReason ? ` - ${x.RejectReason}` : ''}
              </p>
            ))}
          </>
        )}
        <button className="btn btn-secondary btn-sm" style={{ marginTop: 12 }} onClick={onClose}>Đóng</button>
      </div>
    </div>
  );
};

export default ExtendBookingModal;
