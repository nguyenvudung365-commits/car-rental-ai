import React, { useState, useEffect, useCallback } from 'react';
import axiosClient from '../../api/axiosClient';
import { formatCurrency } from '../../utils/formatCurrency';

/**
 * Trang Quản lý Đơn thuê (AdminBookingsPage)
 * Chức năng:
 * - Xem danh sách tất cả các đơn đặt xe (GET /bookings)
 * - Duyệt đơn thuê chờ duyệt (PUT /bookings/:id/approve)
 * - Hủy đơn thuê kèm lý do (PUT /bookings/:id/cancel)
 * - Ghi đè (override) giá đơn thuê (PUT /bookings/:id/override-price)
 * - Bộ lọc trạng thái và tìm kiếm khách hàng/xe
 */
const AdminBookingsPage = () => {
  const [bookings, setBookings] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [notification, setNotification] = useState(null);

  // Bộ lọc và tìm kiếm
  const [statusFilter, setStatusFilter] = useState('ALL');
  const [searchTerm, setSearchTerm] = useState('');

  // Trạng thái Modal Hủy đơn
  const [cancelModalOpen, setCancelModalOpen] = useState(false);
  const [cancelBooking, setCancelBooking] = useState(null);
  const [cancelReason, setCancelReason] = useState('');

  // Trạng thái Modal Override giá
  const [overrideModalOpen, setOverrideModalOpen] = useState(false);
  const [overrideBooking, setOverrideBooking] = useState(null);
  const [newPrice, setNewPrice] = useState('');
  const [overrideReason, setOverrideReason] = useState('');

  // Trạng thái xử lý form modal
  const [actionLoading, setActionLoading] = useState(false);
  const [modalError, setModalError] = useState('');

  /**
   * Lấy danh sách tất cả các đơn thuê xe từ hệ thống
   * API Endpoint: GET /bookings (Dành cho Admin)
   */
  const fetchBookings = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const response = await axiosClient.get('/bookings');
      // Xử lý dữ liệu trả về linh hoạt: mảng trực tiếp hoặc { items: [] } hoặc { data: [] }
      const data = Array.isArray(response.data)
        ? response.data
        : (response.data?.items || response.data?.data || []);
      setBookings(data);
    } catch (err) {
      console.error('Lỗi khi tải danh sách đơn thuê:', err);
      setError(err.response?.data?.message || 'Không thể tải danh sách đơn đặt xe. Vui lòng thử lại!');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchBookings();
  }, [fetchBookings]);

  // Tự động tắt thông báo sau 4 giây
  useEffect(() => {
    if (notification) {
      const timer = setTimeout(() => setNotification(null), 4000);
      return () => clearTimeout(timer);
    }
  }, [notification]);

  /**
   * Duyệt đơn thuê Pending
   * API Endpoint: PUT /bookings/:id/approve
   */
  const handleApprove = async (booking) => {
    const confirmMessage = `Bạn có chắc chắn muốn duyệt đơn thuê #${booking.id} của khách hàng ${booking.customerName || 'này'}?`;
    if (!window.confirm(confirmMessage)) return;

    try {
      setActionLoading(true);
      await axiosClient.put(`/bookings/${booking.id}/approve`);
      setNotification({
        type: 'success',
        message: `Đã duyệt thành công đơn thuê #${booking.id}!`,
      });
      await fetchBookings();
    } catch (err) {
      console.error('Lỗi khi duyệt đơn:', err);
      setNotification({
        type: 'error',
        message: err.response?.data?.message || `Không thể duyệt đơn #${booking.id}.`,
      });
    } finally {
      setActionLoading(false);
    }
  };

  /**
   * Mở modal hủy đơn thuê
   */
  const openCancelModal = (booking) => {
    setCancelBooking(booking);
    setCancelReason('');
    setModalError('');
    setCancelModalOpen(true);
  };

  const closeCancelModal = () => {
    setCancelModalOpen(false);
    setCancelBooking(null);
    setCancelReason('');
    setModalError('');
  };

  /**
   * Xác nhận hủy đơn thuê
   * API Endpoint: PUT /bookings/:id/cancel
   * Request Body: { reason }
   */
  const handleConfirmCancel = async (e) => {
    e.preventDefault();
    if (!cancelReason.trim()) {
      setModalError('Vui lòng nhập lý do hủy đơn thuê.');
      return;
    }

    try {
      setActionLoading(true);
      setModalError('');
      await axiosClient.put(`/bookings/${cancelBooking.id}/cancel`, {
        reason: cancelReason.trim(),
      });
      setNotification({
        type: 'success',
        message: `Đã hủy đơn thuê #${cancelBooking.id} thành công!`,
      });
      closeCancelModal();
      await fetchBookings();
    } catch (err) {
      console.error('Lỗi khi hủy đơn thuê:', err);
      setModalError(err.response?.data?.message || 'Có lỗi xảy ra khi hủy đơn thuê.');
    } finally {
      setActionLoading(false);
    }
  };

  /**
   * Mở modal điều chỉnh (override) giá
   */
  const openOverrideModal = (booking) => {
    setOverrideBooking(booking);
    setNewPrice(booking.finalPrice || '');
    setOverrideReason('');
    setModalError('');
    setOverrideModalOpen(true);
  };

  const closeOverrideModal = () => {
    setOverrideModalOpen(false);
    setOverrideBooking(null);
    setNewPrice('');
    setOverrideReason('');
    setModalError('');
  };

  /**
   * Xác nhận ghi đè giá đơn thuê
   * API Endpoint: PUT /bookings/:id/override-price
   * Request Body: { newPrice, reason }
   */
  const handleConfirmOverridePrice = async (e) => {
    e.preventDefault();
    const priceNum = Number(newPrice);
    if (isNaN(priceNum) || priceNum < 0) {
      setModalError('Vui lòng nhập mức giá hợp lệ (lớn hơn hoặc bằng 0).');
      return;
    }
    if (!overrideReason.trim()) {
      setModalError('Vui lòng nhập lý do điều chỉnh giá.');
      return;
    }

    try {
      setActionLoading(true);
      setModalError('');
      await axiosClient.put(`/bookings/${overrideBooking.id}/override-price`, {
        newPrice: priceNum,
        reason: overrideReason.trim(),
      });
      setNotification({
        type: 'success',
        message: `Ghi đè giá đơn #${overrideBooking.id} thành công!`,
      });
      closeOverrideModal();
      await fetchBookings();
    } catch (err) {
      console.error('Lỗi khi override giá:', err);
      setModalError(err.response?.data?.message || 'Có lỗi xảy ra khi ghi đè giá.');
    } finally {
      setActionLoading(false);
    }
  };

  // Định dạng hiển thị tiền tệ an toàn
  const renderPrice = (price) => {
    if (price === null || price === undefined) return '0 ₫';
    try {
      return formatCurrency(price);
    } catch {
      return Number(price || 0).toLocaleString('vi-VN') + ' ₫';
    }
  };

  // Định dạng ngày tháng
  const formatDate = (dateStr) => {
    if (!dateStr) return '-';
    try {
      const d = new Date(dateStr);
      if (isNaN(d.getTime())) return dateStr;
      return d.toLocaleDateString('vi-VN', {
        day: '2-digit',
        month: '2-digit',
        year: 'numeric',
      });
    } catch {
      return dateStr;
    }
  };

  // Badge màu sắc cho các trạng thái đơn đặt xe
  const renderStatusBadge = (status) => {
    const statusMap = {
      Pending: { label: 'Chờ duyệt', bg: '#fef3c7', text: '#92400e', border: '#fde68a' },
      Confirmed: { label: 'Đã duyệt', bg: '#dbeafe', text: '#1e40af', border: '#bfdbfe' },
      Completed: { label: 'Hoàn thành', bg: '#dcfce7', text: '#166534', border: '#bbf7d0' },
      Cancelled: { label: 'Đã hủy', bg: '#fee2e2', text: '#991b1b', border: '#fecaca' },
    };

    const config = statusMap[status] || {
      label: status || 'Không xác định',
      bg: '#f3f4f6',
      text: '#374151',
      border: '#e5e7eb',
    };

    return (
      <span
        style={{
          display: 'inline-block',
          padding: '4px 10px',
          borderRadius: '9999px',
          fontSize: '0.8125rem',
          fontWeight: '600',
          backgroundColor: config.bg,
          color: config.text,
          border: `1px solid ${config.border}`,
          whiteSpace: 'nowrap',
        }}
      >
        {config.label}
      </span>
    );
  };

  // Lọc dữ liệu hiển thị theo trạng thái và từ khóa tìm kiếm
  const filteredBookings = bookings.filter((b) => {
    const matchesStatus = statusFilter === 'ALL' || b.status === statusFilter;
    const searchLower = searchTerm.toLowerCase().trim();
    if (!searchLower) return matchesStatus;

    const customer = (b.customerName || '').toLowerCase();
    const phone = (b.customerPhone || '').toLowerCase();
    const carBrand = (b.carBrand || '').toLowerCase();
    const carModel = (b.carModel || '').toLowerCase();
    const license = (b.licensePlate || '').toLowerCase();
    const id = String(b.id || '');

    const matchesSearch =
      customer.includes(searchLower) ||
      phone.includes(searchLower) ||
      carBrand.includes(searchLower) ||
      carModel.includes(searchLower) ||
      license.includes(searchLower) ||
      id.includes(searchLower);

    return matchesStatus && matchesSearch;
  });

  return (
    <div style={{ maxWidth: '1280px', margin: '0 auto', padding: '24px 16px' }}>
      {/* Tiêu đề trang */}
      <div style={{ marginBottom: '24px' }}>
        <h1 style={{ fontSize: '1.75rem', fontWeight: '700', color: '#111827', margin: 0 }}>
          Quản Lý Đơn Thuê Xe
        </h1>
        <p style={{ color: '#6b7280', marginTop: '6px', fontSize: '0.95rem' }}>
          Xem danh sách đơn đặt xe, duyệt đơn chờ duyệt, hủy đơn và điều chỉnh giá thuê.
        </p>
      </div>

      {/* Thông báo Alert */}
      {notification && (
        <div
          style={{
            marginBottom: '20px',
            padding: '12px 16px',
            borderRadius: '8px',
            fontSize: '0.925rem',
            backgroundColor: notification.type === 'success' ? '#ecfdf5' : '#fef2f2',
            color: notification.type === 'success' ? '#065f46' : '#991b1b',
            border: `1px solid ${notification.type === 'success' ? '#a7f3d0' : '#fecaca'}`,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
          }}
        >
          <span>{notification.message}</span>
          <button
            onClick={() => setNotification(null)}
            style={{ fontWeight: 'bold', fontSize: '1.1rem', lineHeight: 1, padding: '0 4px' }}
          >
            ×
          </button>
        </div>
      )}

      {/* Thanh công cụ: Tìm kiếm & Bộ lọc */}
      <div
        style={{
          display: 'flex',
          flexWrap: 'wrap',
          gap: '12px',
          alignItems: 'center',
          justifyContent: 'space-between',
          backgroundColor: '#ffffff',
          padding: '16px',
          borderRadius: '10px',
          boxShadow: '0 1px 3px rgba(0, 0, 0, 0.08)',
          marginBottom: '20px',
        }}
      >
        <div style={{ display: 'flex', flexWrap: 'wrap', gap: '12px', flex: 1 }}>
          <input
            type="text"
            placeholder="Tìm theo khách hàng, SĐT, biển số, xe..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            style={{
              padding: '8px 14px',
              borderRadius: '6px',
              border: '1px solid #d1d5db',
              fontSize: '0.9rem',
              minWidth: '260px',
              flex: '1 1 260px',
              outline: 'none',
            }}
          />

          <select
            value={statusFilter}
            onChange={(e) => setStatusFilter(e.target.value)}
            style={{
              padding: '8px 14px',
              borderRadius: '6px',
              border: '1px solid #d1d5db',
              fontSize: '0.9rem',
              backgroundColor: '#fff',
              outline: 'none',
              cursor: 'pointer',
            }}
          >
            <option value="ALL">Tất cả trạng thái</option>
            <option value="Pending">Chờ duyệt (Pending)</option>
            <option value="Confirmed">Đã duyệt (Confirmed)</option>
            <option value="Completed">Hoàn thành (Completed)</option>
            <option value="Cancelled">Đã hủy (Cancelled)</option>
          </select>
        </div>

        <button
          onClick={fetchBookings}
          disabled={loading}
          style={{
            padding: '8px 16px',
            backgroundColor: '#f3f4f6',
            color: '#374151',
            borderRadius: '6px',
            border: '1px solid #d1d5db',
            fontSize: '0.9rem',
            fontWeight: '500',
            cursor: loading ? 'not-allowed' : 'pointer',
            display: 'inline-flex',
            alignItems: 'center',
            gap: '6px',
          }}
        >
          {loading ? 'Đang tải...' : 'Làm mới danh sách'}
        </button>
      </div>

      {/* Nội dung bảng danh sách */}
      <div
        style={{
          backgroundColor: '#ffffff',
          borderRadius: '10px',
          boxShadow: '0 1px 3px rgba(0, 0, 0, 0.08)',
          overflow: 'hidden',
        }}
      >
        {loading && bookings.length === 0 ? (
          <div style={{ padding: '60px 20px', textAlign: 'center', color: '#6b7280' }}>
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
            <p>Đang tải danh sách đơn thuê xe...</p>
          </div>
        ) : error ? (
          <div style={{ padding: '40px 20px', textAlign: 'center', color: '#dc2626' }}>
            <p style={{ fontWeight: '500' }}>{error}</p>
            <button
              onClick={fetchBookings}
              style={{
                marginTop: '12px',
                padding: '8px 16px',
                backgroundColor: '#2563eb',
                color: '#fff',
                borderRadius: '6px',
                fontSize: '0.9rem',
              }}
            >
              Thử lại
            </button>
          </div>
        ) : filteredBookings.length === 0 ? (
          <div style={{ padding: '50px 20px', textAlign: 'center', color: '#6b7280' }}>
            <p style={{ fontSize: '1rem', marginBottom: '8px' }}>
              {searchTerm || statusFilter !== 'ALL'
                ? 'Không tìm thấy đơn thuê nào phù hợp với bộ lọc.'
                : 'Chưa có đơn đặt xe nào trong hệ thống.'}
            </p>
            {(searchTerm || statusFilter !== 'ALL') && (
              <button
                onClick={() => {
                  setSearchTerm('');
                  setStatusFilter('ALL');
                }}
                style={{
                  color: '#2563eb',
                  textDecoration: 'underline',
                  fontSize: '0.9rem',
                  fontWeight: '500',
                }}
              >
                Xóa bộ lọc tìm kiếm
              </button>
            )}
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
                  <th style={{ padding: '14px 16px' }}>Mã đơn</th>
                  <th style={{ padding: '14px 16px' }}>Khách hàng</th>
                  <th style={{ padding: '14px 16px' }}>SĐT</th>
                  <th style={{ padding: '14px 16px' }}>Xe thuê</th>
                  <th style={{ padding: '14px 16px' }}>Biển số</th>
                  <th style={{ padding: '14px 16px' }}>Ngày thuê</th>
                  <th style={{ padding: '14px 16px' }}>Giá cuối</th>
                  <th style={{ padding: '14px 16px' }}>Trạng thái</th>
                  <th style={{ padding: '14px 16px', textAlign: 'center' }}>Thao tác</th>
                </tr>
              </thead>
              <tbody style={{ fontSize: '0.875rem', color: '#1f2937' }}>
                {filteredBookings.map((b) => {
                  const isPending = b.status === 'Pending';
                  const canOverride = b.status === 'Pending' || b.status === 'Confirmed';
                  const carTitle = `${b.carBrand || ''} ${b.carModel || ''}`.trim() || `Xe #${b.carId}`;

                  return (
                    <tr
                      key={b.id}
                      style={{
                        borderBottom: '1px solid #f3f4f6',
                        transition: 'background-color 0.15s',
                      }}
                      onMouseEnter={(e) => (e.currentTarget.style.backgroundColor = '#f9fafb')}
                      onMouseLeave={(e) => (e.currentTarget.style.backgroundColor = 'transparent')}
                    >
                      <td style={{ padding: '14px 16px', fontWeight: '600', color: '#4b5563' }}>
                        #{b.id}
                      </td>
                      <td style={{ padding: '14px 16px', fontWeight: '500' }}>
                        {b.customerName || 'Khách vãng lai'}
                      </td>
                      <td style={{ padding: '14px 16px', color: '#4b5563' }}>
                        {b.customerPhone || '-'}
                      </td>
                      <td style={{ padding: '14px 16px', fontWeight: '500', color: '#111827' }}>
                        {carTitle}
                      </td>
                      <td style={{ padding: '14px 16px' }}>
                        <span
                          style={{
                            fontFamily: 'monospace',
                            backgroundColor: '#f3f4f6',
                            padding: '2px 6px',
                            borderRadius: '4px',
                            fontSize: '0.8125rem',
                          }}
                        >
                          {b.licensePlate || '-'}
                        </span>
                      </td>
                      <td style={{ padding: '14px 16px', whiteSpace: 'nowrap' }}>
                        {formatDate(b.startDate)}
                        <span style={{ color: '#9ca3af', margin: '0 4px' }}>→</span>
                        {formatDate(b.endDate)}
                      </td>
                      <td style={{ padding: '14px 16px', fontWeight: '700', color: '#047857', whiteSpace: 'nowrap' }}>
                        {renderPrice(b.finalPrice)}
                      </td>
                      <td style={{ padding: '14px 16px' }}>
                        {renderStatusBadge(b.status)}
                      </td>
                      <td style={{ padding: '14px 16px', textAlign: 'center' }}>
                        <div
                          style={{
                            display: 'flex',
                            gap: '6px',
                            justifyContent: 'center',
                            alignItems: 'center',
                            flexWrap: 'nowrap',
                          }}
                        >
                          {/* Nút Duyệt - chỉ hiển thị khi Pending */}
                          {isPending && (
                            <button
                              type="button"
                              onClick={() => handleApprove(b)}
                              disabled={actionLoading}
                              title="Duyệt đơn thuê"
                              style={{
                                padding: '6px 10px',
                                backgroundColor: '#10b981',
                                color: '#ffffff',
                                borderRadius: '6px',
                                fontSize: '0.8rem',
                                fontWeight: '600',
                                cursor: 'pointer',
                              }}
                            >
                              Duyệt
                            </button>
                          )}

                          {/* Nút Hủy - chỉ hiển thị khi Pending */}
                          {isPending && (
                            <button
                              type="button"
                              onClick={() => openCancelModal(b)}
                              disabled={actionLoading}
                              title="Hủy đơn thuê"
                              style={{
                                padding: '6px 10px',
                                backgroundColor: '#ef4444',
                                color: '#ffffff',
                                borderRadius: '6px',
                                fontSize: '0.8rem',
                                fontWeight: '600',
                                cursor: 'pointer',
                              }}
                            >
                              Hủy
                            </button>
                          )}

                          {/* Nút Override giá - hiển thị cho Pending và Confirmed */}
                          {canOverride && (
                            <button
                              type="button"
                              onClick={() => openOverrideModal(b)}
                              disabled={actionLoading}
                              title="Ghi đè giá đơn thuê"
                              style={{
                                padding: '6px 10px',
                                backgroundColor: '#3b82f6',
                                color: '#ffffff',
                                borderRadius: '6px',
                                fontSize: '0.8rem',
                                fontWeight: '600',
                                cursor: 'pointer',
                                whiteSpace: 'nowrap',
                              }}
                            >
                              Override giá
                            </button>
                          )}

                          {/* Khi không còn thao tác nào khả dụng */}
                          {!isPending && !canOverride && (
                            <span style={{ color: '#9ca3af', fontSize: '0.8rem' }}>—</span>
                          )}
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

      {/* =====================================================================
          MODAL HỦY ĐƠN THUÊ (.modal-overlay + .modal)
          PUT /bookings/:id/cancel with { reason }
          ===================================================================== */}
      {cancelModalOpen && cancelBooking && (
        <div
          className="modal-overlay"
          style={{
            position: 'fixed',
            top: 0,
            left: 0,
            right: 0,
            bottom: 0,
            backgroundColor: 'rgba(0, 0, 0, 0.55)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            zIndex: 1000,
            padding: '16px',
          }}
          onClick={closeCancelModal}
        >
          <div
            className="modal"
            style={{
              backgroundColor: '#ffffff',
              borderRadius: '12px',
              maxWidth: '480px',
              width: '100%',
              padding: '24px',
              boxShadow: '0 20px 25px -5px rgba(0, 0, 0, 0.1), 0 10px 10px -5px rgba(0, 0, 0, 0.04)',
            }}
            onClick={(e) => e.stopPropagation()}
          >
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '16px' }}>
              <h3 style={{ margin: 0, fontSize: '1.25rem', color: '#111827' }}>
                Hủy Đơn Thuê #{cancelBooking.id}
              </h3>
              <button
                type="button"
                onClick={closeCancelModal}
                style={{ fontSize: '1.5rem', lineHeight: 1, color: '#9ca3af', cursor: 'pointer' }}
              >
                ×
              </button>
            </div>

            <div
              style={{
                backgroundColor: '#fef2f2',
                border: '1px solid #fee2e2',
                borderRadius: '8px',
                padding: '12px',
                marginBottom: '16px',
                fontSize: '0.875rem',
                color: '#991b1b',
              }}
            >
              <p style={{ margin: '0 0 4px 0' }}>
                <strong>Khách hàng:</strong> {cancelBooking.customerName || 'N/A'} ({cancelBooking.customerPhone || 'N/A'})
              </p>
              <p style={{ margin: 0 }}>
                <strong>Xe:</strong> {cancelBooking.carBrand} {cancelBooking.carModel} - Biển số: {cancelBooking.licensePlate}
              </p>
            </div>

            {modalError && (
              <div
                style={{
                  backgroundColor: '#fee2e2',
                  color: '#991b1b',
                  padding: '8px 12px',
                  borderRadius: '6px',
                  fontSize: '0.85rem',
                  marginBottom: '12px',
                }}
              >
                {modalError}
              </div>
            )}

            <form onSubmit={handleConfirmCancel}>
              <div style={{ marginBottom: '16px' }}>
                <label
                  htmlFor="cancelReason"
                  style={{ display: 'block', marginBottom: '6px', fontSize: '0.875rem', fontWeight: '500', color: '#374151' }}
                >
                  Lý do hủy đơn <span style={{ color: '#dc2626' }}>*</span>
                </label>
                <textarea
                  id="cancelReason"
                  rows={4}
                  required
                  placeholder="Nhập lý do hủy đơn (ví dụ: Khách hàng đổi kế hoạch, xe bảo trì đột xuất...)"
                  value={cancelReason}
                  onChange={(e) => setCancelReason(e.target.value)}
                  style={{
                    width: '100%',
                    padding: '10px 12px',
                    borderRadius: '6px',
                    border: '1px solid #d1d5db',
                    fontSize: '0.9rem',
                    outline: 'none',
                    resize: 'vertical',
                    boxSizing: 'border-box',
                  }}
                />
              </div>

              <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '10px' }}>
                <button
                  type="button"
                  onClick={closeCancelModal}
                  disabled={actionLoading}
                  style={{
                    padding: '8px 16px',
                    backgroundColor: '#e5e7eb',
                    color: '#374151',
                    borderRadius: '6px',
                    fontWeight: '500',
                    fontSize: '0.875rem',
                    cursor: 'pointer',
                  }}
                >
                  Đóng
                </button>
                <button
                  type="submit"
                  disabled={actionLoading}
                  style={{
                    padding: '8px 18px',
                    backgroundColor: '#dc2626',
                    color: '#ffffff',
                    borderRadius: '6px',
                    fontWeight: '600',
                    fontSize: '0.875rem',
                    cursor: actionLoading ? 'not-allowed' : 'pointer',
                  }}
                >
                  {actionLoading ? 'Đang xử lý...' : 'Xác nhận hủy'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* =====================================================================
          MODAL OVERRIDE GIÁ (.modal-overlay + .modal)
          PUT /bookings/:id/override-price with { newPrice, reason }
          ===================================================================== */}
      {overrideModalOpen && overrideBooking && (
        <div
          className="modal-overlay"
          style={{
            position: 'fixed',
            top: 0,
            left: 0,
            right: 0,
            bottom: 0,
            backgroundColor: 'rgba(0, 0, 0, 0.55)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            zIndex: 1000,
            padding: '16px',
          }}
          onClick={closeOverrideModal}
        >
          <div
            className="modal"
            style={{
              backgroundColor: '#ffffff',
              borderRadius: '12px',
              maxWidth: '480px',
              width: '100%',
              padding: '24px',
              boxShadow: '0 20px 25px -5px rgba(0, 0, 0, 0.1), 0 10px 10px -5px rgba(0, 0, 0, 0.04)',
            }}
            onClick={(e) => e.stopPropagation()}
          >
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '16px' }}>
              <h3 style={{ margin: 0, fontSize: '1.25rem', color: '#111827' }}>
                Điều Chỉnh Giá Đơn #{overrideBooking.id}
              </h3>
              <button
                type="button"
                onClick={closeOverrideModal}
                style={{ fontSize: '1.5rem', lineHeight: 1, color: '#9ca3af', cursor: 'pointer' }}
              >
                ×
              </button>
            </div>

            <div
              style={{
                backgroundColor: '#eff6ff',
                border: '1px solid #dbeafe',
                borderRadius: '8px',
                padding: '12px',
                marginBottom: '16px',
                fontSize: '0.875rem',
                color: '#1e40af',
              }}
            >
              <p style={{ margin: '0 0 4px 0' }}>
                <strong>Khách hàng:</strong> {overrideBooking.customerName || 'N/A'}
              </p>
              <p style={{ margin: '0 0 4px 0' }}>
                <strong>Xe:</strong> {overrideBooking.carBrand} {overrideBooking.carModel} ({overrideBooking.licensePlate})
              </p>
              <p style={{ margin: 0 }}>
                <strong>Giá hiện tại:</strong>{' '}
                <span style={{ fontWeight: '700', color: '#047857' }}>
                  {renderPrice(overrideBooking.finalPrice)}
                </span>
              </p>
            </div>

            {modalError && (
              <div
                style={{
                  backgroundColor: '#fee2e2',
                  color: '#991b1b',
                  padding: '8px 12px',
                  borderRadius: '6px',
                  fontSize: '0.85rem',
                  marginBottom: '12px',
                }}
              >
                {modalError}
              </div>
            )}

            <form onSubmit={handleConfirmOverridePrice}>
              <div style={{ marginBottom: '14px' }}>
                <label
                  htmlFor="newPriceInput"
                  style={{ display: 'block', marginBottom: '6px', fontSize: '0.875rem', fontWeight: '500', color: '#374151' }}
                >
                  Giá mới (VNĐ) <span style={{ color: '#dc2626' }}>*</span>
                </label>
                <input
                  id="newPriceInput"
                  type="number"
                  min="0"
                  step="1000"
                  required
                  placeholder="Nhập mức giá mới..."
                  value={newPrice}
                  onChange={(e) => setNewPrice(e.target.value)}
                  style={{
                    width: '100%',
                    padding: '10px 12px',
                    borderRadius: '6px',
                    border: '1px solid #d1d5db',
                    fontSize: '0.9rem',
                    outline: 'none',
                    boxSizing: 'border-box',
                  }}
                />
              </div>

              <div style={{ marginBottom: '16px' }}>
                <label
                  htmlFor="overrideReason"
                  style={{ display: 'block', marginBottom: '6px', fontSize: '0.875rem', fontWeight: '500', color: '#374151' }}
                >
                  Lý do điều chỉnh giá <span style={{ color: '#dc2626' }}>*</span>
                </label>
                <textarea
                  id="overrideReason"
                  rows={3}
                  required
                  placeholder="Nhập lý do override giá (ví dụ: Áp dụng ưu đãi đặc biệt cho khách quen, hỗ trợ sự cố xe...)"
                  value={overrideReason}
                  onChange={(e) => setOverrideReason(e.target.value)}
                  style={{
                    width: '100%',
                    padding: '10px 12px',
                    borderRadius: '6px',
                    border: '1px solid #d1d5db',
                    fontSize: '0.9rem',
                    outline: 'none',
                    resize: 'vertical',
                    boxSizing: 'border-box',
                  }}
                />
              </div>

              <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '10px' }}>
                <button
                  type="button"
                  onClick={closeOverrideModal}
                  disabled={actionLoading}
                  style={{
                    padding: '8px 16px',
                    backgroundColor: '#e5e7eb',
                    color: '#374151',
                    borderRadius: '6px',
                    fontWeight: '500',
                    fontSize: '0.875rem',
                    cursor: 'pointer',
                  }}
                >
                  Đóng
                </button>
                <button
                  type="submit"
                  disabled={actionLoading}
                  style={{
                    padding: '8px 18px',
                    backgroundColor: '#2563eb',
                    color: '#ffffff',
                    borderRadius: '6px',
                    fontWeight: '600',
                    fontSize: '0.875rem',
                    cursor: actionLoading ? 'not-allowed' : 'pointer',
                  }}
                >
                  {actionLoading ? 'Đang xử lý...' : 'Xác nhận điều chỉnh'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};

export default AdminBookingsPage;
