import { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import axiosClient from '../api/axiosClient';
import { formatCurrency } from '../utils/formatCurrency';

export default function BookingPage() {
  const { carId } = useParams();
  const navigate = useNavigate();

  const [car, setCar] = useState(null);
  const [startDate, setStartDate] = useState('');
  const [endDate, setEndDate] = useState('');
  const [note, setNote] = useState('');

  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(true);
  const [bookingResult, setBookingResult] = useState(null);

  // Load thông tin xe
  useEffect(() => {
    axiosClient.get(`/cars/${carId}`)
      .then(res => setCar(res.data))
      .catch(() => setError('Không thể tải thông tin xe'))
      .finally(() => setLoading(false));
  }, [carId]);

  // Tính số ngày thuê
  const totalDays = startDate && endDate
    ? Math.max(1, Math.ceil((new Date(endDate) - new Date(startDate)) / (1000 * 60 * 60 * 24)))
    : 0;
  const basePrice = car ? (car.BasePricePerDay ?? car.basePricePerDay ?? 0) * totalDays : 0;

  // Xác nhận đặt xe
  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!startDate || !endDate) {
      setError('Vui lòng chọn ngày nhận và trả xe');
      return;
    }
    if (new Date(endDate) <= new Date(startDate)) {
      setError('Ngày trả phải sau ngày nhận');
      return;
    }

    setSubmitting(true);
    setError('');

    try {
      const response = await axiosClient.post('/bookings', {
        carId: Number(carId),
        startDate,
        endDate,
        note: note || undefined,
      });
      setBookingResult(response.data);
    } catch (err) {
      setError(err.response?.data?.message || 'Đặt xe thất bại, vui lòng thử lại');
    } finally {
      setSubmitting(false);
    }
  };

  if (loading) return <div className="page-container"><div className="loading-spinner"></div></div>;
  if (!car) return <div className="page-container"><p>Không tìm thấy xe</p></div>;

  if (bookingResult) {
    return (
      <div className="page-container">
        <h1 className="page-title">Đặt xe thành công</h1>
        <div style={{ maxWidth: 500, background: '#f0fdf4', border: '1px solid #bbf7d0', borderRadius: 12, padding: 24 }}>
          <p style={{ fontSize: 18, fontWeight: 600, color: '#15803d' }}>Đơn #{bookingResult.Id} đã được tạo</p>
          <p style={{ color: '#374151', marginTop: 8 }}>{bookingResult.CarName} · {bookingResult.LicensePlate}</p>
          <p style={{ marginTop: 4 }}>{bookingResult.StartDate?.slice(0, 10)} → {bookingResult.EndDate?.slice(0, 10)} ({bookingResult.RentalDays} ngày)</p>
          <div style={{ marginTop: 16, borderTop: '1px solid #bbf7d0', paddingTop: 16 }}>
            {bookingResult.PredictedPricePerDay != null && (
              <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                <span>Giá AI gợi ý/ngày:</span>
                <span style={{ color: '#16a34a', fontWeight: 600 }}>{formatCurrency(bookingResult.PredictedPricePerDay)}</span>
              </div>
            )}
            <div style={{ display: 'flex', justifyContent: 'space-between', marginTop: 8 }}>
              <span>Giá áp dụng/ngày:</span>
              <span style={{ fontWeight: 700, color: '#2563eb' }}>{formatCurrency(bookingResult.FinalPricePerDay)}</span>
            </div>
            <div style={{ display: 'flex', justifyContent: 'space-between', marginTop: 8, fontSize: 18 }}>
              <span>Tổng tiền:</span>
              <span style={{ fontWeight: 700 }}>{formatCurrency(bookingResult.TotalPrice)}</span>
            </div>
            {bookingResult.IsFallback && (
              <p style={{ marginTop: 8, fontSize: 13, color: '#92400e', background: '#fef3c7', padding: '6px 10px', borderRadius: 6 }}>
                Giá niêm yết được áp dụng (AI tạm thời không khả dụng)
              </p>
            )}
          </div>
          <button className="btn btn-primary" style={{ marginTop: 20, width: '100%' }} onClick={() => navigate('/my-bookings')}>
            Xem lịch sử thuê xe
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="page-container">
      <h1 className="page-title">Đặt xe</h1>

      <div className="booking-summary">
        <div className="booking-car-info">
          {(car.PrimaryImageUrl || car.primaryImageUrl) ? (
            <img src={car.PrimaryImageUrl || car.primaryImageUrl} alt={`${car.Brand || car.brand} ${car.Model || car.model}`} style={{ width: 200, height: 140, objectFit: 'cover', borderRadius: 8 }} />
          ) : (
            <div style={{ width: 200, height: 140, background: '#e5e7eb', borderRadius: 8, display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: 48 }}>🚗</div>
          )}
          <div style={{ marginLeft: 20 }}>
            <h2 style={{ margin: 0 }}>{car.Brand || car.brand} {car.Model || car.model}</h2>
            <p style={{ color: '#6b7280', margin: '4px 0' }}>{car.CarType || car.carType} · {car.LicensePlate || car.licensePlate}</p>
            <p style={{ fontSize: 18, fontWeight: 600, color: '#2563eb' }}>{formatCurrency(car.BasePricePerDay ?? car.basePricePerDay)}/ngày</p>
          </div>
        </div>
      </div>

      <form onSubmit={handleSubmit} style={{ marginTop: 24 }}>
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16, maxWidth: 500 }}>
          <div className="form-group">
            <label className="form-label">Ngày nhận xe</label>
            <input type="date" className="form-input" value={startDate} onChange={(e) => setStartDate(e.target.value)} min={new Date().toISOString().split('T')[0]} required />
          </div>
          <div className="form-group">
            <label className="form-label">Ngày trả xe</label>
            <input type="date" className="form-input" value={endDate} onChange={(e) => setEndDate(e.target.value)} min={startDate || new Date().toISOString().split('T')[0]} required />
          </div>
        </div>

        <div className="form-group" style={{ marginTop: 12, maxWidth: 500 }}>
          <label className="form-label">Ghi chú</label>
          <textarea className="form-input" value={note} onChange={(e) => setNote(e.target.value)} rows={3} placeholder="Yêu cầu thêm..." />
        </div>

        {totalDays > 0 && (
          <div className="price-compare" style={{ marginTop: 16, maxWidth: 500, background: '#f9fafb', padding: 16, borderRadius: 8 }}>
            <p style={{ fontWeight: 600 }}>Chi phí dự kiến ({totalDays} ngày)</p>
            <div style={{ display: 'flex', justifyContent: 'space-between', marginTop: 8 }}>
              <span>Giá niêm yết:</span>
              <span>{formatCurrency(basePrice)}</span>
            </div>
            <p style={{ fontSize: 12, color: '#6b7280', marginTop: 8 }}>Giá AI sẽ được tính khi bạn xác nhận đặt xe.</p>
          </div>
        )}

        {error && <p className="form-error" style={{ marginTop: 12 }}>{error}</p>}

        <button type="submit" className="btn btn-primary" style={{ marginTop: 20, width: '100%', maxWidth: 500 }} disabled={submitting || !startDate || !endDate}>
          {submitting ? 'Đang xử lý...' : 'Xác nhận đặt xe'}
        </button>
      </form>
    </div>
  );
}
