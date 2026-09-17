import { useState, useEffect } from 'react';
import axiosClient from '../api/axiosClient';
import { formatCurrency } from '../utils/formatCurrency';

const STATUS_MAP = {
  Pending:   { label: 'Chờ duyệt',  className: 'badge badge-pending'   },
  Confirmed: { label: 'Đã duyệt',   className: 'badge badge-confirmed' },
  Cancelled: { label: 'Đã hủy',     className: 'badge badge-cancelled' },
  Completed: { label: 'Hoàn thành',  className: 'badge badge-completed' },
};

export default function MyBookingsPage() {
  const [bookings, setBookings] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    axiosClient.get('/bookings/my')
      .then(res => setBookings(res.data))
      .catch(() => setError('Không thể tải lịch sử thuê xe'))
      .finally(() => setLoading(false));
  }, []);

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
                  <th>Giá</th>
                  <th>Trạng thái</th>
                  <th>Hợp đồng</th>
                </tr>
              </thead>
              <tbody>
                {bookings.map(b => {
                  const st = STATUS_MAP[b.status] || { label: b.status, className: 'badge' };
                  return (
                    <tr key={b.id}>
                      <td>{b.carBrand} {b.carModel}</td>
                      <td>{b.licensePlate}</td>
                      <td>{b.startDate} → {b.endDate}</td>
                      <td>{formatCurrency(b.finalPrice)}</td>
                      <td><span className={st.className}>{st.label}</span></td>
                      <td>
                        {b.contractPdfUrl ? (
                          <a
                            href={b.contractPdfUrl}
                            target="_blank"
                            rel="noopener noreferrer"
                            className="btn btn-sm btn-secondary"
                          >
                            Tải PDF
                          </a>
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
              const st = STATUS_MAP[b.status] || { label: b.status, className: 'badge' };
              return (
                <div key={b.id} className="admin-card">
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 8 }}>
                    <strong>{b.carBrand} {b.carModel}</strong>
                    <span className={st.className}>{st.label}</span>
                  </div>
                  <p style={{ color: '#6b7280', margin: '4px 0' }}>Biển số: {b.licensePlate}</p>
                  <p style={{ margin: '4px 0' }}>📅 {b.startDate} → {b.endDate}</p>
                  <p style={{ fontWeight: 600, color: '#2563eb', margin: '4px 0' }}>
                    {formatCurrency(b.finalPrice)}
                  </p>
                  {b.contractPdfUrl && (
                    <a
                      href={b.contractPdfUrl}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="btn btn-sm btn-secondary"
                      style={{ marginTop: 8 }}
                    >
                      Tải hợp đồng PDF
                    </a>
                  )}
                </div>
              );
            })}
          </div>
        </>
      )}
    </div>
  );
}
