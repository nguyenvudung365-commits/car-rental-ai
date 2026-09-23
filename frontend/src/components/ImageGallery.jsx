import React, { useState, useEffect } from 'react';

/**
 * Bộ sưu tập hình ảnh xe (ImageGallery)
 * Hiển thị ảnh lớn chính và danh sách thumbnail bên dưới cho phép người dùng nhấp để đổi ảnh
 *
 * @param {Object} props
 * @param {Array<{ id: string|number, url: string, isThumbnail?: boolean }>} [props.images] - Danh sách hình ảnh
 */
const ImageGallery = ({ images = [] }) => {
  const imageList = Array.isArray(images) ? images : [];

  // Tìm index của ảnh thumbnail mặc định (nếu có)
  const getInitialIndex = () => {
    const thumbIndex = imageList.findIndex((img) => img?.isThumbnail);
    return thumbIndex >= 0 ? thumbIndex : 0;
  };

  const [selectedIndex, setSelectedIndex] = useState(getInitialIndex);
  const [mainImgError, setMainImgError] = useState(false);

  // Cập nhật selectedIndex khi danh sách ảnh thay đổi
  useEffect(() => {
    setSelectedIndex(getInitialIndex());
    setMainImgError(false);
  }, [images]);

  // Nếu không có hình ảnh nào -> hiển thị placeholder
  if (imageList.length === 0) {
    return (
      <div className="image-gallery-empty" aria-label="Không có hình ảnh">
        <div className="gallery-placeholder-box">
          <span className="placeholder-icon" style={{ fontSize: '4rem' }}>🚗</span>
          <p className="placeholder-text" style={{ marginTop: '8px', color: '#6b7280' }}>
            Chưa có hình ảnh cho xe này
          </p>
        </div>
      </div>
    );
  }

  // Đảm bảo selectedIndex hợp lệ
  const safeIndex = selectedIndex >= 0 && selectedIndex < imageList.length ? selectedIndex : 0;
  const currentImage = imageList[safeIndex];

  return (
    <div className="image-gallery">
      {/* Khung hiển thị ảnh lớn */}
      <div className="gallery-main-wrapper">
        {currentImage?.url && !mainImgError ? (
          <img
            src={currentImage.url}
            alt="Hình ảnh chi tiết xe"
            className="gallery-main-image"
            onError={() => setMainImgError(true)}
          />
        ) : (
          <div className="gallery-main-placeholder">
            <span className="placeholder-icon" style={{ fontSize: '4rem' }}>🚗</span>
            <p style={{ marginTop: '8px', color: '#6b7280' }}>Không thể tải hình ảnh</p>
          </div>
        )}
      </div>

      {/* Danh sách thumbnail bên dưới (chỉ hiển thị nếu có nhiều hơn 1 ảnh) */}
      {imageList.length > 1 && (
        <div className="gallery-thumbnails" role="tablist" aria-label="Chọn ảnh">
          {imageList.map((img, index) => {
            const isSelected = index === safeIndex;
            return (
              <button
                key={img.id || index}
                type="button"
                role="tab"
                aria-selected={isSelected}
                className={`gallery-thumb-btn ${isSelected ? 'active' : ''}`}
                onClick={() => {
                  setSelectedIndex(index);
                  setMainImgError(false);
                }}
              >
                <img
                  src={img.url}
                  alt={`Ảnh nhỏ ${index + 1}`}
                  className="gallery-thumb-image"
                  onError={(e) => {
                    e.target.style.display = 'none';
                  }}
                />
                {img.isThumbnail && (
                  <span className="thumb-primary-tag">Chính</span>
                )}
              </button>
            );
          })}
        </div>
      )}
    </div>
  );
};

export default ImageGallery;
