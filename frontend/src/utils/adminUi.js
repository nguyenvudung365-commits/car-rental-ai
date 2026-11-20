// Style dùng chung cho các trang quản trị mới (khớp AdminCarsPage).
export const card = { backgroundColor: '#fff', borderRadius: '10px', padding: '20px', boxShadow: '0 1px 3px rgba(0,0,0,0.08)' };
export const input = { padding: '8px 10px', borderRadius: '6px', border: '1px solid #d1d5db', fontSize: '0.95rem' };
export const th = { textAlign: 'left', padding: '10px', borderBottom: '1px solid #e5e7eb', fontSize: '0.85rem', color: '#6b7280' };
export const td = { padding: '10px', borderBottom: '1px solid #f3f4f6', fontSize: '0.92rem', verticalAlign: 'top' };
export const apiMessage = (err, fallback) => err.response?.data?.message || err.response?.data?.Message || fallback;
export const fmtDate = (v) => (v ? new Date(v).toLocaleString('vi-VN') : '—');
