import React, { useState, useEffect, useCallback } from 'react';
import { Link } from 'react-router-dom';
import axiosClient from '../../api/axiosClient';
import { useAuth } from '../../auth/AuthContext';
import { formatCurrency } from '../../utils/formatCurrency';
import { card, th, td, apiMessage } from '../../utils/adminUi';
import { formatLocalDateTime } from '../../utils/utcTime';

const POLL_MS = 30000;
const STATUS_LABEL = { 1: 'Sẵn sàng', 2: 'Đang thuê', 3: 'Bảo trì', 4: 'Ngừng hoạt động', 5: 'Chờ duyệt' };

function Stat({ label, value, to, warn }) {
  const body = (
    <div style={{ ...card, borderLeft: `4px solid ${warn ? '#dc2626' : '#2563eb'}` }}>
      <div style={{ fontSize: '0.8rem', color: '#6b7280', fontWeight: 600, textTransform: 'uppercase' }}>{label}</div>
      <div style={{ fontSize: '1.8rem', fontWeight: 800, color: warn ? '#b91c1c' : '#111827', marginTop: 6 }}>{value}</div>
    </div>
  );
  return to ? <Link to={to} style={{ textDecoration: 'none' }}>{body}</Link> : body;
}

function BookingList({ title, rows }) {
  return (
    <div style={card}>
      <h2 style={{ fontSize: '1.05rem', marginTop: 0 }}>{title} ({rows.length})</h2>
      {rows.length === 0 ? <p style={{ color: '#6b7280' }}>Không có.</p> : (
        <table style={{ width: '100%', borderCollapse: 'collapse' }}>
          <thead><tr><th style={th}>Đơn</th><th style={th}>Xe</th><th style={th}>Khách</th><th style={th}>Hạn trả</th><th style={th} /></tr></thead>
          <tbody>
            {rows.map((r) => (
              <tr key={r.BookingId}>
                <td style={td}>#{r.BookingId}</td>
                <td style={td}>{r.CarName} ({r.LicensePlate})</td>
                <td style={td}>{r.CustomerName}</td>
                <td style={td}>{r.Mode === 2 ? formatLocalDateTime(r.EndDate) : new Date(r.EndDate).toLocaleDateString('vi-VN')}</td>
                <td style={td}><Link to={`/admin/returns?bookingId=${r.BookingId}`} className="btn btn-primary">Nhận trả xe</Link></td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}

/** Dashboard vận hành: polling 30 giây, dừng khi tab ẩn (ADR-011, không dùng SignalR). */
const AdminDashboardPage = () => {
  const { user } = useAuth();
  const [data, setData] = useState(null);
  const [error, setError] = useState(null);
  const [updatedAt, setUpdatedAt] = useState(null);

  const load = useCallback(async () => {
    try {
      const res = await axiosClient.get('/dashboard/summary');
      setData(res.data);
      setUpdatedAt(new Date());
      setError(null);
    } catch (err) {
      setError(apiMessage(err, 'Không tải được dashboard.'));
    }
  }, []);

  useEffect(() => {
    let timer = null;
    const start = () => { if (!timer) timer = setInterval(load, POLL_MS); };
    const stop = () => { clearInterval(timer); timer = null; };
    const onVisibility = () => { if (document.hidden) stop(); else { load(); start(); } };
    load();
    if (!document.hidden) start();
    document.addEventListener('visibilitychange', onVisibility);
    return () => { stop(); document.removeEventListener('visibilitychange', onVisibility); };
  }, [load]);

  return (
    <div style={{ maxWidth: 1200, margin: '0 auto', padding: '24px 16px' }}>
      <h1 style={{ fontSize: '1.75rem', fontWeight: 700, margin: 0 }}>Tổng quan vận hành</h1>
      <p style={{ color: '#6b7280' }}>
        Tự cập nhật mỗi 30 giây{updatedAt ? ` · lần cuối ${updatedAt.toLocaleTimeString('vi-VN')}` : ''}.
      </p>
      {error && <div style={{ ...card, color: '#b91c1c', marginBottom: 16 }}>{error}</div>}
      {data && (
        <>
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(190px, 1fr))', gap: 16, marginBottom: 24 }}>
            <Stat label="Đang thuê" value={data.RentingCount} />
            <Stat label="Quá hạn trả" value={data.Overdue.length} warn={data.Overdue.length > 0} />
            <Stat label="Chờ xác nhận" value={data.PendingConfirm} to="/admin/bookings" />
            <Stat label="Giấy tờ chờ duyệt" value={data.PendingDocuments} to="/admin/documents" />
            <Stat label="Sắp/quá hạn bảo trì" value={data.MaintenanceDue} to="/admin/maintenance" warn={data.MaintenanceDue > 0} />
            {user?.role === 'Admin' && data.TodayRevenue != null && (
              <Stat label="Doanh thu hôm nay" value={formatCurrency(data.TodayRevenue)} />
            )}
          </div>
          <div style={{ ...card, marginBottom: 24 }}>
            <h2 style={{ fontSize: '1.05rem', marginTop: 0 }}>Xe theo trạng thái</h2>
            <div style={{ display: 'flex', gap: 24, flexWrap: 'wrap' }}>
              {data.CarsByStatus.map((s) => (
                <div key={s.Status}><strong>{s.Count}</strong> {STATUS_LABEL[s.Status] || s.Status}</div>
              ))}
            </div>
          </div>
          <div style={{ display: 'grid', gap: 24 }}>
            <BookingList title="Quá hạn trả xe" rows={data.Overdue} />
            <BookingList title="Sắp trả trong 24 giờ" rows={data.DueTodayOrTomorrow} />
          </div>
        </>
      )}
    </div>
  );
};

export default AdminDashboardPage;
