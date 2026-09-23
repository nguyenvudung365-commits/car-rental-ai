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
  const [hasDriver, setHasDriver] = useState(false);

  // Giá AI
  const [aiPrice, setAiPrice] = useState(null);
  const [isFallback, setIsFallback] = useState(false);
  const [loadingPrice, setLoadingPrice] = useState(false);

  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(true);

  // Load thông tin xe
  useEffect(() => {
    axiosClient.get(`/cars/${carId}`)
      .then(res => setCar(res.data))
      .catch(() => setError('Không thể tải thông tin xe'))
      .finally(() => setLoading(false));
  }, [carId]);

  // Gọi API dự đoán giá AI khi chọn đủ ngày
  useEffect(() => {
    if (!startDate || !endDate) return;
    if (new Date(endDate) <= new Date(startDate)) return;

    setLoadingPrice(true);
    setAiPrice(null);
    setIsFallback(false);

    axiosClient.post('/ai/quote', { carId, startDate, endDate })
      .then(res => {
        setAiPrice(res.data.suggestedPrice);
        setIsFallback(res.data.isFallback);
      })
      .catch(() => {
        // Nếu gọi AI lỗi → fallback, không báo lỗi cho khách
        setIsFallback(true);
      })
      .finally(() => setLoadingPrice(false));
  }, [carId, startDate, endDate]);

  // Tính giá niêm yết
  const totalDays = startDate && endDate
    ? Math.max(1, Math.ceil((new Date(endDate) - new Date(startDate)) / (1000 * 60 * 60 * 24)))
    : 0;
  const basePrice = car ? car.basePricePerDay * totalDays : 0;

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
      await axiosClient.post('/bookings', {
        carId,
        startDate,
        endDate,
        hasDriver,
      });
      navigate('/my-bookings');
    } catch (err) {
      setError(err.response?.data?.message || 'Đặt xe thất bại, vui lòng thử lại');
    } finally {
      setSubmitting(false);
    }
  };

  if (loading) return <div className="page-container"><div className="loading-spinner"></div></div>;
  if (!car) return <div className="page-container"><p>Không tìm thấy xe</p></div>;

  return (
    <div className="page-container">
      <h1 className="page-title">Đặt xe</h1>

      {/* Tóm tắt xe */}
      <div className="booking-summary">
        <div className="booking-car-info">
          {car.primaryImageUrl ? (
            <img
              src={car.primaryImageUrl}
              alt={`${car.brand} ${car.model}`}
              style={{ width: 200, height: 140, objectFit: 'cover', borderRadius: 8 }}
            />
          ) : (
            <div style={{
              width: 200, height: 140, background: '#e5e7eb', borderRadius: 8,
              display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: 48
            }}>🚗</div>
          )}
          <div style={{ marginLeft: 20 }}>
            <h2 style={{ margin: 0 }}>{car.brand} {car.model}</h2>
            <p style={{ color: '#6b7280', margin: '4px 0' }}>{car.carType} · {car.licensePlate}</p>
            <p style={{ fontSize: 18, fontWeight: 600, color: '#2563eb' }}>
              {formatCurrency(car.basePricePerDay)}/ngày
            </p>
          </div>
        </div>
      </div>

      {/* Form đặt xe */}
      <form onSubmit={handleSubmit} style={{ marginTop: 24 }}>
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16, maxWidth: 500 }}>
          <div className="form-group">
            <label className="form-label">Ngày nhận xe</label>
            <input
              type="date"
              className="form-input"
              value={startDate}
              onChange={(e) => setStartDate(e.target.value)}
              min={new Date().toISOString().split('T')[0]}
              required
            />
          </div>
          <div className="form-group">
            <label className="form-label">Ngày trả xe</label>
            <input
              type="date"
              className="form-input"
              value={endDate}
              onChange={(e) => setEndDate(e.target.value)}
              min={startDate || new Date().toISOString().split('T')[0]}
              required
            />
          </div>
        </div>

        <div className="form-group" style={{ marginTop: 12 }}>
          <label style={{ display: 'flex', alignItems: 'center', gap: 8, cursor: 'pointer' }}>
            <input
              type="checkbox"
              checked={hasDriver}
              onChange={(e) => setHasDriver(e.target.checked)}
            />
            <span>Thuê có tài xế</span>
          </label>
        </div>

        {/* So sánh giá */}
        {totalDays > 0 && (
          <div className="price-compare" style={{ marginTop: 24 }}>
            <h3 style={{ marginBottom: 12 }}>Chi phí dự kiến ({totalDays} ngày)</h3>

            <div className="price-row">
              <span className="price-label">Giá niêm yết:</span>
              <span className="price-value">{formatCurrency(basePrice)}</span>
            </div>

            {loadingPrice && (
              <div style={{ padding: '8px 0', color: '#6b7280' }}>
                <span className="loading-spinner" style={{ width: 16, height: 16, display: 'inline-block', marginRight: 8 }}></span>
                Đang tính giá AI...
              </div>
            )}

            {/* Hiển thị giá AI nếu KHÔNG fallback */}
            {!loadingPrice && !isFallback && aiPrice != null && (
              <div className="price-ai">
                <div className="price-row">
                  <span className="price-label">Giá AI gợi ý:</span>
                  <span className="price-value" style={{ color: '#16a34a' }}>
                    {formatCurrency(aiPrice)}
                  </span>
                </div>
                <p className="price-note">
                  💡 Giá AI gợi ý dựa trên nhu cầu thực tế
                </p>
              </div>
            )}

            {/* Nếu isFallback = true → chỉ hiện giá niêm yết, KHÔNG báo lỗi */}
          </div>
        )}

        {error && <p className="form-error" style={{ marginTop: 12 }}>{error}</p>}

        <button
          type="submit"
          className="btn btn-primary"
          style={{ marginTop: 20, width: '100%', maxWidth: 500 }}
          disabled={submitting || !startDate || !endDate}
        >
          {submitting ? 'Đang xử lý...' : 'Xác nhận đặt xe'}
        </button>
      </form>
    </div>
  );
}
