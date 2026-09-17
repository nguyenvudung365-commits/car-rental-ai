import React, { useState, useEffect } from 'react';
import { useParams, useNavigate, useLocation, Link } from 'react-router-dom';
import axiosClient from '../api/axiosClient';
import ImageGallery from '../components/ImageGallery';
import { formatCurrency } from '../utils/formatCurrency';
import useAuth from '../auth/AuthContext';

/**
 * Trang Chi tiết xe (CarDetailPage)
 * Hiển thị hình ảnh chi tiết, thông số kỹ thuật đầy đủ và nút đặt xe
 */
const CarDetailPage = () => {
  const { id } = useParams();
  const navigate = useNavigate();
  const location = useLocation();
  const auth = useAuth();

  // Kiểm tra trạng thái đăng nhập
  const isAuthenticated = Boolean(auth?.isAuthenticated ?? (auth?.user || auth?.token));

  const [car, setCar] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  /**
   * Gọi API lấy thông tin chi tiết của xe theo ID
   * Endpoint: GET /cars/:id
   */
  useEffect(() => {
    const fetchCarDetail = async () => {
      if (!id) return;
      try {
        setLoading(true);
        setError(null);
        const response = await axiosClient.get(`/cars/${id}`);
        setCar(response.data);
      } catch (err) {
        console.error('Lỗi khi tải thông tin xe:', err);
        setError(
          err.response?.status === 404
            ? 'Không tìm thấy thông tin xe yêu cầu.'
            : 'Đã xảy ra lỗi khi tải dữ liệu xe. Vui lòng thử lại sau.'
        );
      } finally {
        setLoading(false);
      }
    };

    fetchCarDetail();
  }, [id]);

  // Chuyển đổi tên hộp số sang tiếng Việt
  const formatTransmission = (val) => {
    if (val === 'Automatic') return 'Tự động';
    if (val === 'Manual') return 'Số sàn';
    return val || '—';
  };

  // Chuyển đổi loại nhiên liệu sang tiếng Việt
  const formatFuelType = (val) => {
    switch (val) {
      case 'Gasoline':
        return 'Xăng';
      case 'Diesel':
        return 'Dầu';
      case 'Electric':
        return 'Điện';
      case 'Hybrid':
        return 'Hybrid';
      default:
        return val || '—';
    }
  };

  // Hiển thị huy hiệu trạng thái của xe
  const renderStatusBadge = (status) => {
    switch (status) {
      case 'Available':
        return <span className="badge badge-confirmed">Sẵn sàng</span>;
      case 'Rented':
        return <span className="badge badge-pending">Đang thuê</span>;
      case 'Maintenance':
        return <span className="badge badge-cancelled">Bảo dưỡng</span>;
      default:
        return <span className="badge">{status || '—'}</span>;
    }
  };

  // Xử lý khi nhấn nút Đặt xe hoặc Đăng nhập
  const handleBookingClick = () => {
    if (!isAuthenticated) {
      // Chuyển đến trang đăng nhập kèm trạng thái để quay lại sau
      navigate('/login', { state: { from: location } });
    } else {
      // Đã đăng nhập -> Điều hướng đến trang đặt xe
      navigate(`/booking/${id}`);
    }
  };

  if (loading) {
    return (
      <div className="page-container" style={{ textAlign: 'center', padding: '80px 0' }}>
        <div className="loading-spinner" />
        <p className="text-muted" style={{ marginTop: '16px' }}>Đang tải thông tin xe...</p>
      </div>
    );
  }

  if (error || !car) {
    return (
      <div className="page-container" style={{ textAlign: 'center', padding: '60px 16px' }}>
        <div style={{ fontSize: '3rem', marginBottom: '16px' }}>⚠️</div>
        <h2 style={{ fontSize: '1.4rem', fontWeight: '600', marginBottom: '12px', color: '#1f2937' }}>
          {error || 'Không tìm thấy thông tin xe'}
        </h2>
        <p className="text-muted" style={{ marginBottom: '24px' }}>
          Chiếc xe này có thể đã bị xóa hoặc đường dẫn không chính xác.
        </p>
        <Link to="/cars" className="btn btn-primary">
          Quay lại danh sách xe
        </Link>
      </div>
    );
  }

  const isAvailable = car.status === 'Available';

  return (
    <div className="page-container">
      {/* Breadcrumb điều hướng */}
      <div style={{ marginBottom: '20px', fontSize: '0.9rem', color: '#6b7280' }}>
        <Link to="/cars" style={{ color: 'var(--primary-color, #2563eb)', textDecoration: 'none' }}>
          Danh mục xe
        </Link>{' '}
        / <span>{car.brand} {car.model}</span>
      </div>

      <div
        style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(auto-fit, minmax(320px, 1fr))',
          gap: '32px',
          alignItems: 'start',
        }}
      >
        {/* Cột trái: Bộ sưu tập hình ảnh xe */}
        <div>
          <ImageGallery images={car.images || []} />
        </div>

        {/* Cột phải: Thông số chi tiết & Thao tác đặt xe */}
        <div
          style={{
            backgroundColor: '#ffffff',
            borderRadius: '8px',
            border: '1px solid #e5e7eb',
            padding: '28px',
            boxShadow: 'var(--shadow-sm)',
          }}
        >
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '12px' }}>
            <div>
              <h1 style={{ fontSize: '1.75rem', fontWeight: '700', color: '#111827', margin: 0 }}>
                {car.brand} {car.model}
              </h1>
              <p className="text-muted" style={{ marginTop: '4px', fontSize: '0.9rem' }}>
                Biển số: <strong style={{ color: '#374151' }}>{car.licensePlate || 'Chưa cập nhật'}</strong>
              </p>
            </div>
            <div>{renderStatusBadge(car.status)}</div>
          </div>

          {/* Mức giá theo ngày */}
          <div
            style={{
              padding: '16px',
              backgroundColor: '#f8fafc',
              borderRadius: '8px',
              border: '1px solid #e2e8f0',
              margin: '16px 0 24px',
              display: 'flex',
              alignItems: 'baseline',
              justifyContent: 'space-between',
            }}
          >
            <span style={{ fontSize: '0.95rem', color: '#64748b', fontWeight: '500' }}>Giá thuê niêm yết:</span>
            <div>
              <span style={{ fontSize: '1.6rem', fontWeight: '700', color: 'var(--primary-color, #2563eb)' }}>
                {formatCurrency(car.pricePerDay)}
              </span>
              <span style={{ fontSize: '0.85rem', color: '#64748b' }}> / ngày</span>
            </div>
          </div>

          {/* Bảng thông số kỹ thuật xe */}
          <h2 style={{ fontSize: '1.1rem', fontWeight: '600', marginBottom: '12px', color: '#1f2937' }}>
            Thông số kỹ thuật
          </h2>
          <table
            style={{
              width: '100%',
              borderCollapse: 'collapse',
              marginBottom: '24px',
              fontSize: '0.9rem',
            }}
          >
            <tbody>
              <tr style={{ borderBottom: '1px solid #f3f4f6' }}>
                <td style={{ padding: '10px 0', color: '#6b7280', width: '45%' }}>Hãng xe</td>
                <td style={{ padding: '10px 0', fontWeight: '600', color: '#111827' }}>{car.brand || '—'}</td>
              </tr>
              <tr style={{ borderBottom: '1px solid #f3f4f6' }}>
                <td style={{ padding: '10px 0', color: '#6b7280' }}>Model</td>
                <td style={{ padding: '10px 0', fontWeight: '600', color: '#111827' }}>{car.model || '—'}</td>
              </tr>
              <tr style={{ borderBottom: '1px solid #f3f4f6' }}>
                <td style={{ padding: '10px 0', color: '#6b7280' }}>Loại xe</td>
                <td style={{ padding: '10px 0', fontWeight: '600', color: '#111827' }}>{car.type || '—'}</td>
              </tr>
              <tr style={{ borderBottom: '1px solid #f3f4f6' }}>
                <td style={{ padding: '10px 0', color: '#6b7280' }}>Biển số</td>
                <td style={{ padding: '10px 0', fontWeight: '600', color: '#111827' }}>{car.licensePlate || '—'}</td>
              </tr>
              <tr style={{ borderBottom: '1px solid #f3f4f6' }}>
                <td style={{ padding: '10px 0', color: '#6b7280' }}>Số ghế</td>
                <td style={{ padding: '10px 0', fontWeight: '600', color: '#111827' }}>{car.seats ? `${car.seats} chỗ` : '—'}</td>
              </tr>
              <tr style={{ borderBottom: '1px solid #f3f4f6' }}>
                <td style={{ padding: '10px 0', color: '#6b7280' }}>Hộp số</td>
                <td style={{ padding: '10px 0', fontWeight: '600', color: '#111827' }}>{formatTransmission(car.transmission)}</td>
              </tr>
              <tr style={{ borderBottom: '1px solid #f3f4f6' }}>
                <td style={{ padding: '10px 0', color: '#6b7280' }}>Nhiên liệu</td>
                <td style={{ padding: '10px 0', fontWeight: '600', color: '#111827' }}>{formatFuelType(car.fuelType)}</td>
              </tr>
              <tr style={{ borderBottom: '1px solid #f3f4f6' }}>
                <td style={{ padding: '10px 0', color: '#6b7280' }}>Năm sản xuất</td>
                <td style={{ padding: '10px 0', fontWeight: '600', color: '#111827' }}>{car.yearOfManufacture || '—'}</td>
              </tr>
              <tr>
                <td style={{ padding: '10px 0', color: '#6b7280' }}>Giá thuê / ngày</td>
                <td style={{ padding: '10px 0', fontWeight: '600', color: 'var(--primary-color, #2563eb)' }}>
                  {formatCurrency(car.pricePerDay)}
                </td>
              </tr>
            </tbody>
          </table>

          {/* Mô tả chi tiết xe */}
          {car.description && (
            <div style={{ marginBottom: '24px' }}>
              <h2 style={{ fontSize: '1.1rem', fontWeight: '600', marginBottom: '8px', color: '#1f2937' }}>
                Mô tả chi tiết
              </h2>
              <p style={{ color: '#4b5563', lineHeight: '1.6', fontSize: '0.95rem', whiteSpace: 'pre-line' }}>
                {car.description}
              </p>
            </div>
          )}

          {/* Nút hành động Đặt xe / Đăng nhập */}
          <div style={{ marginTop: '28px' }}>
            {!isAuthenticated ? (
              <button
                type="button"
                className="btn btn-primary w-full"
                style={{ padding: '14px', fontSize: '1.05rem' }}
                onClick={handleBookingClick}
              >
                Đăng nhập để đặt xe
              </button>
            ) : (
              <div>
                <button
                  type="button"
                  className="btn btn-primary w-full"
                  style={{ padding: '14px', fontSize: '1.05rem' }}
                  disabled={!isAvailable}
                  onClick={handleBookingClick}
                >
                  {isAvailable ? 'Đặt xe ngay' : 'Xe hiện không khả dụng'}
                </button>
                {!isAvailable && (
                  <p className="text-center text-muted" style={{ fontSize: '0.85rem', marginTop: '8px' }}>
                    Xe đang trong trạng thái <strong>{car.status}</strong>, vui lòng chọn xe khác.
                  </p>
                )}
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
};

export default CarDetailPage;
