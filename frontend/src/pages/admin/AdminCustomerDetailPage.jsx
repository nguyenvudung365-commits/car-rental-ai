import React, { useState, useEffect, useCallback } from 'react';
import { Link, useParams } from 'react-router-dom';
import axiosClient from '../../api/axiosClient';
import useAuth from '../../auth/AuthContext';
import { can } from '../../utils/permissions';
import TierBadge, { DOC_STATUS } from '../../components/TierBadge';
import { formatCurrency } from '../../utils/formatCurrency';
import { card, th, td, apiMessage, fmtDate } from '../../utils/adminUi';

const DOC_TYPES = { 1: 'CCCD', 2: 'Bằng lái' };
const STATUS_TEXT = { 1: 'Chờ duyệt', 2: 'Đã duyệt', 3: 'Bị từ chối' };

/** Chi tiết khách. Staff xem bản rút gọn; khóa/chặn/điều chỉnh điểm chỉ Admin. */
const AdminCustomerDetailPage = () => {
  const { id } = useParams();
  const role = useAuth?.()?.user?.role;
  const manage = can(role, 'customer.manage');
  const [c, setC] = useState(null);
  const [msg, setMsg] = useState(null);
  const [error, setError] = useState(null);

  const load = useCallback(async () => {
    try { setC((await axiosClient.get(`/admin/customers/${id}`)).data); }
    catch (err) { setError(apiMessage(err, 'Không tải được khách hàng.')); }
  }, [id]);

  useEffect(() => { load(); }, [load]);

  const run = async (fn, ok) => {
    try { setError(null); await fn(); setMsg(ok); await load(); }
    catch (err) { setError(apiMessage(err, 'Thao tác thất bại.')); }
  };

  const ask = (text) => {
    const v = window.prompt(text);
    if (v === null) return null;
    if (!v.trim()) { setError('Cần nhập lý do.'); return null; }
    return v.trim();
  };

  const lock = () => { const r = ask('Lý do khóa tài khoản:'); if (r) run(() => axiosClient.post(`/admin/users/${id}/lock`, { Reason: r }), 'Đã khóa tài khoản.'); };
  const unlock = () => run(() => axiosClient.post(`/admin/users/${id}/unlock`), 'Đã mở khóa tài khoản.');
  const blacklist = () => {
    const r = ask('Lý do đưa vào danh sách đen (chặn):');
    if (r) run(() => axiosClient.post('/admin/blacklist', { CustomerId: Number(id), Level: 2, Reason: r }), 'Đã thêm vào danh sách đen.');
  };
  const adjust = () => {
    const p = window.prompt('Số điểm cộng/trừ (ví dụ 50 hoặc -50):');
    if (p === null) return;
    const n = parseInt(p, 10);
    if (!n) { setError('Số điểm không hợp lệ.'); return; }
    const note = ask('Lý do điều chỉnh:');
    if (note) run(() => axiosClient.post(`/admin/customers/${id}/loyalty/adjust`, { Points: n, Note: note }), 'Đã điều chỉnh điểm.');
  };

  if (!c) return <div style={{ padding: 24 }}>{error || 'Đang tải...'}</div>;
  const bl = c.Blacklist?.Level;
  const btn = { marginRight: 8 };
  return (
    <div style={{ maxWidth: 1000, margin: '0 auto', padding: '24px 16px' }}>
      <Link to="/admin/customers">← Danh sách khách</Link>
      <h1 style={{ fontSize: '1.6rem', margin: '8px 0' }}>{c.FullName} <TierBadge tier={c.Loyalty?.Tier} /></h1>
      {msg && <div style={{ marginBottom: 12, padding: 12, borderRadius: 8, background: '#ecfdf5', color: '#065f46' }}>{msg}</div>}
      {error && <div role="alert" style={{ marginBottom: 12, padding: 12, borderRadius: 8, background: '#fee2e2', color: '#991b1b' }}>{error}</div>}

      <div style={{ ...card, marginBottom: 16 }}>
        <div>{c.Email} · {c.PhoneNumber}</div>
        {c.IdentityNumber && <div>CCCD: {c.IdentityNumber}</div>}
        {c.Address && <div>Địa chỉ: {c.Address}</div>}
        <div>Đăng ký {fmtDate(c.CreatedAt)} · {c.BookingCount} đơn · tổng chi {formatCurrency(c.TotalSpent)}</div>
        <div>Giấy tờ: <strong>{DOC_STATUS[c.DocumentsStatus]}</strong> · Điểm: <strong>{c.Loyalty?.Points}</strong> (tích lũy {c.Loyalty?.LifetimePoints})</div>
        {c.IsLocked && <div style={{ color: '#b91c1c' }}>Đã khóa{c.LockReason ? `: ${c.LockReason}` : ''}</div>}
        {bl && <div style={{ color: '#92400e' }}>Danh sách đen ({bl === 2 ? 'chặn' : 'cảnh báo'}): {c.Blacklist.Reason}</div>}
        {manage && (
          <div style={{ marginTop: 12 }}>
            {c.IsLocked
              ? <button type="button" className="btn" style={btn} onClick={unlock}>Mở khóa</button>
              : <button type="button" className="btn" style={{ ...btn, color: '#b91c1c' }} onClick={lock}>Khóa tài khoản</button>}
            <button type="button" className="btn" style={btn} onClick={blacklist}>Thêm vào danh sách đen</button>
            <button type="button" className="btn" style={btn} onClick={adjust}>Điều chỉnh điểm</button>
          </div>
        )}
      </div>

      <div style={{ ...card, marginBottom: 16 }}>
        <strong>Giấy tờ</strong>
        {c.Documents?.length === 0 && <p>Chưa nộp.</p>}
        {c.Documents?.map((d) => (
          <div key={d.Id}>{DOC_TYPES[d.Type]} {d.Number} - {STATUS_TEXT[d.Status]}{d.RejectReason ? ` (${d.RejectReason})` : ''}</div>
        ))}
        <Link to="/admin/documents">Mở hàng đợi duyệt giấy tờ</Link>
      </div>

      <div style={{ ...card, overflowX: 'auto' }}>
        <strong>Lịch sử thuê gần đây</strong>
        <table style={{ width: '100%', borderCollapse: 'collapse' }}>
          <thead><tr><th style={th}>Đơn</th><th style={th}>Xe</th><th style={th}>Thời gian</th><th style={th}>Tổng</th><th style={th}>Trạng thái</th></tr></thead>
          <tbody>
            {c.RecentBookings?.map((b) => (
              <tr key={b.Id}>
                <td style={td}>#{b.Id}</td><td style={td}>{b.CarName}</td>
                <td style={td}>{new Date(b.StartDate).toLocaleDateString('vi-VN')} - {new Date(b.EndDate).toLocaleDateString('vi-VN')}</td>
                <td style={td}>{formatCurrency(b.TotalPrice)}</td><td style={td}>{b.Status}</td>
              </tr>
            ))}
            {!c.RecentBookings?.length && <tr><td style={td} colSpan={5}>Chưa có đơn.</td></tr>}
          </tbody>
        </table>
      </div>
    </div>
  );
};

export default AdminCustomerDetailPage;
