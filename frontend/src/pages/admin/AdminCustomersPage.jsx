import React, { useState, useEffect, useCallback } from 'react';
import { Link } from 'react-router-dom';
import axiosClient from '../../api/axiosClient';
import TierBadge, { DOC_STATUS, TIERS } from '../../components/TierBadge';
import { formatCurrency } from '../../utils/formatCurrency';
import { card, input, th, td, apiMessage } from '../../utils/adminUi';

/** Danh sách khách hàng (Staff, Admin): tìm kiếm, lọc hạng/giấy tờ/danh sách đen. */
const AdminCustomersPage = () => {
  const [items, setItems] = useState([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [f, setF] = useState({ search: '', tier: '', docStatus: '', blacklisted: '' });
  const [error, setError] = useState(null);
  const pageSize = 20;

  const load = useCallback(async () => {
    try {
      const params = { page, pageSize };
      Object.entries(f).forEach(([k, v]) => { if (v !== '') params[k] = v; });
      const r = await axiosClient.get('/admin/customers', { params });
      setItems(r.data?.Items ?? r.data?.items ?? []);
      setTotal(r.data?.Total ?? r.data?.total ?? 0);
    } catch (err) { setError(apiMessage(err, 'Không tải được danh sách khách hàng.')); }
  }, [f, page]);

  useEffect(() => { load(); }, [load]);

  const set = (k) => (e) => { setPage(1); setF((x) => ({ ...x, [k]: e.target.value })); };
  const totalPages = Math.max(1, Math.ceil(total / pageSize));
  return (
    <div style={{ maxWidth: 1200, margin: '0 auto', padding: '24px 16px' }}>
      <h1 style={{ fontSize: '1.75rem', fontWeight: 700, marginBottom: 16 }}>Quản Lý Khách Hàng</h1>
      <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap', marginBottom: 12 }}>
        <input style={input} placeholder="Tìm tên, email, SĐT" value={f.search} onChange={set('search')} />
        <select style={input} value={f.tier} onChange={set('tier')}>
          <option value="">Mọi hạng</option>
          {Object.entries(TIERS).map(([k, t]) => <option key={k} value={k}>{t.label}</option>)}
        </select>
        <select style={input} value={f.docStatus} onChange={set('docStatus')}>
          <option value="">Mọi trạng thái giấy tờ</option>
          {Object.entries(DOC_STATUS).map(([k, t]) => <option key={k} value={k}>{t}</option>)}
        </select>
        <select style={input} value={f.blacklisted} onChange={set('blacklisted')}>
          <option value="">Danh sách đen: tất cả</option><option value="true">Có trong danh sách đen</option><option value="false">Không</option>
        </select>
      </div>
      {error && <div role="alert" style={{ marginBottom: 12, color: '#991b1b' }}>{error}</div>}
      <div style={{ ...card, overflowX: 'auto' }}>
        <table style={{ width: '100%', borderCollapse: 'collapse' }}>
          <thead>
            <tr>
              <th style={th}>Khách</th><th style={th}>SĐT</th><th style={th}>Hạng</th><th style={th}>Giấy tờ</th>
              <th style={th}>Số đơn</th><th style={th}>Tổng chi</th><th style={th}>Trạng thái</th>
            </tr>
          </thead>
          <tbody>
            {items.map((c) => (
              <tr key={c.Id}>
                <td style={td}><Link to={`/admin/customers/${c.Id}`}>{c.FullName}</Link><div style={{ color: '#9ca3af', fontSize: '0.8rem' }}>{c.Email}</div></td>
                <td style={td}>{c.PhoneNumber}</td>
                <td style={td}><TierBadge tier={c.Tier} /> <small>{c.LoyaltyPoints} đ</small></td>
                <td style={td}>{DOC_STATUS[c.DocumentsStatus]}</td>
                <td style={td}>{c.BookingCount}</td>
                <td style={td}>{formatCurrency(c.TotalSpent)}</td>
                <td style={td}>
                  {c.IsLocked && <span style={{ color: '#b91c1c' }}>Đã khóa </span>}
                  {c.BlacklistLevel && <span style={{ color: '#92400e' }}>{c.BlacklistLevel === 2 ? 'Bị chặn' : 'Cảnh báo'}</span>}
                </td>
              </tr>
            ))}
            {items.length === 0 && <tr><td style={td} colSpan={7}>Không có khách hàng.</td></tr>}
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

export default AdminCustomersPage;
