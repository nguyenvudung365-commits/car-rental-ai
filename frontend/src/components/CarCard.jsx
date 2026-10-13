import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { formatCurrency } from '../utils/formatCurrency';
import { carStatusLabel, carTypeLabel, normalizeCarStatus } from '../utils/carLabels';

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

  // Lấy thuộc tính với fallback PascalCase → camelCase
  const carId = car.CarId ?? car.carId ?? car.Id ?? car.id;
  const brand = car.Brand || car.brand;
  const model = car.Model || car.model;
  const carType = car.CarType ?? car.carType;
  const licensePlate = car.LicensePlate || car.licensePlate;
  const basePricePerDay = car.BasePricePerDay ?? car.basePricePerDay;
  const status = normalizeCarStatus(car.Status ?? car.status);
  const primaryImageUrl = car.PrimaryImageUrl || car.primaryImageUrl;

  // Lấy ảnh đại diện: primaryImageUrl
  const imageUrl = primaryImageUrl || '';

  // Điều hướng đến trang chi tiết xe
  const handleClick = () => {
    const targetId = carId || car.id;
    if (targetId) {
      navigate(`/cars/${targetId}`);
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
    const label = carStatusLabel(status);
    switch (status) {
      case 'SanSang':
        return <span className="status-badge status-available">{label}</span>;
      case 'DangThue':
        return <span className="status-badge status-rented">{label}</span>;
      case 'BaoTri':
        return <span className="status-badge status-maintenance">{label}</span>;
      default:
        return <span className="status-badge status-default">{label}</span>;
    }
  };

  // Định dạng an toàn tiền tệ VND
  const formattedPrice = (() => {
    try {
      if (typeof formatCurrency === 'function') {
        return formatCurrency(basePricePerDay);
      }
    } catch {
      // Bỏ qua lỗi và dùng fallback
    }
    return `${Number(basePricePerDay || 0).toLocaleString('vi-VN')} ₫`;
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
        {imageUrl && !imgError ? (
          <img
            src={imageUrl}
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
          {carType && <span className="badge car-type-badge">{carTypeLabel(carType)}</span>}
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
