import React, { useState, useEffect, useCallback } from 'react';
import axiosClient from '../../api/axiosClient';
import { card, input, th, td, apiMessage, fmtDate } from '../../utils/adminUi';

const ENTITIES = ['Car', 'Booking', 'Payment', 'ReturnRecord', 'Customer', 'BlacklistEntry'];

const prettyJson = (s) => {
  try { return JSON.stringify(JSON.parse(s), null, 2); } catch { return s || ''; }
};

/** Nhật ký thay đổi dữ liệu (chỉ Admin). */
const AdminAuditLogsPage = () => {
  const [items, setItems] = useState([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [entityType, setEntityType] = useState('');
  const [entityId, setEntityId] = useState('');
  const [from, setFrom] = useState('');
  const [to, setTo] = useState('');
  const [open, setOpen] = useState(null);
  const [error, setError] = useState(null);
  const pageSize = 20;

  const load = useCallback(async () => {
    try {
      setError(null);
      const r = await axiosClient.get('/admin/audit-logs', {
        params: {
          page, pageSize,
          entityType: entityType || undefined,
          entityId: entityId || undefined,
          from: from || undefined,
          to: to ? `${to}T23:59:59` : undefined,
        },
      });
      setItems(r.data?.items || []);
      setTotal(r.data?.total || 0);
    } catch (err) {
      setError(apiMessage(err, 'Không thể tải nhật ký.'));
    }
  }, [page, entityType, entityId, from, to]);

  useEffect(() => { load(); }, [load]);

  const reset = (setter) => (e) => { setter(e.target.value); setPage(1); };
  const totalPages = Math.max(1, Math.ceil(total / pageSize));

  return (
    <div style={{ maxWidth: 1200, margin: '0 auto', padding: '24px 16px' }}>
      <h1 style={{ fontSize: '1.75rem', fontWeight: 700, marginTop: 0 }}>Nhật Ký Hoạt Động</h1>
      {error && <div role="alert" style={{ marginBottom: 12, padding: 12, borderRadius: 8, background: '#fef2f2', color: '#991b1b' }}>{error}</div>}
      <div style={{ display: 'flex', gap: 10, marginBottom: 12, flexWrap: 'wrap' }}>
        <select style={input} value={entityType} onChange={reset(setEntityType)}>
          <option value="">Tất cả đối tượng</option>
          {ENTITIES.map((e) => <option key={e} value={e}>{e}</option>)}
        </select>
        <input style={input} placeholder="Mã đối tượng" value={entityId} onChange={reset(setEntityId)} />
        <label>Từ <input style={input} type="date" value={from} onChange={reset(setFrom)} /></label>
        <label>Đến <input style={input} type="date" value={to} onChange={reset(setTo)} /></label>
      </div>
      <div style={{ ...card, overflowX: 'auto' }}>
        <table style={{ width: '100%', borderCollapse: 'collapse' }}>
          <thead>
            <tr>
              <th style={th}>Thời gian</th><th style={th}>Người thực hiện</th><th style={th}>Hành động</th>
              <th style={th}>Đối tượng</th><th style={th}>IP</th><th style={th}></th>
            </tr>
          </thead>
          <tbody>
            {items.map((l) => (
              <React.Fragment key={l.Id}>
                <tr>
                  <td style={td}>{fmtDate(l.CreatedAt)}</td>
                  <td style={td}>{l.ActorName || (l.ActorId ? `#${l.ActorId}` : 'Hệ thống')}{l.ActorRole ? ` (${l.ActorRole})` : ''}</td>
                  <td style={td}>{l.Action}</td>
                  <td style={td}>{l.EntityType} #{l.EntityId}</td>
                  <td style={td}>{l.IpAddress || '—'}</td>
                  <td style={td}>
                    <button type="button" className="btn" onClick={() => setOpen(open === l.Id ? null : l.Id)}>
                      {open === l.Id ? 'Ẩn' : 'Chi tiết'}
                    </button>
                  </td>
                </tr>
                {open === l.Id && (
                  <tr>
                    <td style={td} colSpan={6}>
                      <pre style={{ margin: 0, background: '#f9fafb', padding: 12, borderRadius: 6, whiteSpace: 'pre-wrap', fontSize: '0.8rem' }}>{prettyJson(l.Changes)}</pre>
                    </td>
                  </tr>
                )}
              </React.Fragment>
            ))}
            {items.length === 0 && <tr><td style={td} colSpan={6}>Chưa có bản ghi.</td></tr>}
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

export default AdminAuditLogsPage;
