import { useState, useEffect } from 'react';
import { primaryImageOf } from '../utils/imageUrl';
import { useParams, useNavigate, useSearchParams } from 'react-router-dom';
import axiosClient from '../api/axiosClient';
import { formatCurrency } from '../utils/formatCurrency';
import { carTypeLabel } from '../utils/carLabels';
import { parseApiError } from '../utils/validators';
import PriceBreakdown from '../components/PriceBreakdown';
import { parseUtc, formatLocalDateTime } from '../utils/utcTime';

const NOTE_MAX = 500;
const pad = (n) => String(n).padStart(2, '0');
// Ngày cục bộ YYYY-MM-DD (toISOString lệch ngày theo múi giờ UTC).
const todayStr = () => { const d = new Date(); return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`; };
const MODE_DAY = 1;
const MODE_HOUR = 2;

export default function BookingPage() {
  const { carId } = useParams();
  const navigate = useNavigate();

  const [car, setCar] = useState(null);
  const [search] = useSearchParams();
  const [startDate, setStartDate] = useState(search.get('start') || '');
  const [endDate, setEndDate] = useState(search.get('end') || '');
  const [note, setNote] = useState('');
  const [mode, setMode] = useState(MODE_DAY);
  const [startTime, setStartTime] = useState('08:00');
  const [hours, setHours] = useState(4);
  const [withDriver, setWithDriver] = useState(false);
  const [delivery, setDelivery] = useState(false);
  const [returnHome, setReturnHome] = useState(false);
  const [deliveryAddress, setDeliveryAddress] = useState('');
  const [returnAddress, setReturnAddress] = useState('');
  const [couponCode, setCouponCode] = useState('');
  const [couponInfo, setCouponInfo] = useState('');
  const [branches, setBranches] = useState([]);
  const [returnBranchId, setReturnBranchId] = useState('');

  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState('');
  const [fieldErrors, setFieldErrors] = useState({});
  const [loading, setLoading] = useState(true);
  const [bookingResult, setBookingResult] = useState(null);
  const [bookedRanges, setBookedRanges] = useState([]);
  const [estimate, setEstimate] = useState(null);
  const [estimating, setEstimating] = useState(false);

  // Load thông tin xe
  useEffect(() => {
    axiosClient.get(`/cars/${carId}`)
      .then(res => setCar(res.data))
      .catch(() => setError('Không thể tải thông tin xe'))
      .finally(() => setLoading(false));
  }, [carId]);

  // Các khoảng ngày xe đã có người đặt
  useEffect(() => {
    axiosClient.get(`/bookings/car/${carId}/booked-ranges`)
      .then(res => setBookedRanges(res.data || []))
      .catch(() => setBookedRanges([]));
  }, [carId]);

  useEffect(() => {
    axiosClient.get('/branches').then(res => setBranches(res.data || [])).catch(() => setBranches([]));
  }, []);

  const hourly = mode === MODE_HOUR;
  // Khoảng thời gian gửi lên API: theo ngày dùng ngày thuần, theo giờ dùng ngày giờ + số giờ.
  const effStart = hourly && startDate ? new Date(`${startDate}T${startTime}:00`).toISOString() : startDate;
  const effEnd = hourly
    ? (startDate ? new Date(new Date(effStart).getTime() + Number(hours) * 3600000).toISOString() : '')
    : endDate;
  const toDay = (d) => String(d).slice(0, 10);
  const overlapRange = startDate && effEnd
    ? bookedRanges.find(r => hourly
        ? parseUtc(r.StartDate) < new Date(effEnd) && parseUtc(r.EndDate) > new Date(effStart)
        : toDay(r.StartDate) < endDate && toDay(r.EndDate) > startDate)
    : null;
  // Tùy chọn gửi kèm khi báo giá và đặt xe.
  const options = {
    mode,
    withDriver,
    couponCode: couponCode.trim() || undefined,
    deliveryAddress: delivery && deliveryAddress.trim() ? deliveryAddress.trim() : undefined,
    returnAddress: returnHome && returnAddress.trim() ? returnAddress.trim() : undefined,
    returnBranchId: returnBranchId ? Number(returnBranchId) : undefined,
  };
  const optionsKey = JSON.stringify(options);

  // Báo giá AI khi đã chọn khoảng ngày hợp lệ và không trùng lịch
  useEffect(() => {
    setEstimate(null);
    setCouponInfo('');
    if (!startDate || !effEnd || (!hourly && endDate <= startDate) || startDate < todayStr() || overlapRange) return undefined;
    let cancelled = false;
    setEstimating(true);
    const timer = setTimeout(() => {
      axiosClient.post('/bookings/estimate', { carId: Number(carId), startDate: effStart, endDate: effEnd, ...options })
        .then(res => { if (!cancelled) { setEstimate(res.data); if (options.couponCode) setCouponInfo('Đã áp dụng mã giảm giá.'); } })
        .catch((err) => { if (!cancelled) { setEstimate(null); if (options.couponCode) setCouponInfo(parseApiError(err).message || 'Mã giảm giá không hợp lệ.'); } })
        .finally(() => { if (!cancelled) setEstimating(false); });
    }, 400);
    return () => { cancelled = true; clearTimeout(timer); setEstimating(false); };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [carId, startDate, effStart, effEnd, hourly, endDate, overlapRange, optionsKey]);

  // Tính số ngày thuê
  const totalDays = hourly ? (startDate ? 1 : 0) : startDate && endDate
    ? Math.max(1, Math.ceil((new Date(endDate) - new Date(startDate)) / (1000 * 60 * 60 * 24)))
    : 0;
  const basePrice = car ? (car.BasePricePerDay ?? car.basePricePerDay ?? 0) * totalDays : 0;

  // Xác nhận đặt xe
  const handleSubmit = async (e) => {
    e.preventDefault();
    const errors = {};
    if (!startDate) errors.startDate = 'Vui lòng chọn ngày nhận xe.';
    else if (startDate < todayStr()) errors.startDate = 'Ngày nhận xe không được ở quá khứ.';
    if (hourly) {
      if (startDate && hourly && new Date(effStart) < new Date(Date.now() + 2 * 3600000)) errors.startDate = 'Thuê theo giờ phải đặt trước ít nhất 2 giờ.';
    else if (startDate && new Date(effStart) <= new Date()) errors.startDate = 'Giờ nhận xe phải ở tương lai.';
    } else if (!endDate) errors.endDate = 'Vui lòng chọn ngày trả xe.';
    else if (startDate && endDate <= startDate) errors.endDate = 'Ngày trả xe phải sau ngày nhận xe.';
    if (delivery && !deliveryAddress.trim()) errors.deliveryAddress = 'Vui lòng nhập địa chỉ giao xe.';
    if (returnHome && !returnAddress.trim()) errors.returnAddress = 'Vui lòng nhập địa chỉ nhận xe.';
    if (!errors.startDate && !errors.endDate && overlapRange) {
      errors.startDate = 'Xe đã có người đặt trong khoảng thời gian này. Hãy chọn ngày khác.';
    }
    if (note.length > NOTE_MAX) errors.note = `Ghi chú tối đa ${NOTE_MAX} ký tự.`;
    setFieldErrors(errors);
    if (Object.keys(errors).length > 0) return;

    setSubmitting(true);
    setError('');

    try {
      const response = await axiosClient.post('/bookings', {
        carId: Number(carId),
        startDate: effStart,
        endDate: effEnd,
        note: note || undefined,
        ...options,
      });
      setBookingResult(response.data);
    } catch (err) {
      const { code, message } = parseApiError(err);
      if (code === 'BOOKING_OVERLAP') {
        setFieldErrors({ startDate: 'Xe đã có người đặt trong khoảng thời gian này. Hãy chọn ngày khác.' });
      } else if (code?.startsWith('COUPON_')) {
        setError(message || 'Mã giảm giá không dùng được.');
      } else if (code === 'CUSTOMER_BLOCKED') {
        setError('Tài khoản của bạn hiện không thể đặt xe. Vui lòng liên hệ cửa hàng.');
      } else if (code === 'CAR_NOT_AVAILABLE') {
        setError('Xe hiện không sẵn sàng cho thuê. Vui lòng chọn xe khác.');
      } else if (err.response?.status === 401) {
        setError('Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.');
      } else {
        setError(message || 'Đặt xe thất bại, vui lòng thử lại');
      }
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
          <p style={{ marginTop: 4 }}>{bookingResult.Mode === MODE_HOUR ? `${formatLocalDateTime(bookingResult.StartDate)} → ${formatLocalDateTime(bookingResult.EndDate)}` : `${bookingResult.StartDate?.slice(0, 10)} → ${bookingResult.EndDate?.slice(0, 10)}`} ({bookingResult.Mode === MODE_HOUR ? 'thuê theo giờ' : `${bookingResult.RentalDays} ngày`})</p>
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
            <div style={{ marginTop: 8 }}>
              <PriceBreakdown
                rentalAmount={bookingResult.TotalPrice - (bookingResult.Charges || []).reduce((a, c) => a + c.Amount, 0)}
                charges={bookingResult.Charges}
                total={bookingResult.TotalPrice}
              />
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
          {primaryImageOf(car) ? (
            <img src={primaryImageOf(car)} alt={`${car.Brand || car.brand} ${car.Model || car.model}`} style={{ width: 200, height: 140, objectFit: 'cover', borderRadius: 8 }} />
          ) : (
            <div style={{ width: 200, height: 140, background: '#e5e7eb', borderRadius: 8, display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: 48 }}>🚗</div>
          )}
          <div style={{ marginLeft: 20 }}>
            <h2 style={{ margin: 0 }}>{car.Brand || car.brand} {car.Model || car.model}</h2>
            <p style={{ color: '#6b7280', margin: '4px 0' }}>{carTypeLabel(car.CarType ?? car.carType)} · {car.LicensePlate || car.licensePlate}</p>
            <p style={{ fontSize: 18, fontWeight: 600, color: '#2563eb' }}>{formatCurrency(car.BasePricePerDay ?? car.basePricePerDay)}/ngày</p>
          </div>
        </div>
      </div>

      <form onSubmit={handleSubmit} style={{ marginTop: 24 }}>
        <div className="form-group" style={{ maxWidth: 500 }}>
          <label className="form-label">Hình thức thuê</label>
          <select className="form-input" value={mode} onChange={(e) => setMode(Number(e.target.value))}>
            <option value={MODE_DAY}>Theo ngày</option>
            <option value={MODE_HOUR}>Theo giờ (4 đến 12 giờ)</option>
          </select>
        </div>
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16, maxWidth: 500 }}>
          <div className="form-group">
            <label className="form-label">Ngày nhận xe</label>
            <input type="date" className={`form-input ${fieldErrors.startDate ? 'invalid' : ''}`} value={startDate} onBlur={() => setFieldErrors((f) => ({ ...f, startDate: !startDate ? 'Vui lòng chọn ngày nhận xe.' : startDate < todayStr() ? 'Ngày nhận xe không được ở quá khứ.' : '' }))} onChange={(e) => { setStartDate(e.target.value); setFieldErrors((f) => ({ ...f, startDate: '', endDate: '' })); }} min={todayStr()} required />
            {fieldErrors.startDate && <span className="field-error" role="alert">{fieldErrors.startDate}</span>}
          </div>
          {hourly ? (
            <div className="form-group">
              <label className="form-label">Giờ nhận xe và số giờ thuê</label>
              <div style={{ display: 'flex', gap: 8 }}>
                <input type="time" className="form-input" value={startTime} onChange={(e) => setStartTime(e.target.value)} required />
                <input type="number" className="form-input" min={4} max={12} value={hours} onChange={(e) => setHours(Math.min(12, Math.max(4, Number(e.target.value) || 4)))} aria-label="Số giờ thuê" />
              </div>
            </div>
          ) : (
          <div className="form-group">
            <label className="form-label">Ngày trả xe</label>
            <input type="date" className={`form-input ${fieldErrors.endDate ? 'invalid' : ''}`} value={endDate} onBlur={() => setFieldErrors((f) => ({ ...f, endDate: !endDate ? 'Vui lòng chọn ngày trả xe.' : startDate && endDate <= startDate ? 'Ngày trả xe phải sau ngày nhận xe.' : '' }))} onChange={(e) => { setEndDate(e.target.value); setFieldErrors((f) => ({ ...f, startDate: '', endDate: '' })); }} min={startDate || todayStr()} required />
            {overlapRange && !fieldErrors.startDate && <span className="field-error" role="alert">Khoảng ngày này trùng lịch đã có người đặt.</span>}
            {fieldErrors.endDate && <span className="field-error" role="alert">{fieldErrors.endDate}</span>}
          </div>
          )}
        </div>

        <div style={{ marginTop: 12, maxWidth: 500, display: 'grid', gap: 8 }}>
          <label><input type="checkbox" checked={withDriver} onChange={(e) => setWithDriver(e.target.checked)} /> Thuê kèm tài xế</label>
          <label><input type="checkbox" checked={delivery} onChange={(e) => setDelivery(e.target.checked)} /> Giao xe tận nơi (có phí)</label>
          {delivery && (
            <>
              <input className="form-input" maxLength={300} placeholder="Địa chỉ giao xe" value={deliveryAddress} onChange={(e) => setDeliveryAddress(e.target.value)} />
              {fieldErrors.deliveryAddress && <span className="field-error" role="alert">{fieldErrors.deliveryAddress}</span>}
            </>
          )}
          <label><input type="checkbox" checked={returnHome} onChange={(e) => setReturnHome(e.target.checked)} /> Nhận lại xe tận nơi (có phí)</label>
          {returnHome && (
            <>
              <input className="form-input" maxLength={300} placeholder="Địa chỉ nhận xe" value={returnAddress} onChange={(e) => setReturnAddress(e.target.value)} />
              {fieldErrors.returnAddress && <span className="field-error" role="alert">{fieldErrors.returnAddress}</span>}
            </>
          )}
          {branches.length > 1 && (
            <label>Trả xe tại chi nhánh
              <select className="form-input" value={returnBranchId} onChange={(e) => setReturnBranchId(e.target.value)}>
                <option value="">Cùng chi nhánh nhận xe</option>
                {branches.map(b => <option key={b.Id} value={b.Id}>{b.Name}</option>)}
              </select>
            </label>
          )}
          <label>Mã giảm giá
            <input className="form-input" maxLength={40} value={couponCode} onChange={(e) => setCouponCode(e.target.value.toUpperCase())} placeholder="Nhập mã nếu có" />
          </label>
          {couponInfo && <span style={{ fontSize: 13, color: couponInfo.startsWith('Đã') ? '#15803d' : '#b91c1c' }}>{couponInfo}</span>}
        </div>

        <div className="form-group" style={{ marginTop: 12, maxWidth: 500 }}>
          <label className="form-label">Ghi chú</label>
          <textarea className={`form-input ${fieldErrors.note ? 'invalid' : ''}`} value={note} onChange={(e) => { setNote(e.target.value); setFieldErrors((f) => ({ ...f, note: '' })); }} rows={3} placeholder="Yêu cầu thêm..." />
          <span style={{ fontSize: 12, color: '#6b7280', alignSelf: 'flex-end' }}>{note.length}/{NOTE_MAX}</span>
          {fieldErrors.note && <span className="field-error" role="alert">{fieldErrors.note}</span>}
        </div>

        {bookedRanges.length > 0 && (
          <div style={{ marginTop: 8, maxWidth: 500, fontSize: 13, color: '#92400e', background: '#fef3c7', padding: '8px 12px', borderRadius: 6 }}>
            Xe đã được đặt trong: {bookedRanges.map(r => (r.EndDate && parseUtc(r.EndDate) - parseUtc(r.StartDate) < 86400000 ? `${formatLocalDateTime(r.StartDate)} → ${formatLocalDateTime(r.EndDate)}` : `${toDay(r.StartDate)} → ${toDay(r.EndDate)}`)).join('; ')}
          </div>
        )}

        {totalDays > 0 && (
          <div className="price-compare" style={{ marginTop: 16, maxWidth: 500, background: '#f9fafb', padding: 16, borderRadius: 8 }}>
            <p style={{ fontWeight: 600 }}>Chi phí dự kiến ({hourly ? `${hours} giờ` : `${totalDays} ngày`})</p>
            <div style={{ display: 'flex', justifyContent: 'space-between', marginTop: 8 }}>
              <span>Giá niêm yết:</span>
              <span>{formatCurrency(basePrice)}</span>
            </div>
            {estimating && <p style={{ fontSize: 13, color: '#6b7280', marginTop: 8 }}>Đang tính giá AI...</p>}
            {estimate && (
              <>
                <div style={{ display: 'flex', justifyContent: 'space-between', marginTop: 8 }}>
                  <span>{estimate.IsFallback ? 'Giá áp dụng/ngày (AI không khả dụng):' : 'Giá AI gợi ý/ngày:'}</span>
                  <span style={{ color: '#16a34a', fontWeight: 600 }}>{formatCurrency(estimate.FinalPricePerDay)}</span>
                </div>
                <div style={{ marginTop: 8 }}>
                  <PriceBreakdown rentalAmount={estimate.RentalAmount} charges={estimate.Charges} total={estimate.TotalPrice} />
                </div>
              </>
            )}
            <p style={{ fontSize: 12, color: '#6b7280', marginTop: 8 }}>Giá cuối cùng được chốt khi bạn xác nhận đặt xe.</p>
          </div>
        )}

        {error && <p className="form-error" role="alert" style={{ marginTop: 12 }}>{error}</p>}

        <button type="submit" className="btn btn-primary" style={{ marginTop: 20, width: '100%', maxWidth: 500 }} disabled={submitting || !startDate || !effEnd || !!overlapRange}>
          {submitting ? 'Đang xử lý...' : 'Xác nhận đặt xe'}
        </button>
      </form>
    </div>
  );
}
