import React, { useState, useEffect, useCallback } from 'react';
import axiosClient from '../api/axiosClient';
import { formatCurrency } from '../utils/formatCurrency';

const METHODS = [
  { value: 'TienMat', label: 'Tiền mặt' },
  { value: 'ChuyenKhoan', label: 'Chuyển khoản' },
  { value: 'TheNoiDia', label: 'Thẻ nội địa' },
  { value: 'ViDienTu', label: 'Ví điện tử' },
];

const STATUS_LABEL = {
  ChuaThanhToan: 'Chưa thanh toán',
  DaCoc: 'Đã cọc',
  DaThanhToan: 'Đã thanh toán',
  DaHoanTien: 'Đã hoàn tiền',
};

const METHOD_NUM = { TienMat: 1, ChuyenKhoan: 2, TheNoiDia: 3, ViDienTu: 4 };
const STATUS_NUM = { DaCoc: 2, DaThanhToan: 3 };

/**
 * Lịch sử thanh toán của một booking (GET /bookings/:id/payments),
 * tạo khoản thanh toán (POST /bookings/:id/payments),
 * Staff/Admin ghi nhận đã thu (POST /payments/:id/mark-paid).
 */
const PaymentsModal = ({ booking, canMarkPaid = false, onClose }) => {
  const [data, setData] = useState(null);
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  const [amount, setAmount] = useState('');
  const [method, setMethod] = useState('ChuyenKhoan');
  const [code, setCode] = useState('');

  const load = useCallback(async () => {
    try {
      const res = await axiosClient.get(`/bookings/${booking.Id}/payments`);
      setData(res.data);
    } catch (err) {
      setError(err.response?.data?.message || 'Không tải được lịch sử thanh toán.');
    }
  }, [booking.Id]);

  useEffect(() => { load(); }, [load]);

  const submit = async (e) => {
    e.preventDefault();
    const value = Number(amount);
    if (!(value > 0)) { setError('Số tiền phải lớn hơn 0.'); return; }
    try {
      setBusy(true);
      setError('');
      await axiosClient.post(`/bookings/${booking.Id}/payments`, {
        amount: value,
        method: METHOD_NUM[method],
        transactionCode: code.trim() || null,
      });
      setAmount('');
      setCode('');
      await load();
    } catch (err) {
      setError(err.response?.data?.message || 'Không tạo được khoản thanh toán.');
    } finally {
      setBusy(false);
    }
  };

  const markPaid = async (payment, status) => {
    try {
      setBusy(true);
      setError('');
      await axiosClient.post(`/payments/${payment.Id}/mark-paid`, { status: STATUS_NUM[status] });
      await load();
    } catch (err) {
      setError(err.response?.data?.message || 'Không ghi nhận được thanh toán.');
    } finally {
      setBusy(false);
    }
  };

  const cell = { padding: '8px 10px', borderBottom: '1px solid #f3f4f6', fontSize: '0.85rem' };

  return (
    <div
      style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.45)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 1000 }}
      onClick={onClose}
    >
      <div
        style={{ background: '#fff', borderRadius: 12, padding: 24, width: 'min(680px, 94vw)', maxHeight: '88vh', overflowY: 'auto' }}
        onClick={(e) => e.stopPropagation()}
      >
        <h2 style={{ margin: '0 0 12px', fontSize: '1.2rem' }}>Thanh toán đơn #{booking.Id}</h2>

        {error && (
          <div style={{ background: '#fef2f2', color: '#b91c1c', padding: '8px 12px', borderRadius: 6, marginBottom: 12, fontSize: '0.875rem' }}>
            {error}
          </div>
        )}

        {data && (
          <>
            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: 12, marginBottom: 16, fontSize: '0.875rem' }}>
              <div><div style={{ color: '#6b7280' }}>Cần thanh toán</div><strong>{formatCurrency(data.AmountToPay)}</strong></div>
              <div><div style={{ color: '#6b7280' }}>Đã ghi nhận</div><strong style={{ color: '#047857' }}>{formatCurrency(data.PaidAmount)}</strong></div>
              <div><div style={{ color: '#6b7280' }}>Còn lại</div><strong style={{ color: '#b45309' }}>{formatCurrency(data.AmountDue)}</strong></div>
            </div>

            <table style={{ width: '100%', borderCollapse: 'collapse', marginBottom: 16 }}>
              <thead>
                <tr style={{ textAlign: 'left', color: '#6b7280', fontSize: '0.8rem' }}>
                  <th style={cell}>Số tiền</th><th style={cell}>Hình thức</th><th style={cell}>Trạng thái</th><th style={cell}>Mã GD</th>
                  {canMarkPaid && <th style={cell}></th>}
                </tr>
              </thead>
              <tbody>
                {data.Items.length === 0 && (
                  <tr><td style={cell} colSpan={canMarkPaid ? 5 : 4}>Chưa có khoản thanh toán nào.</td></tr>
                )}
                {data.Items.map((p) => (
                  <tr key={p.Id}>
                    <td style={cell}>{formatCurrency(p.Amount)}</td>
                    <td style={cell}>{METHODS.find((m) => m.value === p.Method)?.label || p.Method}</td>
                    <td style={cell}>{STATUS_LABEL[p.Status] || p.Status}</td>
                    <td style={cell}>{p.TransactionCode || '-'}</td>
                    {canMarkPaid && (
                      <td style={cell}>
                        {p.Status === 'ChuaThanhToan' && (
                          <span style={{ display: 'flex', gap: 6 }}>
                            <button type="button" disabled={busy} onClick={() => markPaid(p, 'DaCoc')}>Cọc</button>
                            <button type="button" disabled={busy} onClick={() => markPaid(p, 'DaThanhToan')}>Đã thu</button>
                          </span>
                        )}
                      </td>
                    )}
                  </tr>
                ))}
              </tbody>
            </table>

            {data.AmountDue > 0 && (
              <form onSubmit={submit} style={{ display: 'flex', gap: 8, flexWrap: 'wrap', alignItems: 'flex-end' }}>
                <label style={{ fontSize: '0.8rem' }}>Số tiền<br />
                  <input type="number" min="1" value={amount} onChange={(e) => setAmount(e.target.value)} placeholder={String(data.AmountDue)} />
                </label>
                <label style={{ fontSize: '0.8rem' }}>Hình thức<br />
                  <select value={method} onChange={(e) => setMethod(e.target.value)}>
                    {METHODS.map((m) => <option key={m.value} value={m.value}>{m.label}</option>)}
                  </select>
                </label>
                <label style={{ fontSize: '0.8rem' }}>Mã giao dịch<br />
                  <input type="text" maxLength={100} value={code} onChange={(e) => setCode(e.target.value)} />
                </label>
                <button type="submit" disabled={busy}>Tạo khoản thanh toán</button>
              </form>
            )}
          </>
        )}

        <div style={{ textAlign: 'right', marginTop: 16 }}>
          <button type="button" onClick={onClose}>Đóng</button>
        </div>
      </div>
    </div>
  );
};

export default PaymentsModal;
