import React, { useState, useEffect, useCallback } from 'react';
import axiosClient from '../../api/axiosClient';
import { useAuth } from '../../auth/AuthContext';
import { formatCurrency } from '../../utils/formatCurrency';
import { card, input, th, td, apiMessage } from '../../utils/adminUi';

const TYPE = { 1: 'Bảo dưỡng', 2: 'Đăng kiểm', 3: 'Bảo hiểm' };
const STATUS = { 1: 'Kế hoạch', 2: 'Đang thực hiện', 3: 'Hoàn thành', 4: 'Hủy' };
const day = (v) => new Date(v).toLocaleDateString('vi-VN');

/** Lịch bảo dưỡng/đăng kiểm/bảo hiểm: xem hạn, tạo lịch, bắt đầu, hoàn thành, hủy. */
const AdminMaintenancePage = () => {
  const { user } = useAuth();
  const isAdmin = user?.role === 'Admin';
  const [due, setDue] = useState({ Overdue: [], Upcoming: [], ByOdometer: [] });
  const [items, setItems] = useState([]);
  const [cars, setCars] = useState([]);
  const [form, setForm] = useState({ CarId: '', Type: 2, DueDate: '', DueOdometer: '', Note: '' });
  const [msg, setMsg] = useState(null);
  const [error, setError] = useState(null);

  const load = useCallback(async () => {
    try {
      const [d, l] = await Promise.all([axiosClient.get('/maintenance/due', { params: { withinDays: 30 } }), axiosClient.get('/maintenance')]);
      setDue(d.data);
      setItems(l.data);
    } catch (err) {
      setError(apiMessage(err, 'Không tải được lịch bảo trì.'));
    }
  }, []);
  useEffect(() => {
    load();
    axiosClient.get('/cars', { params: { page: 1, pageSize: 200 } }).then((r) => setCars(r.data?.items ?? r.data?.Items ?? [])).catch(() => {});
  }, [load]);

  const run = async (fn) => {
    setError(null); setMsg(null);
    try { await fn(); await load(); } catch (err) { setError(apiMessage(err, 'Thao tác không thành công.')); }
  };

  const create = (e) => {
    e.preventDefault();
    run(async () => {
      await axiosClient.post('/maintenance', {
        CarId: Number(form.CarId), Type: Number(form.Type), DueDate: form.DueDate,
        DueOdometer: form.DueOdometer === '' ? null : Number(form.DueOdometer), Note: form.Note || null,
      });
      setForm({ ...form, DueDate: '', DueOdometer: '', Note: '' });
      setMsg('Đã tạo lịch.');
    });
  };

  const start = (m) => run(async () => {
    try {
      await axiosClient.post(`/maintenance/${m.Id}/start`, { Force: false });
    } catch (err) {
      // Có đơn chồng thời gian: hỏi xác nhận rồi gửi lại với force.
      if (err.response?.data?.code === 'MAINTENANCE_BOOKING_OVERLAP' && window.confirm(`${err.response.data.message}\n\nVẫn bắt đầu bảo trì?`)) {
        await axiosClient.post(`/maintenance/${m.Id}/start`, { Force: true });
      } else throw err;
    }
  });

  const complete = (m) => {
    const cost = isAdmin ? window.prompt('Chi phí (VNĐ, để trống nếu không có):', '') : '';
    if (cost === null) return;
    const next = window.prompt('Ngày hạn kế tiếp (YYYY-MM-DD, để trống nếu không có):', '');
    if (next === null) return;
    run(() => axiosClient.post(`/maintenance/${m.Id}/complete`, { Cost: cost ? Number(cost) : null, Note: null, NextDueDate: next || null }));
  };

  const cancel = (m) => window.confirm('Hủy lịch này?') && run(() => axiosClient.post(`/maintenance/${m.Id}/cancel`));

  const Table = ({ rows, actions }) => (
    <table style={{ width: '100%', borderCollapse: 'collapse' }}>
      <thead><tr><th style={th}>Xe</th><th style={th}>Loại</th><th style={th}>Hạn</th><th style={th}>Mốc km</th><th style={th}>Trạng thái</th>{isAdmin && <th style={th}>Chi phí</th>}<th style={th}>Ghi chú</th><th style={th} /></tr></thead>
      <tbody>
        {rows.map((m) => (
          <tr key={m.Id}>
            <td style={td}>{m.LicensePlate}<br /><small>{m.CarName}</small></td>
            <td style={td}>{TYPE[m.Type]}</td>
            <td style={td}>{day(m.DueDate)}</td>
            <td style={td}>{m.DueOdometer ?? '—'}</td>
            <td style={td}>{STATUS[m.Status]}</td>
            {isAdmin && <td style={td}>{m.Cost != null ? formatCurrency(m.Cost) : '—'}</td>}
            <td style={td}>{m.Note}</td>
            <td style={td}>
              {actions && m.Status === 1 && (<><button type="button" className="btn btn-primary" onClick={() => start(m)}>Bắt đầu</button>{' '}<button type="button" className="btn" onClick={() => cancel(m)}>Hủy</button></>)}
              {actions && m.Status === 2 && <button type="button" className="btn btn-primary" onClick={() => complete(m)}>Hoàn thành</button>}
            </td>
          </tr>
        ))}
        {rows.length === 0 && <tr><td style={td} colSpan={8}>Không có.</td></tr>}
      </tbody>
    </table>
  );

  return (
    <div style={{ maxWidth: 1200, margin: '0 auto', padding: '24px 16px' }}>
      <h1 style={{ fontSize: '1.75rem', fontWeight: 700, margin: 0 }}>Bảo dưỡng, đăng kiểm, bảo hiểm</h1>
      {error && <div style={{ ...card, color: '#b91c1c', margin: '12px 0' }}>{error}</div>}
      {msg && <div style={{ ...card, color: '#065f46', margin: '12px 0' }}>{msg}</div>}

      <form onSubmit={create} style={{ ...card, display: 'flex', gap: 8, flexWrap: 'wrap', margin: '16px 0' }}>
        <select style={input} required value={form.CarId} onChange={(e) => setForm({ ...form, CarId: e.target.value })}>
          <option value="">Chọn xe</option>
          {cars.map((c) => <option key={c.Id} value={c.Id}>{c.LicensePlate} - {c.Brand} {c.Model}</option>)}
        </select>
        <select style={input} value={form.Type} onChange={(e) => setForm({ ...form, Type: e.target.value })}>
          {Object.entries(TYPE).map(([k, v]) => <option key={k} value={k}>{v}</option>)}
        </select>
        <input style={input} type="date" required value={form.DueDate} onChange={(e) => setForm({ ...form, DueDate: e.target.value })} />
        <input style={input} type="number" min="0" placeholder="Mốc km (tùy chọn)" value={form.DueOdometer} onChange={(e) => setForm({ ...form, DueOdometer: e.target.value })} />
        <input style={{ ...input, flex: 1 }} maxLength={500} placeholder="Ghi chú" value={form.Note} onChange={(e) => setForm({ ...form, Note: e.target.value })} />
        <button type="submit" className="btn btn-primary">Tạo lịch</button>
      </form>

      <div style={{ ...card, marginBottom: 16 }}>
        <h2 style={{ fontSize: '1.05rem', marginTop: 0, color: '#b91c1c' }}>Quá hạn ({due.Overdue.length})</h2>
        <Table rows={due.Overdue} actions />
      </div>
      <div style={{ ...card, marginBottom: 16 }}>
        <h2 style={{ fontSize: '1.05rem', marginTop: 0 }}>Sắp tới hạn trong 30 ngày ({due.Upcoming.length})</h2>
        <Table rows={due.Upcoming} actions />
      </div>
      <div style={{ ...card, marginBottom: 16 }}>
        <h2 style={{ fontSize: '1.05rem', marginTop: 0 }}>Tới mốc km ({due.ByOdometer.length})</h2>
        <Table rows={due.ByOdometer} actions />
      </div>
      <div style={card}>
        <h2 style={{ fontSize: '1.05rem', marginTop: 0 }}>Tất cả lịch</h2>
        <Table rows={items} actions />
      </div>
    </div>
  );
};

export default AdminMaintenancePage;
