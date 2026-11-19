import React, { useState } from 'react';
import axiosClient from '../api/axiosClient';
import { downloadFile } from '../utils/downloadBlob';
import { card, th, td, apiMessage } from '../utils/adminUi';

const MAX_BYTES = 2 * 1024 * 1024;

/** Nhập xe hàng loạt từ Excel: tải mẫu, chọn tệp, xem trước lỗi (dry run), xác nhận nhập. Tất cả hoặc không. */
const CarImportModal = ({ onClose, onImported, isAdmin }) => {
  const [file, setFile] = useState(null);
  const [result, setResult] = useState(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState(null);

  const pick = (e) => {
    const f = e.target.files?.[0] || null;
    setResult(null);
    setError(null);
    if (f && (!f.name.toLowerCase().endsWith('.xlsx') || f.size > MAX_BYTES)) {
      setFile(null);
      setError('Chỉ nhận tệp .xlsx tối đa 2 MB.');
      return;
    }
    setFile(f);
  };

  const send = async (dryRun) => {
    const body = new FormData();
    body.append('file', file);
    try {
      setBusy(true);
      setError(null);
      const res = await axiosClient.post('/cars/import', body, { params: { dryRun }, headers: { 'Content-Type': undefined } });
      setResult(res.data);
      if (res.data.Committed) onImported?.(res.data);
    } catch (err) {
      setError(apiMessage(err, 'Không nhập được tệp.'));
    } finally {
      setBusy(false);
    }
  };

  const template = () => downloadFile('/cars/import/template', undefined, 'mau-nhap-xe.xlsx').catch((e) => setError(e.message));
  const ok = result && result.Errors.length === 0 && result.Valid > 0;

  return (
    <div style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.4)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 50 }}>
      <div style={{ ...card, width: 'min(760px, 94vw)', maxHeight: '86vh', overflowY: 'auto' }}>
        <h2 style={{ marginTop: 0 }}>Nhập xe từ Excel</h2>
        <p style={{ color: '#6b7280', fontSize: '0.9rem' }}>
          Tối đa 500 dòng, 2 MB. {isAdmin ? 'Xe nhập vào sẽ ở trạng thái Sẵn sàng.' : 'Xe nhập vào sẽ ở trạng thái Chờ duyệt (cột tiền cọc và giá theo giờ chỉ Admin được nhập).'} Có lỗi thì không dòng nào được ghi.
        </p>
        <button type="button" className="btn" onClick={template}>Tải file mẫu</button>
        <div style={{ margin: '16px 0' }}>
          <input type="file" accept=".xlsx" onChange={pick} />
        </div>
        {error && <div style={{ color: '#b91c1c', marginBottom: 12 }}>{error}</div>}
        {result && (
          <div style={{ marginBottom: 12 }}>
            <p>
              Tổng {result.Total} dòng, hợp lệ {result.Valid}, lỗi {result.Errors.length}.
              {result.Committed && <strong style={{ color: '#065f46' }}> Đã nhập {result.Valid} xe.</strong>}
            </p>
            {result.Errors.length > 0 && (
              <table style={{ width: '100%', borderCollapse: 'collapse' }}>
                <thead><tr><th style={th}>Dòng</th><th style={th}>Cột</th><th style={th}>Lỗi</th></tr></thead>
                <tbody>
                  {result.Errors.map((er, i) => (
                    <tr key={i}><td style={td}>{er.Row}</td><td style={td}>{er.Column}</td><td style={td}>{er.Message}</td></tr>
                  ))}
                </tbody>
              </table>
            )}
          </div>
        )}
        <div style={{ display: 'flex', gap: 8, justifyContent: 'flex-end' }}>
          <button type="button" className="btn" onClick={onClose}>Đóng</button>
          <button type="button" className="btn" disabled={!file || busy} onClick={() => send(true)}>Kiểm tra</button>
          <button type="button" className="btn btn-primary" disabled={!ok || busy || result?.Committed} onClick={() => send(false)}>Xác nhận nhập</button>
        </div>
      </div>
    </div>
  );
};

export default CarImportModal;
