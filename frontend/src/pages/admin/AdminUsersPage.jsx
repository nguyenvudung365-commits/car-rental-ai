import React, { useState, useEffect, useCallback } from 'react';
import axiosClient from '../../api/axiosClient';
import { roleLabel } from '../../utils/roleLabels';
import { card, input, th, td, apiMessage, fmtDate } from '../../utils/adminUi';

const ROLES = { 1: 'Customer', 2: 'Staff', 3: 'Admin' };
const EMPTY = { fullName: '', email: '', phoneNumber: '', password: '' };

/** Quản lý người dùng (chỉ Admin): tạo Staff, đổi vai trò, khóa/mở khóa. */
const AdminUsersPage = () => {
  const [items, setItems] = useState([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState('');
  const [role, setRole] = useState('');
  const [msg, setMsg] = useState(null);
  const [error, setError] = useState(null);
  const [form, setForm] = useState(null);
  const pageSize = 20;

  const load = useCallback(async () => {
    try {
      const r = await axiosClient.get('/admin/users', {
        params: { page, pageSize, search: search || undefined, role: role || undefined },
      });
      setItems(r.data?.items || []);
      setTotal(r.data?.total || 0);
    } catch (err) {
      setError(apiMessage(err, 'Không thể tải danh sách người dùng.'));
    }
  }, [page, search, role]);

  useEffect(() => { load(); }, [load]);

  const run = async (fn, ok) => {
    try {
      setError(null);
      await fn();
      setMsg(ok);
      await load();
    } catch (err) {
      setError(apiMessage(err, 'Thao tác thất bại.'));
    }
  };

  const changeRole = (u, newRole) =>
    run(() => axiosClient.put(`/admin/users/${u.Id}/role`, { Role: Number(newRole) }), 'Đã đổi vai trò.');

  const lock = (u) => {
    const reason = window.prompt(`Lý do khóa tài khoản ${u.Email}:`);
    if (reason === null) return;
    if (!reason.trim()) { setError('Cần nhập lý do khóa.'); return; }
    run(() => axiosClient.post(`/admin/users/${u.Id}/lock`, { Reason: reason.trim() }), 'Đã khóa tài khoản.');
  };

  const unlock = (u) => run(() => axiosClient.post(`/admin/users/${u.Id}/unlock`), 'Đã mở khóa tài khoản.');

  const createStaff = (e) => {
    e.preventDefault();
    run(async () => {
      await axiosClient.post('/admin/users', {
        FullName: form.fullName.trim(), Email: form.email.trim(),
        PhoneNumber: form.phoneNumber.trim(), Password: form.password,
      });
      setForm(null);
    }, 'Đã tạo tài khoản nhân viên.');
  };

  const setF = (k) => (e) => setForm((f) => ({ ...f, [k]: e.target.value }));
  const totalPages = Math.max(1, Math.ceil(total / pageSize));
  const full = { ...input, width: '100%' };

  return (
    <div style={{ maxWidth: 1200, margin: '0 auto', padding: '24px 16px' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 20 }}>
        <h1 style={{ fontSize: '1.75rem', fontWeight: 700, margin: 0 }}>Quản Lý Người Dùng</h1>
        {!form && <button type="button" className="btn btn-primary" onClick={() => setForm(EMPTY)}>+ Tạo nhân viên</button>}
      </div>
      {msg && <div style={{ marginBottom: 12, padding: 12, borderRadius: 8, background: '#ecfdf5', color: '#065f46' }}>{msg}</div>}
      {error && <div role="alert" style={{ marginBottom: 12, padding: 12, borderRadius: 8, background: '#fef2f2', color: '#991b1b' }}>{error}</div>}

      {form && (
        <form onSubmit={createStaff} style={{ ...card, marginBottom: 20, display: 'grid', gap: 12, gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))' }}>
          <label>Họ tên<input style={full} value={form.fullName} onChange={setF('fullName')} required /></label>
          <label>Email<input style={full} type="email" value={form.email} onChange={setF('email')} required /></label>
          <label>Số điện thoại<input style={full} value={form.phoneNumber} onChange={setF('phoneNumber')} required /></label>
          <label>Mật khẩu (tối thiểu 8 ký tự)<input style={full} type="password" minLength={8} value={form.password} onChange={setF('password')} required /></label>
          <div style={{ display: 'flex', gap: 10, alignItems: 'flex-end' }}>
            <button type="submit" className="btn btn-primary">Tạo</button>
            <button type="button" className="btn" onClick={() => setForm(null)}>Hủy</button>
          </div>
        </form>
      )}

      <div style={{ display: 'flex', gap: 10, marginBottom: 12 }}>
        <input style={input} placeholder="Tìm tên, email, SĐT" value={search} onChange={(e) => { setSearch(e.target.value); setPage(1); }} />
        <select style={input} value={role} onChange={(e) => { setRole(e.target.value); setPage(1); }}>
          <option value="">Tất cả vai trò</option>
          {Object.entries(ROLES).map(([n, k]) => <option key={n} value={n}>{roleLabel(k)}</option>)}
        </select>
      </div>

      <div style={{ ...card, overflowX: 'auto' }}>
        <table style={{ width: '100%', borderCollapse: 'collapse' }}>
          <thead>
            <tr>
              <th style={th}>Họ tên</th><th style={th}>Email</th><th style={th}>SĐT</th>
              <th style={th}>Vai trò</th><th style={th}>Trạng thái</th><th style={th}></th>
            </tr>
          </thead>
          <tbody>
            {items.map((u) => (
              <tr key={u.Id}>
                <td style={td}>{u.FullName}</td>
                <td style={td}>{u.Email}</td>
                <td style={td}>{u.PhoneNumber}</td>
                <td style={td}>
                  <select style={input} value={u.Role} onChange={(e) => changeRole(u, e.target.value)}>
                    {Object.entries(ROLES).map(([n, k]) => <option key={n} value={n}>{roleLabel(k)}</option>)}
                  </select>
                </td>
                <td style={td}>
                  {u.IsLocked
                    ? <span style={{ color: '#b91c1c' }}>Đã khóa{u.LockReason ? `: ${u.LockReason}` : ''}</span>
                    : 'Hoạt động'}
                  <div style={{ color: '#9ca3af', fontSize: '0.75rem' }}>Tạo {fmtDate(u.CreatedAt)}</div>
                </td>
                <td style={td}>
                  {u.IsLocked
                    ? <button type="button" className="btn" onClick={() => unlock(u)}>Mở khóa</button>
                    : <button type="button" className="btn" style={{ color: '#b91c1c' }} onClick={() => lock(u)}>Khóa</button>}
                </td>
              </tr>
            ))}
            {items.length === 0 && <tr><td style={td} colSpan={6}>Không có người dùng.</td></tr>}
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

export default AdminUsersPage;
