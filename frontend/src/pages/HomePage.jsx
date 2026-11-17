import React, { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import {
  FiSearch, FiShield, FiTrendingDown, FiCalendar, FiCheckCircle, FiMessageCircle,
  FiMapPin, FiClock, FiArrowRight,
} from 'react-icons/fi';
import axiosClient from '../api/axiosClient';
import useAuth from '../auth/AuthContext';
import CarCard from '../components/CarCard';
import { formatCurrency } from '../utils/formatCurrency';
import { roleLabel } from '../utils/roleLabels';
import './home.css';

const CAR_TYPES = [
  { value: 'Sedan', label: 'Sedan', note: 'Gọn gàng, tiết kiệm' },
  { value: 'SUV', label: 'SUV', note: 'Gầm cao, đi xa thoải mái' },
  { value: 'MPV', label: 'MPV', note: 'Cả gia đình đi chung' },
  { value: 'Hatchback', label: 'Hatchback', note: 'Dễ đi phố' },
  { value: 'Pickup', label: 'Bán tải', note: 'Chở nhiều, đi đường xấu' },
  { value: 'Luxury', label: 'Hạng sang', note: 'Cho dịp quan trọng' },
];

const STEPS = [
  { icon: FiSearch, title: 'Chọn xe', text: 'Lọc theo loại xe và mức giá, xem ảnh và thông tin từng xe.' },
  { icon: FiCalendar, title: 'Chọn ngày', text: 'Xem lịch xe còn trống và nhận báo giá ngay trước khi đặt.' },
  { icon: FiCheckCircle, title: 'Nhận xe', text: 'Nhân viên xác nhận đơn, bạn đến nhận xe và ký hợp đồng.' },
];

const BENEFITS = [
  { icon: FiTrendingDown, title: 'Giá thông minh', text: 'Mô hình AI gợi ý giá theo loại xe, tuổi xe và thời điểm thuê.' },
  { icon: FiShield, title: 'Minh bạch', text: 'Báo giá rõ từng ngày thuê, không phí ẩn, hủy đơn khi chưa nhận xe.' },
  { icon: FiMessageCircle, title: 'Trợ lý tư vấn', text: 'Hỏi trợ lý ảo để chọn xe phù hợp với số người và hành trình.' },
];

const ACTIVE_STATUS = { ChoXacNhan: 'Chờ xác nhận', DaXacNhan: 'Đã xác nhận', DangThue: 'Đang thuê' };
const FEATURED_PARAMS = { Page: 1, PageSize: 6, Status: 'SanSang' };

const HomePage = () => {
  const navigate = useNavigate();
  const auth = useAuth?.() || {};
  const isAuthenticated = Boolean(auth.isAuthenticated);
  const role = auth.user?.role;
  const isManager = role === 'Admin' || role === 'Staff';
  const [type, setType] = useState('');
  const [maxPrice, setMaxPrice] = useState('');
  const [cars, setCars] = useState([]);
  const [loading, setLoading] = useState(true);
  const [bookings, setBookings] = useState([]);

  useEffect(() => {
    axiosClient.get('/cars', { params: FEATURED_PARAMS })
      .then((res) => setCars(res.data?.items || (Array.isArray(res.data) ? res.data : [])))
      .catch(() => setCars([]))
      .finally(() => setLoading(false));
  }, []);

  useEffect(() => {
    if (!isAuthenticated) return;
    axiosClient.get('/bookings')
      .then((res) => setBookings(res.data?.items || (Array.isArray(res.data) ? res.data : [])))
      .catch(() => setBookings([]));
  }, [isAuthenticated]);

  const active = bookings.filter((b) => ACTIVE_STATUS[b.Status]);
  const next = [...active].sort((a, b) => new Date(a.StartDate) - new Date(b.StartDate))[0];
  const fmtDate = (d) => (d ? new Date(d).toLocaleDateString('vi-VN') : '-');

  const onSearch = (e) => {
    e.preventDefault();
    const q = new URLSearchParams();
    if (type) q.set('type', type);
    if (maxPrice) q.set('maxPrice', maxPrice);
    navigate(`/cars${q.toString() ? `?${q}` : ''}`);
  };

  return (
    <div className="home">
      <section className="hero">
        <div className="hero-inner">
          <p className="eyebrow">Thuê xe tự lái</p>
          <h1>
            {isAuthenticated
              ? `Xin chào, ${auth.user?.fullName || 'bạn'}`
              : 'Thuê xe tự lái dễ dàng, giá rõ ràng'}
          </h1>
          <p className="hero-sub">
            {isAuthenticated
              ? `Bạn đang đăng nhập với vai trò ${roleLabel(role)}. Chọn xe cho chuyến đi tiếp theo.`
              : 'Chọn xe, xem lịch trống và nhận báo giá ngay. Không cần gọi điện, không phí ẩn.'}
          </p>

          <form className="search-box" onSubmit={onSearch} aria-label="Tìm xe nhanh">
            <label>
              <span>Loại xe</span>
              <select value={type} onChange={(e) => setType(e.target.value)}>
                <option value="">Tất cả loại xe</option>
                {CAR_TYPES.map((t) => <option key={t.value} value={t.value}>{t.label}</option>)}
              </select>
            </label>
            <label>
              <span>Giá tối đa mỗi ngày</span>
              <select value={maxPrice} onChange={(e) => setMaxPrice(e.target.value)}>
                <option value="">Không giới hạn</option>
                <option value="600000">Đến 600.000 ₫</option>
                <option value="900000">Đến 900.000 ₫</option>
                <option value="1200000">Đến 1.200.000 ₫</option>
              </select>
            </label>
            <button type="submit" className="btn btn-primary home-cta">
              <FiSearch aria-hidden="true" /> Tìm xe
            </button>
          </form>

          {!isAuthenticated && (
            <div className="hero-actions">
              <Link to="/register" className="home-btn-light">Tạo tài khoản</Link>
              <Link to="/login" className="home-link-light">Đã có tài khoản? Đăng nhập</Link>
            </div>
          )}
        </div>
      </section>

      {isAuthenticated && (
        <section className="home-wrap dash">
          <div className="dash-card">
            <h2>Chuyến sắp tới</h2>
            {next ? (
              <>
                <p className="dash-car">{next.CarName}</p>
                <p className="dash-meta"><FiClock aria-hidden="true" /> {fmtDate(next.StartDate)} → {fmtDate(next.EndDate)}</p>
                <p className="dash-meta">{ACTIVE_STATUS[next.Status]} · {formatCurrency(next.TotalPrice)}</p>
                <Link to="/my-bookings" className="btn btn-primary btn-sm">Xem chi tiết</Link>
              </>
            ) : (
              <>
                <p className="dash-meta">Bạn chưa có chuyến nào sắp tới.</p>
                <Link to="/cars" className="btn btn-primary btn-sm">Đặt xe ngay</Link>
              </>
            )}
          </div>
          <div className="dash-card">
            <h2>Tổng quan</h2>
            <p className="dash-stat">{active.length}</p>
            <p className="dash-meta">đơn đang hoạt động</p>
            <p className="dash-stat">{bookings.length}</p>
            <p className="dash-meta">đơn đã tạo</p>
          </div>
          <div className="dash-card">
            <h2>Lối tắt</h2>
            <Link to="/my-bookings" className="dash-link">Lịch sử thuê <FiArrowRight aria-hidden="true" /></Link>
            <Link to="/cars" className="dash-link">Danh sách xe <FiArrowRight aria-hidden="true" /></Link>
            {isManager && <Link to="/admin/bookings" className="dash-link">Quản lý đơn <FiArrowRight aria-hidden="true" /></Link>}
            {role === 'Admin' && <Link to="/admin/reports" className="dash-link">Báo cáo <FiArrowRight aria-hidden="true" /></Link>}
          </div>
        </section>
      )}

      <section className="home-wrap section">
        <div className="section-head">
          <h2>Xe nổi bật</h2>
          <Link to="/cars" className="section-more">Xem tất cả <FiArrowRight aria-hidden="true" /></Link>
        </div>
        {loading ? (
          <div className="loading-spinner" />
        ) : cars.length ? (
          <div className="car-grid">
            {cars.map((c) => <CarCard key={c.CarId ?? c.carId ?? c.Id ?? c.id} car={c} />)}
          </div>
        ) : (
          <p className="text-muted">Chưa có xe nào sẵn sàng.</p>
        )}
      </section>

      <section className="home-wrap section">
        <div className="section-head"><h2>Chọn theo loại xe</h2></div>
        <div className="type-grid">
          {CAR_TYPES.map((t) => (
            <Link key={t.value} to={`/cars?type=${t.value}`} className="type-card">
              <strong>{t.label}</strong>
              <span>{t.note}</span>
            </Link>
          ))}
        </div>
      </section>

      <section className="steps-band">
        <div className="home-wrap">
          <div className="section-head"><h2>Thuê xe trong 3 bước</h2></div>
          <div className="steps">
            {STEPS.map((s, i) => (
              <div key={s.title} className="step">
                <span className="step-icon"><s.icon aria-hidden="true" /></span>
                <h3>{i + 1}. {s.title}</h3>
                <p>{s.text}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      <section className="home-wrap section">
        <div className="section-head"><h2>Vì sao chọn Car Rental AI</h2></div>
        <div className="steps">
          {BENEFITS.map((b) => (
            <div key={b.title} className="step">
              <span className="step-icon"><b.icon aria-hidden="true" /></span>
              <h3>{b.title}</h3>
              <p>{b.text}</p>
            </div>
          ))}
        </div>
      </section>

      {!isAuthenticated && (
        <section className="home-wrap section">
          <div className="cta-band">
            <div>
              <h2>Sẵn sàng cho chuyến đi tiếp theo?</h2>
              <p><FiMapPin aria-hidden="true" /> Đăng ký miễn phí và đặt xe trong vài phút.</p>
            </div>
            <Link to="/register" className="home-btn-light">Đăng ký ngay</Link>
          </div>
        </section>
      )}
    </div>
  );
};

export default HomePage;
