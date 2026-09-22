import React, { useState, useEffect, useCallback } from 'react';
import axiosClient from '../../api/axiosClient';
import { formatCurrency } from '../../utils/formatCurrency';

/**
 * Trang Báo Cáo & Thống Kê Quản Trị (AdminReportsPage)
 * Chức năng:
 * - Chọn khoảng thời gian báo cáo (từ ngày - đến ngày)
 * - Tab 1: Doanh thu (GET /reports/revenue?from=&to=)
 *   + Thống kê: Tổng doanh thu (totalRevenue), Tổng số đơn đặt (totalBookings)
 *   + Bảng chi tiết doanh thu theo từng tháng (byMonth)
 * - Tab 2: Tỷ lệ lấp đầy (GET /reports/occupancy?from=&to=)
 *   + Thống kê: Tổng số xe (totalCars), Tỷ lệ lấp đầy trung bình (avgOccupancyRate)
 *   + Bảng chi tiết tỷ lệ lấp đầy theo từng xe/biển số (byCar)
 */
const AdminReportsPage = () => {
  // Khoảng thời gian lọc báo cáo (mặc định từ ngày đầu năm hoặc 30 ngày trước đến hiện tại)
  const [fromDate, setFromDate] = useState(() => {
    const d = new Date();
    d.setDate(d.getDate() - 30);
    return d.toISOString().split('T')[0];
  });

  const [toDate, setToDate] = useState(() => {
    return new Date().toISOString().split('T')[0];
  });

  // Tab đang hoạt động: 'revenue' hoặc 'occupancy'
  const [activeTab, setActiveTab] = useState('revenue');

  // Dữ liệu báo cáo
  const [revenueData, setRevenueData] = useState(null);
  const [occupancyData, setOccupancyData] = useState(null);

  // Trạng thái tải & lỗi
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  /**
   * Gọi API lấy báo cáo doanh thu
   * API Endpoint: GET /reports/revenue?from=&to=
   */
  const fetchRevenueReport = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const response = await axiosClient.get('/reports/revenue', {
        params: {
          from: fromDate || undefined,
          to: toDate || undefined,
        },
      });
      setRevenueData(response.data);
    } catch (err) {
      console.error('Lỗi khi tải báo cáo doanh thu:', err);
      setError(err.response?.data?.message || 'Không thể tải dữ liệu báo cáo doanh thu.');
    } finally {
      setLoading(false);
    }
  }, [fromDate, toDate]);

  /**
   * Gọi API lấy báo cáo tỷ lệ lấp đầy xe
   * API Endpoint: GET /reports/occupancy?from=&to=
   */
  const fetchOccupancyReport = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const response = await axiosClient.get('/reports/occupancy', {
        params: {
          from: fromDate || undefined,
          to: toDate || undefined,
        },
      });
      setOccupancyData(response.data);
    } catch (err) {
      console.error('Lỗi khi tải báo cáo tỷ lệ lấp đầy:', err);
      setError(err.response?.data?.message || 'Không thể tải dữ liệu báo cáo tỷ lệ lấp đầy.');
    } finally {
      setLoading(false);
    }
  }, [fromDate, toDate]);

  // Tự động tải dữ liệu khi tab thay đổi
  useEffect(() => {
    if (activeTab === 'revenue') {
      fetchRevenueReport();
    } else {
      fetchOccupancyReport();
    }
  }, [activeTab, fetchRevenueReport, fetchOccupancyReport]);

  // Xử lý khi nhấn nút "Xem báo cáo"
  const handleFilterSubmit = (e) => {
    e.preventDefault();
    if (activeTab === 'revenue') {
      fetchRevenueReport();
    } else {
      fetchOccupancyReport();
    }
  };

  // Định dạng tiền tệ an toàn
  const renderPrice = (amount) => {
    if (amount === null || amount === undefined) return '0 ₫';
    try {
      return formatCurrency(amount);
    } catch {
      return Number(amount || 0).toLocaleString('vi-VN') + ' ₫';
    }
  };

  // Định dạng phần trăm tỷ lệ lấp đầy
  const renderPercentage = (rate) => {
    if (rate === null || rate === undefined || isNaN(rate)) return '0%';
    const num = Number(rate);
    // Nếu rate trong khoảng 0.0 -> 1.0 thì nhân với 100
    const val = num <= 1 && num > 0 ? num * 100 : num;
    return `${val.toFixed(1)}%`;
  };

  // Lấy giá trị số của phần trăm cho thanh progress bar
  const getPercentValue = (rate) => {
    if (!rate || isNaN(rate)) return 0;
    const num = Number(rate);
    const val = num <= 1 && num > 0 ? num * 100 : num;
    return Math.min(100, Math.max(0, val));
  };

  return (
    <div style={{ maxWidth: '1200px', margin: '0 auto', padding: '24px 16px' }}>
      {/* Tiêu đề trang */}
      <div style={{ marginBottom: '24px' }}>
        <h1 style={{ fontSize: '1.75rem', fontWeight: '700', color: '#111827', margin: 0 }}>
          Báo Cáo & Thống Kê Hoạt Động
        </h1>
        <p style={{ color: '#6b7280', marginTop: '6px', fontSize: '0.95rem' }}>
          Theo dõi chi tiết hiệu suất doanh thu và hiệu suất khai thác sử dụng đội xe theo thời gian.
        </p>
      </div>

      {/* Bộ lọc khoảng ngày */}
      <div
        style={{
          backgroundColor: '#ffffff',
          borderRadius: '10px',
          padding: '20px',
          marginBottom: '24px',
          boxShadow: '0 1px 3px rgba(0, 0, 0, 0.08)',
        }}
      >
        <form
          onSubmit={handleFilterSubmit}
          style={{
            display: 'flex',
            flexWrap: 'wrap',
            gap: '16px',
            alignItems: 'flex-end',
          }}
        >
          <div style={{ flex: '1 1 200px' }}>
            <label
              htmlFor="fromDateInput"
              style={{ display: 'block', fontSize: '0.85rem', fontWeight: '600', color: '#374151', marginBottom: '6px' }}
            >
              Từ ngày
            </label>
            <input
              id="fromDateInput"
              type="date"
              value={fromDate}
              onChange={(e) => setFromDate(e.target.value)}
              style={{
                width: '100%',
                padding: '8px 12px',
                borderRadius: '6px',
                border: '1px solid #d1d5db',
                fontSize: '0.9rem',
                outline: 'none',
                boxSizing: 'border-box',
              }}
            />
          </div>

          <div style={{ flex: '1 1 200px' }}>
            <label
              htmlFor="toDateInput"
              style={{ display: 'block', fontSize: '0.85rem', fontWeight: '600', color: '#374151', marginBottom: '6px' }}
            >
              Đến ngày
            </label>
            <input
              id="toDateInput"
              type="date"
              value={toDate}
              onChange={(e) => setToDate(e.target.value)}
              style={{
                width: '100%',
                padding: '8px 12px',
                borderRadius: '6px',
                border: '1px solid #d1d5db',
                fontSize: '0.9rem',
                outline: 'none',
                boxSizing: 'border-box',
              }}
            />
          </div>

          <div style={{ display: 'flex', gap: '8px' }}>
            <button
              type="submit"
              disabled={loading}
              style={{
                padding: '9px 20px',
                backgroundColor: '#2563eb',
                color: '#ffffff',
                borderRadius: '6px',
                fontWeight: '600',
                fontSize: '0.9rem',
                cursor: loading ? 'not-allowed' : 'pointer',
              }}
            >
              {loading ? 'Đang tải...' : 'Xem báo cáo'}
            </button>

            <button
              type="button"
              onClick={() => {
                const d = new Date();
                const todayStr = d.toISOString().split('T')[0];
                d.setDate(d.getDate() - 30);
                setFromDate(d.toISOString().split('T')[0]);
                setToDate(todayStr);
              }}
              style={{
                padding: '9px 14px',
                backgroundColor: '#f3f4f6',
                color: '#374151',
                borderRadius: '6px',
                fontWeight: '500',
                fontSize: '0.85rem',
                cursor: 'pointer',
                border: '1px solid #d1d5db',
              }}
            >
              30 ngày qua
            </button>
          </div>
        </form>
      </div>

      {/* Tabs chuyển đổi Doanh thu / Tỷ lệ lấp đầy */}
      <div
        style={{
          display: 'flex',
          borderBottom: '2px solid #e5e7eb',
          marginBottom: '24px',
          gap: '8px',
        }}
      >
        <button
          type="button"
          onClick={() => setActiveTab('revenue')}
          style={{
            padding: '12px 24px',
            fontSize: '1rem',
            fontWeight: '600',
            color: activeTab === 'revenue' ? '#2563eb' : '#6b7280',
            borderBottom: activeTab === 'revenue' ? '3px solid #2563eb' : '3px solid transparent',
            marginBottom: '-2px',
            cursor: 'pointer',
            backgroundColor: 'transparent',
            transition: 'color 0.15s, border-color 0.15s',
          }}
        >
          📈 Doanh Thu
        </button>

        <button
          type="button"
          onClick={() => setActiveTab('occupancy')}
          style={{
            padding: '12px 24px',
            fontSize: '1rem',
            fontWeight: '600',
            color: activeTab === 'occupancy' ? '#2563eb' : '#6b7280',
            borderBottom: activeTab === 'occupancy' ? '3px solid #2563eb' : '3px solid transparent',
            marginBottom: '-2px',
            cursor: 'pointer',
            backgroundColor: 'transparent',
            transition: 'color 0.15s, border-color 0.15s',
          }}
        >
          🚗 Tỷ Lệ Lấp Đầy Đội Xe
        </button>
      </div>

      {/* Thông báo lỗi nếu có */}
      {error && (
        <div
          style={{
            backgroundColor: '#fef2f2',
            border: '1px solid #fee2e2',
            color: '#b91c1c',
            padding: '14px 18px',
            borderRadius: '8px',
            marginBottom: '24px',
            fontSize: '0.9rem',
          }}
        >
          {error}
        </div>
      )}

      {/* Nội dung báo cáo theo tab */}
      {loading ? (
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
          <div
            style={{
              width: '36px',
              height: '36px',
              border: '3px solid #e5e7eb',
              borderTopColor: '#2563eb',
              borderRadius: '50%',
              margin: '0 auto 12px',
              animation: 'spin 1s linear infinite',
            }}
          />
          <p style={{ margin: 0 }}>Đang tổng hợp dữ liệu báo cáo...</p>
        </div>
      ) : activeTab === 'revenue' ? (
        /* =====================================================================
           TAB 1: BÁO CÁO DOANH THU
           GET /reports/revenue?from=&to=
           ===================================================================== */
        <div>
          {/* Thẻ thống kê tổng quan doanh thu */}
          <div
            style={{
              display: 'grid',
              gridTemplateColumns: 'repeat(auto-fit, minmax(280px, 1fr))',
              gap: '20px',
              marginBottom: '24px',
            }}
          >
            {/* Card Tổng doanh thu */}
            <div
              style={{
                backgroundColor: '#ffffff',
                borderRadius: '10px',
                padding: '22px',
                boxShadow: '0 1px 3px rgba(0, 0, 0, 0.08)',
                borderLeft: '4px solid #10b981',
              }}
            >
              <div style={{ fontSize: '0.85rem', color: '#6b7280', fontWeight: '600', textTransform: 'uppercase' }}>
                Tổng Doanh Thu
              </div>
              <div style={{ fontSize: '1.8rem', fontWeight: '800', color: '#047857', marginTop: '8px' }}>
                {renderPrice(revenueData?.totalRevenue)}
              </div>
              <div style={{ fontSize: '0.8rem', color: '#9ca3af', marginTop: '4px' }}>
                Theo khoảng thời gian đã chọn
              </div>
            </div>

            {/* Card Tổng số đơn thuê */}
            <div
              style={{
                backgroundColor: '#ffffff',
                borderRadius: '10px',
                padding: '22px',
                boxShadow: '0 1px 3px rgba(0, 0, 0, 0.08)',
                borderLeft: '4px solid #2563eb',
              }}
            >
              <div style={{ fontSize: '0.85rem', color: '#6b7280', fontWeight: '600', textTransform: 'uppercase' }}>
                Tổng Số Đơn Thuê
              </div>
              <div style={{ fontSize: '1.8rem', fontWeight: '800', color: '#1d4ed8', marginTop: '8px' }}>
                {revenueData?.totalBookings || 0}
              </div>
              <div style={{ fontSize: '0.8rem', color: '#9ca3af', marginTop: '4px' }}>
                Đơn hoàn thành & ghi nhận thành công
              </div>
            </div>
          </div>

          {/* Bảng chi tiết doanh thu theo tháng (byMonth) */}
          <div
            style={{
              backgroundColor: '#ffffff',
              borderRadius: '10px',
              boxShadow: '0 1px 3px rgba(0, 0, 0, 0.08)',
              overflow: 'hidden',
            }}
          >
            <div style={{ padding: '18px 20px', borderBottom: '1px solid #e5e7eb' }}>
              <h2 style={{ fontSize: '1.1rem', fontWeight: '600', color: '#111827', margin: 0 }}>
                Chi Tiết Doanh Thu Theo Tháng
              </h2>
            </div>

            {!revenueData?.byMonth || revenueData.byMonth.length === 0 ? (
              <div style={{ padding: '40px 20px', textAlign: 'center', color: '#6b7280' }}>
                Không có dữ liệu doanh thu trong khoảng thời gian này.
              </div>
            ) : (
              <div style={{ overflowX: 'auto' }}>
                <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left' }}>
                  <thead>
                    <tr
                      style={{
                        backgroundColor: '#f9fafb',
                        borderBottom: '1px solid #e5e7eb',
                        color: '#4b5563',
                        fontSize: '0.8125rem',
                        textTransform: 'uppercase',
                        letterSpacing: '0.05em',
                      }}
                    >
                      <th style={{ padding: '14px 20px' }}>Tháng</th>
                      <th style={{ padding: '14px 20px' }}>Doanh Thu</th>
                      <th style={{ padding: '14px 20px' }}>Số Lượng Đơn</th>
                      <th style={{ padding: '14px 20px' }}>Doanh Thu TB / Đơn</th>
                    </tr>
                  </thead>
                  <tbody style={{ fontSize: '0.9rem', color: '#1f2937' }}>
                    {revenueData.byMonth.map((item, index) => {
                      const avgPerBooking =
                        item.bookings > 0 ? item.revenue / item.bookings : 0;

                      return (
                        <tr
                          key={item.month || index}
                          style={{
                            borderBottom: '1px solid #f3f4f6',
                            transition: 'background-color 0.15s',
                          }}
                          onMouseEnter={(e) => (e.currentTarget.style.backgroundColor = '#f9fafb')}
                          onMouseLeave={(e) => (e.currentTarget.style.backgroundColor = 'transparent')}
                        >
                          <td style={{ padding: '14px 20px', fontWeight: '600', color: '#1f2937' }}>
                            {item.month}
                          </td>
                          <td style={{ padding: '14px 20px', fontWeight: '700', color: '#047857' }}>
                            {renderPrice(item.revenue)}
                          </td>
                          <td style={{ padding: '14px 20px', color: '#4b5563' }}>
                            {item.bookings} đơn
                          </td>
                          <td style={{ padding: '14px 20px', color: '#6b7280' }}>
                            {renderPrice(avgPerBooking)}
                          </td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        </div>
      ) : (
        /* =====================================================================
           TAB 2: BÁO CÁO TỶ LỆ LẤP ĐẦY
           GET /reports/occupancy?from=&to=
           ===================================================================== */
        <div>
          {/* Thẻ thống kê tổng quan lấp đầy */}
          <div
            style={{
              display: 'grid',
              gridTemplateColumns: 'repeat(auto-fit, minmax(280px, 1fr))',
              gap: '20px',
              marginBottom: '24px',
            }}
          >
            {/* Card Tổng số xe */}
            <div
              style={{
                backgroundColor: '#ffffff',
                borderRadius: '10px',
                padding: '22px',
                boxShadow: '0 1px 3px rgba(0, 0, 0, 0.08)',
                borderLeft: '4px solid #6366f1',
              }}
            >
              <div style={{ fontSize: '0.85rem', color: '#6b7280', fontWeight: '600', textTransform: 'uppercase' }}>
                Tổng Số Xe Trong Hệ Thống
              </div>
              <div style={{ fontSize: '1.8rem', fontWeight: '800', color: '#4338ca', marginTop: '8px' }}>
                {occupancyData?.totalCars || 0} xe
              </div>
              <div style={{ fontSize: '0.8rem', color: '#9ca3af', marginTop: '4px' }}>
                Tổng số lượng phương tiện vận hành
              </div>
            </div>

            {/* Card Tỷ lệ lấp đầy trung bình */}
            <div
              style={{
                backgroundColor: '#ffffff',
                borderRadius: '10px',
                padding: '22px',
                boxShadow: '0 1px 3px rgba(0, 0, 0, 0.08)',
                borderLeft: '4px solid #f59e0b',
              }}
            >
              <div style={{ fontSize: '0.85rem', color: '#6b7280', fontWeight: '600', textTransform: 'uppercase' }}>
                Tỷ Lệ Lấp Đầy Trung Bình
              </div>
              <div style={{ fontSize: '1.8rem', fontWeight: '800', color: '#b45309', marginTop: '8px' }}>
                {renderPercentage(occupancyData?.avgOccupancyRate)}
              </div>
              <div style={{ fontSize: '0.8rem', color: '#9ca3af', marginTop: '4px' }}>
                Thời gian xe lăn bánh phục vụ khách
              </div>
            </div>
          </div>

          {/* Bảng chi tiết tỷ lệ lấp đầy theo xe (byCar) */}
          <div
            style={{
              backgroundColor: '#ffffff',
              borderRadius: '10px',
              boxShadow: '0 1px 3px rgba(0, 0, 0, 0.08)',
              overflow: 'hidden',
            }}
          >
            <div style={{ padding: '18px 20px', borderBottom: '1px solid #e5e7eb' }}>
              <h2 style={{ fontSize: '1.1rem', fontWeight: '600', color: '#111827', margin: 0 }}>
                Hiệu Suất Khai Thác Theo Phương Tiện
              </h2>
            </div>

            {!occupancyData?.byCar || occupancyData.byCar.length === 0 ? (
              <div style={{ padding: '40px 20px', textAlign: 'center', color: '#6b7280' }}>
                Không có dữ liệu tỷ lệ lấp đầy trong khoảng thời gian này.
              </div>
            ) : (
              <div style={{ overflowX: 'auto' }}>
                <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left' }}>
                  <thead>
                    <tr
                      style={{
                        backgroundColor: '#f9fafb',
                        borderBottom: '1px solid #e5e7eb',
                        color: '#4b5563',
                        fontSize: '0.8125rem',
                        textTransform: 'uppercase',
                        letterSpacing: '0.05em',
                      }}
                    >
                      <th style={{ padding: '14px 20px' }}>Mã Xe</th>
                      <th style={{ padding: '14px 20px' }}>Biển Số Xe</th>
                      <th style={{ padding: '14px 20px' }}>Tỷ Lệ Lấp Đầy</th>
                      <th style={{ padding: '14px 20px', minWidth: '180px' }}>Biểu Đồ Hiệu Suất</th>
                    </tr>
                  </thead>
                  <tbody style={{ fontSize: '0.9rem', color: '#1f2937' }}>
                    {occupancyData.byCar.map((car, index) => {
                      const pctVal = getPercentValue(car.occupancyRate);
                      // Màu sắc thanh biểu đồ dựa theo tỷ lệ lấp đầy
                      const barColor =
                        pctVal >= 70 ? '#10b981' : pctVal >= 40 ? '#f59e0b' : '#ef4444';

                      return (
                        <tr
                          key={car.carId || index}
                          style={{
                            borderBottom: '1px solid #f3f4f6',
                            transition: 'background-color 0.15s',
                          }}
                          onMouseEnter={(e) => (e.currentTarget.style.backgroundColor = '#f9fafb')}
                          onMouseLeave={(e) => (e.currentTarget.style.backgroundColor = 'transparent')}
                        >
                          <td style={{ padding: '14px 20px', fontWeight: '600', color: '#6b7280' }}>
                            #{car.carId}
                          </td>
                          <td style={{ padding: '14px 20px' }}>
                            <span
                              style={{
                                fontFamily: 'monospace',
                                backgroundColor: '#f3f4f6',
                                padding: '4px 8px',
                                borderRadius: '4px',
                                fontSize: '0.875rem',
                                fontWeight: '600',
                                color: '#1f2937',
                              }}
                            >
                              {car.licensePlate || 'N/A'}
                            </span>
                          </td>
                          <td style={{ padding: '14px 20px', fontWeight: '700', color: barColor }}>
                            {renderPercentage(car.occupancyRate)}
                          </td>
                          <td style={{ padding: '14px 20px' }}>
                            <div
                              style={{
                                width: '100%',
                                backgroundColor: '#e5e7eb',
                                borderRadius: '9999px',
                                height: '10px',
                                overflow: 'hidden',
                              }}
                            >
                              <div
                                style={{
                                  width: `${pctVal}%`,
                                  backgroundColor: barColor,
                                  height: '100%',
                                  borderRadius: '9999px',
                                  transition: 'width 0.4s ease',
                                }}
                              />
                            </div>
                          </td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        </div>
      )}
    </div>
  );
};

export default AdminReportsPage;
