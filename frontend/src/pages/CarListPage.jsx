import React, { useState, useEffect, useCallback } from 'react';
import axiosClient from '../api/axiosClient';
import CarCard from '../components/CarCard';
import Pagination from '../components/Pagination';

/**
 * Trang Danh sách xe (CarListPage)
 * Hiển thị danh mục xe cho thuê, bộ lọc tìm kiếm theo loại xe, khoảng giá, trạng thái
 * và phân trang danh sách.
 */
const CarListPage = () => {
  const [cars, setCars] = useState([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const pageSize = 12;

  // Bộ lọc tìm kiếm
  const [filters, setFilters] = useState({
    type: '',
    minPrice: '',
    maxPrice: '',
    status: '',
  });

  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  /**
   * Gọi API lấy danh sách xe với các tham số tìm kiếm và phân trang
   * Endpoint: GET /cars?type=&minPrice=&maxPrice=&status=&page=&pageSize=12
   */
  const fetchCars = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);

      // Chuẩn bị query params
      const params = {
        page,
        pageSize,
      };

      if (filters.type) params.type = filters.type;
      if (filters.minPrice) params.minPrice = Number(filters.minPrice);
      if (filters.maxPrice) params.maxPrice = Number(filters.maxPrice);
      if (filters.status) params.status = filters.status;

      const response = await axiosClient.get('/cars', { params });

      // Backend trả về dạng { items, totalCount, page, pageSize } hoặc mảng trực tiếp
      const data = response.data;
      if (Array.isArray(data)) {
        setCars(data);
        setTotalCount(data.length);
      } else if (data && Array.isArray(data.items)) {
        setCars(data.items);
        setTotalCount(data.totalCount ?? data.items.length);
      } else {
        setCars([]);
        setTotalCount(0);
      }
    } catch (err) {
      console.error('Lỗi khi tải danh sách xe:', err);
      setError('Không thể tải danh sách xe. Vui lòng thử lại sau.');
    } finally {
      setLoading(false);
    }
  }, [page, filters]);

  // Tự động gọi API khi component mount hoặc khi page / filters thay đổi
  useEffect(() => {
    fetchCars();
  }, [fetchCars]);

  // Xử lý thay đổi từng trường trong bộ lọc (tự động reset về trang 1)
  const handleFilterChange = (field, value) => {
    setFilters((prev) => ({
      ...prev,
      [field]: value,
    }));
    setPage(1);
  };

  // Đặt lại tất cả các bộ lọc về mặc định
  const handleResetFilters = () => {
    setFilters({
      type: '',
      minPrice: '',
      maxPrice: '',
      status: '',
    });
    setPage(1);
  };

  return (
    <div className="page-container">
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '16px' }}>
        <div>
          <h1 className="page-title" style={{ marginBottom: '4px' }}>Danh mục xe cho thuê</h1>
          <p className="text-muted" style={{ fontSize: '0.95rem' }}>
            Tìm kiếm chiếc xe lý tưởng với mức giá tối ưu và định giá thông minh AI
          </p>
        </div>
        {totalCount > 0 && (
          <span className="text-muted" style={{ fontSize: '0.9rem', fontWeight: '500' }}>
            Tổng cộng: <strong style={{ color: '#111827' }}>{totalCount}</strong> xe
          </span>
        )}
      </div>

      {/* Thanh lọc (Filter Bar) */}
      <div className="filter-bar">
        {/* Phân loại xe: All, Sedan, SUV, Truck, Van, Hatchback */}
        <div className="filter-group">
          <label htmlFor="filter-type">Loại xe</label>
          <select
            id="filter-type"
            value={filters.type}
            onChange={(e) => handleFilterChange('type', e.target.value)}
          >
            <option value="">Tất cả các loại</option>
            <option value="Sedan">Sedan</option>
            <option value="SUV">SUV</option>
            <option value="Truck">Truck (Bán tải)</option>
            <option value="Van">Van (Đa dụng)</option>
            <option value="Hatchback">Hatchback</option>
          </select>
        </div>

        {/* Giá tối thiểu */}
        <div className="filter-group">
          <label htmlFor="filter-min-price">Giá tối thiểu (VNĐ)</label>
          <input
            id="filter-min-price"
            type="number"
            min="0"
            step="50000"
            placeholder="vd: 500.000"
            value={filters.minPrice}
            onChange={(e) => handleFilterChange('minPrice', e.target.value)}
          />
        </div>

        {/* Giá tối đa */}
        <div className="filter-group">
          <label htmlFor="filter-max-price">Giá tối đa (VNĐ)</label>
          <input
            id="filter-max-price"
            type="number"
            min="0"
            step="50000"
            placeholder="vd: 3.000.000"
            value={filters.maxPrice}
            onChange={(e) => handleFilterChange('maxPrice', e.target.value)}
          />
        </div>

        {/* Trạng thái xe: All, Available, Rented, Maintenance */}
        <div className="filter-group">
          <label htmlFor="filter-status">Trạng thái</label>
          <select
            id="filter-status"
            value={filters.status}
            onChange={(e) => handleFilterChange('status', e.target.value)}
          >
            <option value="">Tất cả trạng thái</option>
            <option value="Available">Sẵn sàng (Available)</option>
            <option value="Rented">Đang thuê (Rented)</option>
            <option value="Maintenance">Bảo dưỡng (Maintenance)</option>
          </select>
        </div>

        {/* Nút đặt lại bộ lọc */}
        {(filters.type || filters.minPrice || filters.maxPrice || filters.status) && (
          <button
            type="button"
            className="btn btn-secondary btn-sm"
            onClick={handleResetFilters}
            style={{ alignSelf: 'flex-end', height: '38px', whiteSpace: 'nowrap' }}
          >
            Đặt lại lọc
          </button>
        )}
      </div>

      {/* Hiển thị lỗi nếu có */}
      {error && (
        <div
          style={{
            padding: '16px',
            backgroundColor: '#fee2e2',
            border: '1px solid #fca5a5',
            borderRadius: '8px',
            color: '#b91c1c',
            marginBottom: '24px',
            textAlign: 'center',
          }}
        >
          {error}
        </div>
      )}

      {/* Hiệu ứng tải trang */}
      {loading ? (
        <div style={{ padding: '60px 0', textAlign: 'center' }}>
          <div className="loading-spinner" />
          <p className="text-muted" style={{ marginTop: '12px' }}>Đang tải danh sách xe...</p>
        </div>
      ) : cars.length > 0 ? (
        <>
          {/* Lưới hiển thị các thẻ xe */}
          <div className="car-grid">
            {cars.map((car) => (
              <CarCard key={car.id} car={car} />
            ))}
          </div>

          {/* Thành phần phân trang */}
          <Pagination
            currentPage={page}
            totalCount={totalCount}
            pageSize={pageSize}
            onPageChange={(newPage) => {
              setPage(newPage);
              window.scrollTo({ top: 0, behavior: 'smooth' });
            }}
          />
        </>
      ) : (
        /* Trường hợp không tìm thấy dữ liệu */
        <div
          style={{
            textAlign: 'center',
            padding: '64px 16px',
            backgroundColor: '#ffffff',
            borderRadius: '8px',
            border: '1px solid #e5e7eb',
            margin: '24px 0',
          }}
        >
          <div style={{ fontSize: '3.5rem', marginBottom: '16px' }}>🔍</div>
          <h2 style={{ fontSize: '1.25rem', fontWeight: '600', color: '#374151', marginBottom: '8px' }}>
            Không tìm thấy xe nào phù hợp
          </h2>
          <p className="text-muted" style={{ marginBottom: '20px' }}>
            Vui lòng thử điều chỉnh lại bộ lọc hoặc khoảng giá tìm kiếm của bạn.
          </p>
          <button
            type="button"
            className="btn btn-primary btn-sm"
            onClick={handleResetFilters}
          >
            Xem tất cả xe
          </button>
        </div>
      )}
    </div>
  );
};

export default CarListPage;
