import React, { useState, useEffect, useCallback } from 'react';
import axiosClient from '../../api/axiosClient';
import SecureImage from '../../components/SecureImage';
import { card, input, apiMessage, fmtDate } from '../../utils/adminUi';

const DOC_TYPES = { 1: 'CCCD', 2: 'Bằng lái' };

/** Hàng đợi duyệt giấy tờ (Staff, Admin). Ảnh tải qua API có JWT. */
const AdminDocumentsPage = () => {
  const [items, setItems] = useState([]);
  const [total, setTotal] = useState(0);
  const [status, setStatus] = useState('1');
  const [page, setPage] = useState(1);
  const [msg, setMsg] = useState(null);
  const [error, setError] = useState(null);
  const pageSize = 20;

  const load = useCallback(async () => {
    try {
      const r = await axiosClient.get('/admin/documents', { params: { status: status || undefined, page, pageSize } });
      setItems(r.data?.Items ?? r.data?.items ?? []);
      setTotal(r.data?.Total ?? r.data?.total ?? 0);
    } catch (err) { setError(apiMessage(err, 'Không tải được danh sách giấy tờ.')); }
  }, [status, page]);

  useEffect(() => { load(); }, [load]);

  const run = async (fn, ok) => {
    try { setError(null); await fn(); setMsg(ok); await load(); }
    catch (err) { setError(apiMessage(err, 'Thao tác thất bại.')); }
  };

  const reject = (d) => {
    const reason = window.prompt('Lý do từ chối:');
    if (reason === null) return;
    if (!reason.trim()) { setError('Cần nhập lý do từ chối.'); return; }
    run(() => axiosClient.post(`/admin/documents/${d.Id}/reject`, { Reason: reason.trim() }), 'Đã từ chối giấy tờ.');
  };

  const totalPages = Math.max(1, Math.ceil(total / pageSize));
  return (
    <div style={{ maxWidth: 1200, margin: '0 auto', padding: '24px 16px' }}>
      <h1 style={{ fontSize: '1.75rem', fontWeight: 700, marginBottom: 16 }}>Duyệt Giấy Tờ</h1>
      <select style={{ ...input, marginBottom: 12 }} value={status} onChange={(e) => { setStatus(e.target.value); setPage(1); }}>
        <option value="1">Chờ duyệt</option><option value="2">Đã duyệt</option><option value="3">Đã từ chối</option><option value="">Tất cả</option>
      </select>
      {msg && <div style={{ marginBottom: 12, padding: 12, borderRadius: 8, background: '#ecfdf5', color: '#065f46' }}>{msg}</div>}
      {error && <div role="alert" style={{ marginBottom: 12, padding: 12, borderRadius: 8, background: '#fee2e2', color: '#991b1b' }}>{error}</div>}
      {items.map((d) => (
        <div key={d.Id} style={{ ...card, marginBottom: 12 }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', flexWrap: 'wrap', gap: 8 }}>
            <div>
              <strong>{DOC_TYPES[d.Type]}</strong> {d.Number} - {d.CustomerName} (#{d.CustomerId})
              <div style={{ color: '#6b7280', fontSize: '0.85rem' }}>
                Nộp {fmtDate(d.CreatedAt)}
                {d.ExpiryDate ? ` · hết hạn ${new Date(d.ExpiryDate).toLocaleDateString('vi-VN')}` : ''}
                {d.LicenseClass ? ` · hạng ${d.LicenseClass}` : ''}
              </div>
              {d.Status === 3 && <div style={{ color: '#b91c1c' }}>Từ chối: {d.RejectReason}</div>}
              {d.Status === 2 && <div style={{ color: '#047857' }}>Đã duyệt {fmtDate(d.ReviewedAt)}</div>}
            </div>
            {d.Status === 1 && (
              <div style={{ display: 'flex', gap: 8 }}>
                <button type="button" className="btn btn-primary" onClick={() => run(() => axiosClient.post(`/admin/documents/${d.Id}/approve`), 'Đã duyệt giấy tờ.')}>Duyệt</button>
                <button type="button" className="btn" style={{ color: '#b91c1c' }} onClick={() => reject(d)}>Từ chối</button>
              </div>
            )}
          </div>
          <div style={{ display: 'flex', gap: 12, marginTop: 10 }}>
            <SecureImage docId={d.Id} style={{ maxHeight: 220 }} />
            {d.HasBack && <SecureImage docId={d.Id} side="back" style={{ maxHeight: 220 }} />}
          </div>
        </div>
      ))}
      {items.length === 0 && <div style={card}>Không có giấy tờ.</div>}
      {totalPages > 1 && (
        <div style={{ display: 'flex', justifyContent: 'center', gap: 12, marginTop: 16, alignItems: 'center' }}>
          <button type="button" className="btn" disabled={page <= 1} onClick={() => setPage(page - 1)}>Trước</button>
          <span>Trang {page}/{totalPages}</span>
          <button type="button" className="btn" disabled={page >= totalPages} onClick={() => setPage(page + 1)}>Sau</button>
        </div>
      )}
    </div>
  );
};

export default AdminDocumentsPage;
