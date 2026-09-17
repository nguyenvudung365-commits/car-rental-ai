import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { formatCurrency } from '../utils/formatCurrency';

/**
 * Thẻ hiển thị thông tin tóm tắt xe (CarCard)
 * Nhận vào prop `car` (hoặc các thuộc tính trải phẳng)
 *
 * @param {Object} props
 * @param {Object} [props.car] Đối tượng xe { id, brand, model, type, licensePlate, pricePerDay, status, thumbnailUrl, images }
 */
const CarCard = ({ car: carProp, ...rest }) => {
  const navigate = useNavigate();
  const [imgError, setImgError] = useState(false);

  // Hỗ trợ cả 2 cách truyền props: <CarCard car={item} /> hoặc <CarCard {...item} />
  const car = carProp || rest;
  const { id, brand, model, type, licensePlate, pricePerDay, status } = car;

  // Lấy ảnh đại diện: thumbnailUrl hoặc ảnh isThumbnail trong mảng images, hoặc ảnh đầu tiên
  const thumbnailUrl = car.thumbnailUrl ||
    (Array.isArray(car.images) && car.images.find((img) => img.isThumbnail)?.url) ||
    (Array.isArray(car.images) && car.images[0]?.url) ||
    '';

  // Điều hướng đến trang chi tiết xe
  const handleClick = () => {
    if (id) {
      navigate(`/cars/${id}`);
    }
  };

  const handleKeyDown = (e) => {
    if (e.key === 'Enter' || e.key === ' ') {
      e.preventDefault();
      handleClick();
    }
  };

  // Định dạng hiển thị trạng thái xe
  const renderStatusBadge = () => {
    switch (status) {
      case 'Available':
        return <span className="status-badge status-available">Sẵn sàng</span>;
      case 'Rented':
        return <span className="status-badge status-rented">Đang thuê</span>;
      case 'Maintenance':
        return <span className="status-badge status-maintenance">Bảo dưỡng</span>;
      default:
        return <span className="status-badge status-default">{status || 'Chưa rõ'}</span>;
    }
  };

  // Định dạng an toàn tiền tệ VND
  const formattedPrice = (() => {
    try {
      if (typeof formatCurrency === 'function') {
        return formatCurrency(pricePerDay);
      }
    } catch {
      // Bỏ qua lỗi và dùng fallback
    }
    return `${Number(pricePerDay || 0).toLocaleString('vi-VN')} ₫`;
  })();

  return (
    <div
      className="car-card"
      onClick={handleClick}
      onKeyDown={handleKeyDown}
      tabIndex={0}
      role="button"
      aria-label={`Xe ${brand} ${model} - Biển số ${licensePlate}`}
    >
      {/* Khung ảnh xe hoặc placeholder nếu chưa có ảnh */}
      <div className="car-card-image-wrapper">
        {thumbnailUrl && !imgError ? (
          <img
            src={thumbnailUrl}
            alt={`${brand} ${model}`}
            className="car-card-image"
            onError={() => setImgError(true)}
            loading="lazy"
          />
        ) : (
          <div className="car-card-placeholder" aria-label="Không có hình ảnh">
            <span className="placeholder-icon">🚗</span>
          </div>
        )}
      </div>

      {/* Nội dung thông tin xe */}
      <div className="car-card-body">
        <div className="car-card-header">
          <h3 className="car-card-title">
            {brand} {model}
          </h3>
          {type && <span className="badge car-type-badge">{type}</span>}
        </div>

        <div className="car-card-meta">
          <span className="car-license-plate">Biển số: {licensePlate || 'N/A'}</span>
        </div>

        <div className="car-card-footer">
          <div className="car-price">
            <span className="price-value">{formattedPrice}</span>
            <span className="price-unit">/ngày</span>
          </div>
          <div className="car-status">
            {renderStatusBadge()}
          </div>
        </div>
      </div>
    </div>
  );
};

export default CarCard;
