import React, { useState, useEffect, useCallback } from 'react';
import axiosClient from '../../api/axiosClient';
import useAuth from '../../auth/AuthContext';
import { can } from '../../utils/permissions';
import { card, input, th, td, apiMessage, fmtDate } from '../../utils/adminUi';

const LEVELS = { 1: 'Cảnh báo', 2: 'Chặn' };
const EMPTY = { customerId: '', identityNumber: '', phoneNumber: '', level: '1', reason: '' };

/** Danh sách đen khách hàng. Staff chỉ xem; Admin thêm/gỡ. */
const AdminBlacklistPage = () => {
  const manage = can(useAuth?.()?.user?.role, 'blacklist.manage');
  const [items, setItems] = useState([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [activeOnly, setActiveOnly] = useState(true);
  const [form, setForm] = useState(null);
  const [msg, setMsg] = useState(null);
  const [error, setError] = useState(null);
  const pageSize = 20;

  const load = useCallback(async () => {
    try {
      const r = await axiosClient.get('/admin/blacklist', { params: { page, pageSize, activeOnly } });
      setItems(r.data?.items || []);
      setTotal(r.data?.total || 0);
    } catch (err) {
      setError(apiMessage(err, 'Không thể tải danh sách đen.'));
    }
  }, [page, activeOnly]);

  useEffect(() => { load(); }, [load]);

  const run = async (fn, ok) => {
    try { setError(null); await fn(); setMsg(ok); await load(); }
    catch (err) { setError(apiMessage(err, 'Thao tác thất bại.')); }
  };

  const create = (e) => {
    e.preventDefault();
    run(async () => {
      await axiosClient.post('/admin/blacklist', {
        CustomerId: form.customerId === '' ? null : Number(form.customerId),
        IdentityNumber: form.identityNumber.trim() || null,
        PhoneNumber: form.phoneNumber.trim() || null,
        Level: Number(form.level),
        Reason: form.reason.trim(),
      });
      setForm(null);
    }, 'Đã thêm vào danh sách đen.');
  };

  const remove = (b) => {
    const reason = window.prompt('Lý do gỡ khỏi danh sách đen:');
    if (reason === null) return;
    if (!reason.trim()) { setError('Cần nhập lý do gỡ.'); return; }
    run(() => axiosClient.post(`/admin/blacklist/${b.Id}/remove`, { Reason: reason.trim() }), 'Đã gỡ khỏi danh sách đen.');
  };

  const setF = (k) => (e) => setForm((f) => ({ ...f, [k]: e.target.value }));
  const totalPages = Math.max(1, Math.ceil(total / pageSize));
  const full = { ...input, width: '100%' };
  const badge = (level) => (level === 2
    ? { background: '#fee2e2', color: '#991b1b', padding: '2px 8px', borderRadius: 999 }
    : { background: '#fef3c7', color: '#92400e', padding: '2px 8px', borderRadius: 999 });

  return (
    <div style={{ maxWidth: 1200, margin: '0 auto', padding: '24px 16px' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 20 }}>
        <h1 style={{ fontSize: '1.75rem', fontWeight: 700, margin: 0 }}>Danh Sách Đen Khách Hàng</h1>
        {manage && !form && <button type="button" className="btn btn-primary" onClick={() => setForm(EMPTY)}>+ Thêm</button>}
      </div>
      {msg && <div style={{ marginBottom: 12, padding: 12, borderRadius: 8, background: '#ecfdf5', color: '#065f46' }}>{msg}</div>}
      {error && <div role="alert" style={{ marginBottom: 12, padding: 12, borderRadius: 8, background: '#fef2f2', color: '#991b1b' }}>{error}</div>}

      {manage && form && (
        <form onSubmit={create} style={{ ...card, marginBottom: 20, display: 'grid', gap: 12, gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))' }}>
          <label>Mã khách hàng<input style={full} type="number" min="1" value={form.customerId} onChange={setF('customerId')} /></label>
          <label>CCCD/CMND<input style={full} value={form.identityNumber} onChange={setF('identityNumber')} /></label>
          <label>Số điện thoại<input style={full} value={form.phoneNumber} onChange={setF('phoneNumber')} /></label>
          <label>Mức độ
            <select style={full} value={form.level} onChange={setF('level')}>
              {Object.entries(LEVELS).map(([n, l]) => <option key={n} value={n}>{l}</option>)}
            </select>
          </label>
          <label style={{ gridColumn: '1 / -1' }}>Lý do<textarea style={full} rows={2} value={form.reason} onChange={setF('reason')} required /></label>
          <p style={{ gridColumn: '1 / -1', margin: 0, fontSize: '0.85rem', color: '#6b7280' }}>
            Nhập ít nhất một trong: mã khách, CCCD, số điện thoại. Mức Chặn sẽ chặn đặt xe và duyệt đơn.
          </p>
          <div style={{ display: 'flex', gap: 10 }}>
            <button type="submit" className="btn btn-primary">Lưu</button>
            <button type="button" className="btn" onClick={() => setForm(null)}>Hủy</button>
          </div>
        </form>
      )}

      <label style={{ display: 'block', marginBottom: 12 }}>
        <input type="checkbox" checked={activeOnly} onChange={(e) => { setActiveOnly(e.target.checked); setPage(1); }} /> Chỉ hiển thị bản ghi còn hiệu lực
      </label>

      <div style={{ ...card, overflowX: 'auto' }}>
        <table style={{ width: '100%', borderCollapse: 'collapse' }}>
          <thead>
            <tr>
              <th style={th}>Khách</th><th style={th}>CCCD</th><th style={th}>SĐT</th>
              <th style={th}>Mức độ</th><th style={th}>Lý do</th><th style={th}>Ngày tạo</th><th style={th}></th>
            </tr>
          </thead>
          <tbody>
            {items.map((b) => (
              <tr key={b.Id}>
                <td style={td}>{b.CustomerName || (b.CustomerId ? `#${b.CustomerId}` : '—')}</td>
                <td style={td}>{b.IdentityNumber || '—'}</td>
                <td style={td}>{b.PhoneNumber || '—'}</td>
                <td style={td}><span style={badge(b.Level)}>{LEVELS[b.Level] ?? b.Level}</span></td>
                <td style={td}>
                  {b.Reason}
                  {b.RemovedAt && <div style={{ color: '#6b7280', fontSize: '0.8rem' }}>Đã gỡ {fmtDate(b.RemovedAt)}: {b.RemoveReason}</div>}
                </td>
                <td style={td}>{fmtDate(b.CreatedAt)}</td>
                <td style={td}>{manage && !b.RemovedAt && <button type="button" className="btn" onClick={() => remove(b)}>Gỡ</button>}</td>
              </tr>
            ))}
            {items.length === 0 && <tr><td style={td} colSpan={7}>Không có bản ghi.</td></tr>}
          </tbody>
        </table>
        {totalPages > 1 && (
          <div style={{ display: 'flex', justifyContent: 'center', gap: 12, marginTop: 16, alignItems: 'center' }}>
            <button type="button" className="btn" disabled={page <= 1} onClick={() => setPage(page - 1)}>Trước</button>
            <span>Trang {page}/{totalPages}</span>
            <button type="button" className="btn" disabled={page >= totalPages} onClick={() => setPage(page + 1)}>Sau</button>
          </div>
        )}
      </div>
    </div>
  );
};

export default AdminBlacklistPage;
