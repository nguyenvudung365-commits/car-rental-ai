import React, { useState, useEffect, useCallback } from 'react';
import axiosClient from '../../api/axiosClient';
import { card, input, th, td, apiMessage } from '../../utils/adminUi';
import { formatCurrency } from '../../utils/formatCurrency';

const COUPON_TYPES = { 1: 'Phần trăm', 2: 'Số tiền' };
const SURCHARGE_TYPES = { 1: 'Cố định', 2: 'Theo ngày', 3: 'Phần trăm' };
const EMPTY_COUPON = { code: '', type: '1', value: '', maxDiscount: '', minOrderAmount: '', validFrom: '', validTo: '', usageLimit: '', perCustomerLimit: '1', isActive: true };
const EMPTY_SURCHARGE = { name: '', type: '1', amount: '', autoApply: false, isActive: true };
const EMPTY_BRANCH = { name: '', address: '', phone: '', isActive: true };
const num = (v) => (v === '' || v == null ? null : Number(v));
const day = (v) => (v ? String(v).slice(0, 10) : '');
const grid = { display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 10, margin: '12px 0' };
const table = { width: '100%', borderCollapse: 'collapse', marginTop: 12 };

/** Admin: mã giảm giá, phụ phí, chi nhánh (backend chỉ cho Admin ghi). */
const AdminPricingPage = () => {
  const [coupons, setCoupons] = useState([]);
  const [surcharges, setSurcharges] = useState([]);
  const [branches, setBranches] = useState([]);
  const [cForm, setCForm] = useState(null);
  const [sForm, setSForm] = useState(null);
  const [bForm, setBForm] = useState(null);
  const [msg, setMsg] = useState(null);
  const [error, setError] = useState(null);

  const load = useCallback(async () => {
    try {
      const [c, s, b] = await Promise.all([
        axiosClient.get('/admin/coupons'), axiosClient.get('/admin/surcharges'), axiosClient.get('/admin/branches'),
      ]);
      setCoupons(c.data || []); setSurcharges(s.data || []); setBranches(b.data || []);
    } catch (err) {
      setError(apiMessage(err, 'Không thể tải dữ liệu.'));
    }
  }, []);
  useEffect(() => { load(); }, [load]);

  const save = async (e, form, setForm, path, body) => {
    e.preventDefault();
    try {
      setError(null);
      if (form.id) await axiosClient.put(`${path}/${form.id}`, body); else await axiosClient.post(path, body);
      setMsg('Đã lưu.'); setForm(null); await load();
    } catch (err) {
      setError(apiMessage(err, 'Lưu thất bại.'));
    }
  };

  const saveCoupon = (e) => save(e, cForm, setCForm, '/admin/coupons', {
    Code: cForm.code.trim(), Type: Number(cForm.type), Value: Number(cForm.value), MaxDiscount: num(cForm.maxDiscount),
    MinOrderAmount: num(cForm.minOrderAmount), ValidFrom: cForm.validFrom, ValidTo: cForm.validTo,
    UsageLimit: num(cForm.usageLimit), PerCustomerLimit: Number(cForm.perCustomerLimit) || 1, IsActive: cForm.isActive,
  });
  const saveSurcharge = (e) => save(e, sForm, setSForm, '/admin/surcharges', {
    Name: sForm.name.trim(), Type: Number(sForm.type), Amount: Number(sForm.amount), AutoApply: sForm.autoApply, IsActive: sForm.isActive,
  });
  const saveBranch = (e) => save(e, bForm, setBForm, '/admin/branches', {
    Name: bForm.name.trim(), Address: bForm.address.trim(), Phone: bForm.phone.trim() || null, IsActive: bForm.isActive,
  });

  const editCoupon = (c) => setCForm({
    id: c.Id, code: c.Code, type: String(c.Type), value: c.Value, maxDiscount: c.MaxDiscount ?? '', minOrderAmount: c.MinOrderAmount ?? '',
    validFrom: day(c.ValidFrom), validTo: day(c.ValidTo), usageLimit: c.UsageLimit ?? '', perCustomerLimit: c.PerCustomerLimit, isActive: c.IsActive,
  });
  const editSurcharge = (s) => setSForm({ id: s.Id, name: s.Name, type: String(s.Type), amount: s.Amount, autoApply: s.AutoApply, isActive: s.IsActive });
  const editBranch = (b) => setBForm({ id: b.Id, name: b.Name, address: b.Address, phone: b.Phone ?? '', isActive: b.IsActive });

  const field = (label, el) => <label style={{ display: 'block', fontSize: '0.85rem' }}>{label}{el}</label>;
  const section = { ...card, marginBottom: 20 };

  return (
    <div className="page-container">
      <h1 className="page-title">Mã giảm giá, phụ phí và chi nhánh</h1>
      {msg && <p role="status" style={{ color: '#15803d' }}>{msg}</p>}
      {error && <p role="alert" className="form-error">{error}</p>}

      <div style={section}>
        <h2>Mã giảm giá</h2>
        <button className="btn btn-primary" onClick={() => setCForm({ ...EMPTY_COUPON })}>Thêm mã</button>
        {cForm && (
          <form onSubmit={saveCoupon} style={grid}>
            {field('Mã', <input style={input} value={cForm.code} disabled={!!cForm.id} onChange={(e) => setCForm({ ...cForm, code: e.target.value })} required minLength={3} />)}
            {field('Loại', <select style={input} value={cForm.type} onChange={(e) => setCForm({ ...cForm, type: e.target.value })}><option value="1">Phần trăm</option><option value="2">Số tiền</option></select>)}
            {field('Giá trị', <input style={input} type="number" min="0.01" value={cForm.value} onChange={(e) => setCForm({ ...cForm, value: e.target.value })} required />)}
            {field('Giảm tối đa', <input style={input} type="number" min="0" value={cForm.maxDiscount} onChange={(e) => setCForm({ ...cForm, maxDiscount: e.target.value })} />)}
            {field('Đơn tối thiểu', <input style={input} type="number" min="0" value={cForm.minOrderAmount} onChange={(e) => setCForm({ ...cForm, minOrderAmount: e.target.value })} />)}
            {field('Từ ngày', <input style={input} type="date" value={cForm.validFrom} onChange={(e) => setCForm({ ...cForm, validFrom: e.target.value })} required />)}
            {field('Đến ngày', <input style={input} type="date" value={cForm.validTo} onChange={(e) => setCForm({ ...cForm, validTo: e.target.value })} required />)}
            {field('Giới hạn lượt', <input style={input} type="number" min="1" value={cForm.usageLimit} onChange={(e) => setCForm({ ...cForm, usageLimit: e.target.value })} placeholder="Không giới hạn" />)}
            {field('Lượt tối đa mỗi khách', <input style={input} type="number" min="1" value={cForm.perCustomerLimit} onChange={(e) => setCForm({ ...cForm, perCustomerLimit: e.target.value })} />)}
            <label><input type="checkbox" checked={cForm.isActive} onChange={(e) => setCForm({ ...cForm, isActive: e.target.checked })} /> Đang áp dụng</label>
            <div><button className="btn btn-primary" type="submit">Lưu</button> <button className="btn" type="button" onClick={() => setCForm(null)}>Hủy</button></div>
          </form>
        )}
        <table style={table}>
          <thead><tr>{['Mã', 'Loại', 'Giá trị', 'Hiệu lực', 'Đã dùng', 'Trạng thái', ''].map((h) => <th key={h} style={th}>{h}</th>)}</tr></thead>
          <tbody>{coupons.map((c) => (
            <tr key={c.Id}>
              <td style={td}>{c.Code}</td><td style={td}>{COUPON_TYPES[c.Type]}</td>
              <td style={td}>{c.Type === 1 ? `${c.Value}%` : formatCurrency(c.Value)}</td>
              <td style={td}>{day(c.ValidFrom)} đến {day(c.ValidTo)}</td>
              <td style={td}>{c.UsedCount}{c.UsageLimit ? `/${c.UsageLimit}` : ''}</td>
              <td style={td}>{c.IsActive ? 'Bật' : 'Tắt'}</td>
              <td style={td}><button className="btn" onClick={() => editCoupon(c)}>Sửa</button></td>
            </tr>))}</tbody>
        </table>
      </div>

      <div style={section}>
        <h2>Phụ phí</h2>
        <button className="btn btn-primary" onClick={() => setSForm({ ...EMPTY_SURCHARGE })}>Thêm phụ phí</button>
        {sForm && (
          <form onSubmit={saveSurcharge} style={grid}>
            {field('Tên', <input style={input} value={sForm.name} onChange={(e) => setSForm({ ...sForm, name: e.target.value })} required />)}
            {field('Loại', <select style={input} value={sForm.type} onChange={(e) => setSForm({ ...sForm, type: e.target.value })}><option value="1">Cố định</option><option value="2">Theo ngày</option><option value="3">Phần trăm</option></select>)}
            {field('Giá trị', <input style={input} type="number" min="0" value={sForm.amount} onChange={(e) => setSForm({ ...sForm, amount: e.target.value })} required />)}
            <div>
              <label><input type="checkbox" checked={sForm.autoApply} onChange={(e) => setSForm({ ...sForm, autoApply: e.target.checked })} /> Tự cộng vào mọi đơn mới</label><br />
              <label><input type="checkbox" checked={sForm.isActive} onChange={(e) => setSForm({ ...sForm, isActive: e.target.checked })} /> Đang áp dụng</label>
            </div>
            <div><button className="btn btn-primary" type="submit">Lưu</button> <button className="btn" type="button" onClick={() => setSForm(null)}>Hủy</button></div>
          </form>
        )}
        <table style={table}>
          <thead><tr>{['Tên', 'Loại', 'Giá trị', 'Tự áp dụng', 'Trạng thái', ''].map((h) => <th key={h} style={th}>{h}</th>)}</tr></thead>
          <tbody>{surcharges.map((s) => (
            <tr key={s.Id}>
              <td style={td}>{s.Name}</td><td style={td}>{SURCHARGE_TYPES[s.Type]}</td>
              <td style={td}>{s.Type === 3 ? `${s.Amount}%` : formatCurrency(s.Amount)}</td>
              <td style={td}>{s.AutoApply ? 'Có' : 'Không'}</td><td style={td}>{s.IsActive ? 'Bật' : 'Tắt'}</td>
              <td style={td}><button className="btn" onClick={() => editSurcharge(s)}>Sửa</button></td>
            </tr>))}</tbody>
        </table>
      </div>

      <div style={section}>
        <h2>Chi nhánh</h2>
        <button className="btn btn-primary" onClick={() => setBForm({ ...EMPTY_BRANCH })}>Thêm chi nhánh</button>
        {bForm && (
          <form onSubmit={saveBranch} style={grid}>
            {field('Tên', <input style={input} value={bForm.name} onChange={(e) => setBForm({ ...bForm, name: e.target.value })} required />)}
            {field('Địa chỉ', <input style={input} value={bForm.address} onChange={(e) => setBForm({ ...bForm, address: e.target.value })} required />)}
            {field('Điện thoại', <input style={input} value={bForm.phone} onChange={(e) => setBForm({ ...bForm, phone: e.target.value })} />)}
            <label><input type="checkbox" checked={bForm.isActive} onChange={(e) => setBForm({ ...bForm, isActive: e.target.checked })} /> Đang hoạt động</label>
            <div><button className="btn btn-primary" type="submit">Lưu</button> <button className="btn" type="button" onClick={() => setBForm(null)}>Hủy</button></div>
          </form>
        )}
        <table style={table}>
          <thead><tr>{['Tên', 'Địa chỉ', 'Điện thoại', 'Trạng thái', ''].map((h) => <th key={h} style={th}>{h}</th>)}</tr></thead>
          <tbody>{branches.map((b) => (
            <tr key={b.Id}>
              <td style={td}>{b.Name}</td><td style={td}>{b.Address}</td><td style={td}>{b.Phone || '—'}</td>
              <td style={td}>{b.IsActive ? 'Hoạt động' : 'Ngừng'}</td>
              <td style={td}><button className="btn" onClick={() => editBranch(b)}>Sửa</button></td>
            </tr>))}</tbody>
        </table>
      </div>
    </div>
  );
};

export default AdminPricingPage;
