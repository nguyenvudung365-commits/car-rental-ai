import React, { useState, useEffect, useCallback, useRef } from 'react';
import axiosClient from '../../api/axiosClient';

/**
 * Trang Quản Lý Hình Ảnh Xe (AdminCarImagesPage)
 * Chức năng:
 * - Chọn xe từ danh sách hệ thống (GET /cars?pageSize=100)
 * - Tải chi tiết và danh sách hình ảnh xe (GET /cars/:id)
 * - Hiển thị lưới hình ảnh với huy hiệu ảnh đại diện (isThumbnail)
 * - Tải lên hình ảnh mới cho xe (POST /cars/:id/images) định dạng multipart/form-data
 * - Xóa hình ảnh của xe (DELETE /cars/:carId/images/:imageId)
 */
const AdminCarImagesPage = () => {
  // Danh sách xe để hiển thị trong dropdown
  const [cars, setCars] = useState([]);
  const [loadingCars, setLoadingCars] = useState(true);

  // Xe đang được chọn
  const [selectedCarId, setSelectedCarId] = useState('');
  const [selectedCar, setSelectedCar] = useState(null);
  const [images, setImages] = useState([]);
  const [loadingImages, setLoadingImages] = useState(false);

  // Trạng thái tải file mới
  const [selectedFile, setSelectedFile] = useState(null);
  const [previewUrl, setPreviewUrl] = useState(null);
  const [uploading, setUploading] = useState(false);
  const fileInputRef = useRef(null);

  // Trạng thái xóa ảnh
  const [deletingId, setDeletingId] = useState(null);

  // Thông báo phản hồi
  const [notification, setNotification] = useState(null);
  const [error, setError] = useState(null);

  // Tự động tắt thông báo sau 4 giây
  useEffect(() => {
    if (notification) {
      const timer = setTimeout(() => setNotification(null), 4000);
      return () => clearTimeout(timer);
    }
  }, [notification]);

  /**
   * Tải danh sách tất cả các xe đang có trong hệ thống
   * API Endpoint: GET /cars?pageSize=100
   */
  const fetchCars = useCallback(async () => {
    try {
      setLoadingCars(true);
      setError(null);
      const response = await axiosClient.get('/cars', {
        params: { pageSize: 100 },
      });

      const carList = response.data?.items || response.data?.data || (Array.isArray(response.data) ? response.data : []);
      setCars(carList);
    } catch (err) {
      console.error('Lỗi khi tải danh sách xe:', err);
      setError('Không thể tải danh sách xe. Vui lòng thử lại!');
    } finally {
      setLoadingCars(false);
    }
  }, []);

  useEffect(() => {
    fetchCars();
  }, [fetchCars]);

  /**
   * Tải chi tiết xe và danh sách hình ảnh khi xe được chọn
   * API Endpoint: GET /cars/:id
   */
  const fetchCarImages = useCallback(async (carId) => {
    if (!carId) {
      setSelectedCar(null);
      setImages([]);
      return;
    }

    try {
      setLoadingImages(true);
      setError(null);
      const response = await axiosClient.get(`/cars/${carId}`);
      const carData = response.data;
      setSelectedCar(carData);
      setImages(carData.images || []);
    } catch (err) {
      console.error(`Lỗi khi tải hình ảnh xe #${carId}:`, err);
      setError('Không thể tải hình ảnh của xe đã chọn.');
    } finally {
      setLoadingImages(false);
    }
  }, []);

  // Gọi fetchCarImages khi selectedCarId thay đổi
  useEffect(() => {
    if (selectedCarId) {
      fetchCarImages(selectedCarId);
      // Xóa preview và file đã chọn trước đó
      handleClearFile();
    } else {
      setSelectedCar(null);
      setImages([]);
      handleClearFile();
    }
  }, [selectedCarId, fetchCarImages]);

  // Xử lý khi chọn file từ máy tính
  const handleFileChange = (e) => {
    const file = e.target.files?.[0];
    if (file) {
      // Kiểm tra định dạng ảnh
      if (!file.type.startsWith('image/')) {
        setError('Vui lòng chỉ chọn tập tin định dạng hình ảnh (PNG, JPG, JPEG, WEBP...).');
        return;
      }
      setSelectedFile(file);
      const objectUrl = URL.createObjectURL(file);
      setPreviewUrl(objectUrl);
      setError(null);
    }
  };

  // Hủy file đã chọn
  const handleClearFile = () => {
    setSelectedFile(null);
    if (previewUrl) {
      URL.revokeObjectURL(previewUrl);
      setPreviewUrl(null);
    }
    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
  };

  /**
   * Tải ảnh mới lên server
   * API Endpoint: POST /cars/:id/images
   * Body: FormData multipart
   */
  const handleUploadImage = async (e) => {
    e.preventDefault();
    if (!selectedCarId) {
      setError('Vui lòng chọn một xe trước khi tải ảnh lên.');
      return;
    }

    if (!selectedFile) {
      setError('Vui lòng chọn một tệp hình ảnh để tải lên.');
      return;
    }

    try {
      setUploading(true);
      setError(null);

      const formData = new FormData();
      // Gắn file với cả 2 trường thông dụng 'file' và 'image' để tương thích API backend
      formData.append('image', selectedFile);
      formData.append('file', selectedFile);

      await axiosClient.post(`/cars/${selectedCarId}/images`, formData, {
        headers: {
          'Content-Type': 'multipart/form-data',
        },
      });

      setNotification({
        type: 'success',
        message: 'Tải hình ảnh lên thành công!',
      });

      // Dọn dẹp form tải ảnh
      handleClearFile();

      // Tải lại danh sách ảnh của xe
      await fetchCarImages(selectedCarId);
    } catch (err) {
      console.error('Lỗi khi tải ảnh lên:', err);
      setError(err.response?.data?.message || 'Có lỗi xảy ra khi tải ảnh lên server.');
    } finally {
      setUploading(false);
    }
  };

  /**
   * Xóa một hình ảnh của xe
   * API Endpoint: DELETE /cars/:carId/images/:imageId
   */
  const handleDeleteImage = async (imageId) => {
    if (!window.confirm('Bạn có chắc chắn muốn xóa hình ảnh này khỏi hệ thống?')) {
      return;
    }

    try {
      setDeletingId(imageId);
      setError(null);
      await axiosClient.delete(`/cars/${selectedCarId}/images/${imageId}`);

      setNotification({
        type: 'success',
        message: 'Đã xóa hình ảnh thành công!',
      });

      // Tải lại danh sách ảnh của xe
      await fetchCarImages(selectedCarId);
    } catch (err) {
      console.error('Lỗi khi xóa ảnh:', err);
      setError(err.response?.data?.message || 'Không thể xóa hình ảnh. Vui lòng thử lại!');
    } finally {
      setDeletingId(null);
    }
  };

  return (
    <div style={{ maxWidth: '1200px', margin: '0 auto', padding: '24px 16px' }}>
      {/* Tiêu đề trang */}
      <div style={{ marginBottom: '24px' }}>
        <h1 style={{ fontSize: '1.75rem', fontWeight: '700', color: '#111827', margin: 0 }}>
          Quản Lý Hình Ảnh Xe
        </h1>
        <p style={{ color: '#6b7280', marginTop: '6px', fontSize: '0.95rem' }}>
          Xem bộ sưu tập ảnh, tải thêm ảnh xe mới và xóa bỏ các ảnh không còn sử dụng.
        </p>
      </div>

      {/* Thông báo Alert */}
      {notification && (
        <div
          style={{
            marginBottom: '20px',
            padding: '12px 16px',
            borderRadius: '8px',
            fontSize: '0.9rem',
            backgroundColor: '#ecfdf5',
            color: '#065f46',
            border: '1px solid #a7f3d0',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
          }}
        >
          <span>{notification.message}</span>
          <button
            onClick={() => setNotification(null)}
            style={{ fontWeight: 'bold', fontSize: '1.1rem', lineHeight: 1 }}
          >
            ×
          </button>
        </div>
      )}

      {error && (
        <div
          style={{
            marginBottom: '20px',
            padding: '12px 16px',
            borderRadius: '8px',
            fontSize: '0.9rem',
            backgroundColor: '#fef2f2',
            color: '#991b1b',
            border: '1px solid #fee2e2',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
          }}
        >
          <span>{error}</span>
          <button
            onClick={() => setError(null)}
            style={{ fontWeight: 'bold', fontSize: '1.1rem', lineHeight: 1 }}
          >
            ×
          </button>
        </div>
      )}

      {/* Chọn xe từ danh sách */}
      <div
        style={{
          backgroundColor: '#ffffff',
          borderRadius: '10px',
          padding: '20px',
          marginBottom: '24px',
          boxShadow: '0 1px 3px rgba(0, 0, 0, 0.08)',
        }}
      >
        <label
          htmlFor="carSelectDropdown"
          style={{
            display: 'block',
            marginBottom: '8px',
            fontSize: '0.95rem',
            fontWeight: '600',
            color: '#374151',
          }}
        >
          Chọn phương tiện cần quản lý hình ảnh:
        </label>
        <select
          id="carSelectDropdown"
          value={selectedCarId}
          onChange={(e) => setSelectedCarId(e.target.value)}
          disabled={loadingCars}
          style={{
            width: '100%',
            maxWidth: '600px',
            padding: '10px 14px',
            borderRadius: '6px',
            border: '1px solid #d1d5db',
            fontSize: '0.95rem',
            backgroundColor: '#ffffff',
            outline: 'none',
            cursor: 'pointer',
          }}
        >
          <option value="">-- Vui lòng chọn xe từ danh sách ({cars.length} xe) --</option>
          {cars.map((car) => (
            <option key={car.carId} value={car.id}>
              {car.brand} {car.model} — Biển số: {car.licensePlate || 'Chưa có'} (Năm {car.yearOfManufacture || '-'})
            </option>
          ))}
        </select>
      </div>

      {selectedCarId ? (
        <div>
          {/* Khối tải ảnh mới */}
          <div
            style={{
              backgroundColor: '#ffffff',
              borderRadius: '10px',
              padding: '24px',
              marginBottom: '24px',
              boxShadow: '0 1px 3px rgba(0, 0, 0, 0.08)',
            }}
          >
            <h2 style={{ fontSize: '1.15rem', fontWeight: '600', color: '#1f2937', marginTop: 0, marginBottom: '14px' }}>
              Tải Lên Hình Ảnh Mới Cho {selectedCar?.brand} {selectedCar?.model} ({selectedCar?.licensePlate})
            </h2>

            <form onSubmit={handleUploadImage}>
              <div style={{ display: 'flex', flexWrap: 'wrap', gap: '16px', alignItems: 'center' }}>
                <input
                  ref={fileInputRef}
                  type="file"
                  accept="image/*"
                  onChange={handleFileChange}
                  disabled={uploading}
                  style={{
                    padding: '8px 0',
                    fontSize: '0.9rem',
                    color: '#4b5563',
                  }}
                />

                <div style={{ display: 'flex', gap: '8px' }}>
                  <button
                    type="submit"
                    disabled={!selectedFile || uploading}
                    style={{
                      padding: '9px 20px',
                      backgroundColor: !selectedFile || uploading ? '#9ca3af' : '#2563eb',
                      color: '#ffffff',
                      borderRadius: '6px',
                      fontWeight: '600',
                      fontSize: '0.875rem',
                      cursor: !selectedFile || uploading ? 'not-allowed' : 'pointer',
                      display: 'inline-flex',
                      alignItems: 'center',
                      gap: '6px',
                    }}
                  >
                    {uploading ? 'Đang tải lên...' : 'Tải lên ngay'}
                  </button>

                  {selectedFile && (
                    <button
                      type="button"
                      onClick={handleClearFile}
                      disabled={uploading}
                      style={{
                        padding: '9px 14px',
                        backgroundColor: '#f3f4f6',
                        color: '#4b5563',
                        borderRadius: '6px',
                        fontSize: '0.875rem',
                        fontWeight: '500',
                        cursor: 'pointer',
                      }}
                    >
                      Hủy chọn
                    </button>
                  )}
                </div>
              </div>

              {/* Khung xem trước ảnh chuẩn bị tải lên */}
              {previewUrl && (
                <div style={{ marginTop: '16px', display: 'flex', alignItems: 'center', gap: '16px' }}>
                  <div
                    style={{
                      width: '120px',
                      height: '80px',
                      borderRadius: '8px',
                      overflow: 'hidden',
                      border: '2px dashed #3b82f6',
                      backgroundColor: '#f8fafc',
                    }}
                  >
                    <img
                      src={previewUrl}
                      alt="Xem trước ảnh tải lên"
                      style={{ width: '100%', height: '100%', objectFit: 'cover' }}
                    />
                  </div>
                  <div style={{ fontSize: '0.85rem', color: '#4b5563' }}>
                    <p style={{ margin: '0 0 2px 0', fontWeight: '600' }}>Tên tệp: {selectedFile.name}</p>
                    <p style={{ margin: 0 }}>Dung lượng: {(selectedFile.size / 1024).toFixed(1)} KB</p>
                  </div>
                </div>
              )}
            </form>
          </div>

          {/* Lưới hiển thị các hình ảnh hiện có */}
          <div
            style={{
              backgroundColor: '#ffffff',
              borderRadius: '10px',
              padding: '24px',
              boxShadow: '0 1px 3px rgba(0, 0, 0, 0.08)',
            }}
          >
            <div
              style={{
                display: 'flex',
                justifyContent: 'space-between',
                alignItems: 'center',
                marginBottom: '18px',
                borderBottom: '1px solid #f3f4f6',
                paddingBottom: '12px',
              }}
            >
              <h2 style={{ fontSize: '1.15rem', fontWeight: '600', color: '#1f2937', margin: 0 }}>
                Danh Sách Ảnh Hiện Tại ({images.length})
              </h2>
              <button
                type="button"
                onClick={() => fetchCarImages(selectedCarId)}
                disabled={loadingImages}
                style={{
                  fontSize: '0.85rem',
                  color: '#2563eb',
                  fontWeight: '500',
                  cursor: 'pointer',
                  textDecoration: 'underline',
                }}
              >
                {loadingImages ? 'Đang làm mới...' : 'Làm mới danh sách ảnh'}
              </button>
            </div>

            {loadingImages ? (
              <div style={{ padding: '40px 20px', textAlign: 'center', color: '#6b7280' }}>
                <p>Đang tải danh sách ảnh xe...</p>
              </div>
            ) : images.length === 0 ? (
              <div
                style={{
                  padding: '40px 20px',
                  textAlign: 'center',
                  color: '#6b7280',
                  backgroundColor: '#f9fafb',
                  borderRadius: '8px',
                  border: '1px dashed #d1d5db',
                }}
              >
                <p style={{ margin: 0, fontSize: '0.95rem' }}>
                  Chưa có hình ảnh nào cho xe này. Hãy chọn ảnh và bấm "Tải lên ngay" ở trên.
                </p>
              </div>
            ) : (
              <div
                style={{
                  display: 'grid',
                  gridTemplateColumns: 'repeat(auto-fill, minmax(240px, 1fr))',
                  gap: '20px',
                }}
              >
                {images.map((img) => (
                  <div
                    key={img.id}
                    style={{
                      borderRadius: '8px',
                      overflow: 'hidden',
                      border: '1px solid #e5e7eb',
                      backgroundColor: '#ffffff',
                      boxShadow: '0 1px 2px rgba(0, 0, 0, 0.05)',
                      display: 'flex',
                      flexDirection: 'column',
                      position: 'relative',
                    }}
                  >
                    {/* Ảnh xe */}
                    <div style={{ position: 'relative', height: '160px', backgroundColor: '#f3f4f6' }}>
                      <img
                        src={img.url}
                        alt={`Ảnh xe ${selectedCar?.brand} ${selectedCar?.model}`}
                        style={{
                          width: '100%',
                          height: '100%',
                          objectFit: 'cover',
                        }}
                        onError={(e) => {
                          e.target.onerror = null;
                          e.target.src = 'https://placehold.co/400x300?text=No+Image';
                        }}
                      />

                      {/* Huy hiệu ảnh đại diện nếu có */}
                      {img.isThumbnail && (
                        <span
                          style={{
                            position: 'absolute',
                            top: '8px',
                            left: '8px',
                            backgroundColor: '#2563eb',
                            color: '#ffffff',
                            padding: '3px 8px',
                            borderRadius: '4px',
                            fontSize: '0.75rem',
                            fontWeight: '600',
                            boxShadow: '0 1px 2px rgba(0,0,0,0.2)',
                          }}
                        >
                          Ảnh đại diện
                        </span>
                      )}
                    </div>

                    {/* Khối thông tin và nút xóa */}
                    <div
                      style={{
                        padding: '12px',
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'space-between',
                        backgroundColor: '#ffffff',
                        borderTop: '1px solid #f3f4f6',
                      }}
                    >
                      <span style={{ fontSize: '0.8rem', color: '#6b7280' }}>
                        ID: #{img.id}
                      </span>

                      <button
                        type="button"
                        onClick={() => handleDeleteImage(img.id)}
                        disabled={deletingId === img.id}
                        style={{
                          padding: '6px 12px',
                          backgroundColor: '#fee2e2',
                          color: '#dc2626',
                          borderRadius: '6px',
                          fontSize: '0.8rem',
                          fontWeight: '600',
                          cursor: deletingId === img.id ? 'not-allowed' : 'pointer',
                          transition: 'background-color 0.15s',
                        }}
                      >
                        {deletingId === img.id ? 'Đang xóa...' : 'Xóa ảnh'}
                      </button>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>
      ) : (
        <div
          style={{
            backgroundColor: '#ffffff',
            borderRadius: '10px',
            padding: '60px 20px',
            textAlign: 'center',
            color: '#6b7280',
            boxShadow: '0 1px 3px rgba(0, 0, 0, 0.08)',
          }}
        >
          <div style={{ fontSize: '2.5rem', marginBottom: '12px' }}>🚗</div>
          <h3 style={{ fontSize: '1.15rem', color: '#374151', margin: '0 0 6px 0' }}>
            Vui lòng chọn một phương tiện
          </h3>
          <p style={{ margin: 0, fontSize: '0.9rem' }}>
            Chọn xe từ ô lựa chọn phía trên để tải lên ảnh mới hoặc quản lý thư viện ảnh của xe.
          </p>
        </div>
      )}
    </div>
  );
};

export default AdminCarImagesPage;
