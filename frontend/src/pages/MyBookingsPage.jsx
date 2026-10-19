import { useState, useEffect } from 'react';
import axiosClient from '../api/axiosClient';
import { formatCurrency } from '../utils/formatCurrency';

const STATUS_MAP = {
  ChoXacNhan: { label: 'Chờ duyệt',  className: 'badge badge-pending'   },
  DaXacNhan:  { label: 'Đã duyệt',   className: 'badge badge-confirmed' },
  DangThue:   { label: 'Đang thuê',  className: 'badge badge-confirmed' },
  DaTraXe:    { label: 'Hoàn thành', className: 'badge badge-completed' },
  DaHuy:      { label: 'Đã hủy',     className: 'badge badge-cancelled' },
};

export default function MyBookingsPage() {
  const [bookings, setBookings] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [cancelModalOpen, setCancelModalOpen] = useState(false);
  const [cancelBooking, setCancelBooking] = useState(null);
  const [cancelReason, setCancelReason] = useState('');
  const [actionLoading, setActionLoading] = useState(false);
  const [modalError, setModalError] = useState('');

  const fetchBookings = () => {
    setLoading(true);
    axiosClient.get('/bookings')
      .then(res => {
        const data = res.data?.items || res.data || [];
        setBookings(data);
      })
      .catch(() => setError('Không thể tải lịch sử thuê xe'))
      .finally(() => setLoading(false));
  };

  useEffect(() => {
    fetchBookings();
  }, []);

  const formatDate = (dateStr) => {
    if (!dateStr) return '-';
    try {
      const d = new Date(dateStr);
      if (isNaN(d.getTime())) return dateStr;
      return d.toLocaleDateString('vi-VN', {
        day: '2-digit',
        month: '2-digit',
        year: 'numeric',
      });
    } catch {
      return dateStr;
    }
  };

  const openCancelModal = (booking) => {
    setCancelBooking(booking);
    setCancelReason('');
    setModalError('');
    setCancelModalOpen(true);
  };

  const closeCancelModal = () => {
    setCancelModalOpen(false);
    setCancelBooking(null);
    setCancelReason('');
    setModalError('');
  };

  const handleConfirmCancel = async (e) => {
    e.preventDefault();
    if (!cancelReason.trim()) {
      setModalError('Vui lòng nhập lý do hủy.');
      return;
    }

    try {
      setActionLoading(true);
      setModalError('');
      await axiosClient.post(`/bookings/${cancelBooking.Id}/cancel`, {
        reason: cancelReason.trim(),
      });
      closeCancelModal();
      fetchBookings();
    } catch (err) {
      setModalError(err.response?.data?.message || 'Không thể hủy đơn thuê.');
    } finally {
      setActionLoading(false);
    }
  };

  if (loading) return <div className="page-container"><div className="loading-spinner"></div></div>;

  return (
    <div className="page-container">
      <h1 className="page-title">Lịch sử thuê xe</h1>

      {error && <p className="form-error">{error}</p>}

      {bookings.length === 0 ? (
        <div style={{ textAlign: 'center', padding: 40, color: '#6b7280' }}>
          <p style={{ fontSize: 48 }}>📋</p>
          <p>Bạn chưa có đơn thuê nào</p>
        </div>
      ) : (
        <>
          {/* Desktop table */}
          <div className="table-responsive">
            <table className="table">
              <thead>
                <tr>
                  <th>Xe</th>
                  <th>Biển số</th>
                  <th>Ngày thuê</th>
                  <th>Số ngày</th>
                  <th>Tổng giá</th>
                  <th>Trạng thái</th>
                  <th>Thao tác</th>
                </tr>
              </thead>
              <tbody>
                {bookings.map(b => {
                  const st = STATUS_MAP[b.Status] || { label: b.Status, className: 'badge' };
                  const canCancel = b.Status === 'ChoXacNhan';
                  return (
                    <tr key={b.Id}>
                      <td>{b.CarName}</td>
                      <td>{b.LicensePlate}</td>
                      <td>{formatDate(b.StartDate)} → {formatDate(b.EndDate)}</td>
                      <td>{b.RentalDays} ngày</td>
                      <td>{formatCurrency(b.TotalPrice)}</td>
                      <td><span className={st.className}>{st.label}</span></td>
                      <td>
                        {canCancel ? (
                          <button
                            onClick={() => openCancelModal(b)}
                            className="btn btn-sm btn-secondary"
                            style={{ backgroundColor: '#ef4444', color: '#fff' }}
                          >
                            Hủy đơn
                          </button>
                        ) : '—'}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>

          {/* Mobile cards */}
          <div className="mobile-cards">
            {bookings.map(b => {
              const st = STATUS_MAP[b.Status] || { label: b.Status, className: 'badge' };
              const canCancel = b.Status === 'ChoXacNhan';
              return (
                <div key={b.Id} className="admin-card">
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 8 }}>
                    <strong>{b.CarName}</strong>
                    <span className={st.className}>{st.label}</span>
                  </div>
                  <p style={{ color: '#6b7280', margin: '4px 0' }}>Biển số: {b.LicensePlate}</p>
                  <p style={{ margin: '4px 0' }}>📅 {formatDate(b.StartDate)} → {formatDate(b.EndDate)}</p>
                  <p style={{ margin: '4px 0' }}>⏱️ {b.RentalDays} ngày</p>
                  <p style={{ fontWeight: 600, color: '#2563eb', margin: '4px 0' }}>
                    {formatCurrency(b.TotalPrice)}
                  </p>
                  {canCancel && (
                    <button
                      onClick={() => openCancelModal(b)}
                      className="btn btn-sm btn-secondary"
                      style={{ marginTop: 8, backgroundColor: '#ef4444', color: '#fff' }}
                    >
                      Hủy đơn
                    </button>
                  )}
                </div>
              );
            })}
          </div>
        </>
      )}

      {/* Cancel Modal */}
      {cancelModalOpen && cancelBooking && (
        <div
          className="modal-overlay"
          style={{
            position: 'fixed',
            top: 0,
            left: 0,
            right: 0,
            bottom: 0,
            backgroundColor: 'rgba(0, 0, 0, 0.55)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            zIndex: 1000,
            padding: '16px',
          }}
          onClick={closeCancelModal}
        >
          <div
            className="modal"
            style={{
              backgroundColor: '#ffffff',
              borderRadius: '12px',
              maxWidth: '480px',
              width: '100%',
              padding: '24px',
              boxShadow: '0 20px 25px -5px rgba(0, 0, 0, 0.1)',
            }}
            onClick={(e) => e.stopPropagation()}
          >
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '16px' }}>
              <h3 style={{ margin: 0, fontSize: '1.25rem', color: '#111827' }}>
                Hủy Đơn Thuê #{cancelBooking.Id}
              </h3>
              <button
                type="button"
                onClick={closeCancelModal}
                style={{ fontSize: '1.5rem', lineHeight: 1, color: '#9ca3af', cursor: 'pointer', background: 'none', border: 'none' }}
              >
                ×
              </button>
            </div>

            <div
              style={{
                backgroundColor: '#fef2f2',
                border: '1px solid #fee2e2',
                borderRadius: '8px',
                padding: '12px',
                marginBottom: '16px',
                fontSize: '0.875rem',
                color: '#991b1b',
              }}
            >
              <p style={{ margin: '0 0 4px 0' }}>
                <strong>Xe:</strong> {cancelBooking.CarName}
              </p>
              <p style={{ margin: 0 }}>
                <strong>Ngày thuê:</strong> {formatDate(cancelBooking.StartDate)} → {formatDate(cancelBooking.EndDate)}
              </p>
            </div>

            {modalError && (
              <div
                style={{
                  backgroundColor: '#fee2e2',
                  color: '#991b1b',
                  padding: '8px 12px',
                  borderRadius: '6px',
                  fontSize: '0.85rem',
                  marginBottom: '12px',
                }}
              >
                {modalError}
              </div>
            )}

            <form onSubmit={handleConfirmCancel}>
              <div style={{ marginBottom: '16px' }}>
                <label
                  htmlFor="cancelReason"
                  style={{ display: 'block', marginBottom: '6px', fontSize: '0.875rem', fontWeight: '500', color: '#374151' }}
                >
                  Lý do hủy <span style={{ color: '#dc2626' }}>*</span>
                </label>
                <textarea
                  id="cancelReason"
                  rows={4}
                  required
                  placeholder="Nhập lý do hủy đơn..."
                  value={cancelReason}
                  onChange={(e) => setCancelReason(e.target.value)}
                  style={{
                    width: '100%',
                    padding: '10px 12px',
                    borderRadius: '6px',
                    border: '1px solid #d1d5db',
                    fontSize: '0.9rem',
                    outline: 'none',
                    resize: 'vertical',
                    boxSizing: 'border-box',
                  }}
                />
              </div>

              <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '10px' }}>
                <button
                  type="button"
                  onClick={closeCancelModal}
                  disabled={actionLoading}
                  style={{
                    padding: '8px 16px',
                    backgroundColor: '#e5e7eb',
                    color: '#374151',
                    borderRadius: '6px',
                    fontWeight: '500',
                    fontSize: '0.875rem',
                    cursor: 'pointer',
                    border: 'none',
                  }}
                >
                  Đóng
                </button>
                <button
                  type="submit"
                  disabled={actionLoading}
                  style={{
                    padding: '8px 18px',
                    backgroundColor: '#dc2626',
                    color: '#ffffff',
                    borderRadius: '6px',
                    fontWeight: '600',
                    fontSize: '0.875rem',
                    cursor: actionLoading ? 'not-allowed' : 'pointer',
                    border: 'none',
                  }}
                >
                  {actionLoading ? 'Đang xử lý...' : 'Xác nhận hủy'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
