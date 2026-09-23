import React, { useState, useEffect, useCallback } from 'react';
import axiosClient from '../../api/axiosClient';
import { formatCurrency } from '../../utils/formatCurrency';

/**
 * Trang Xử lý Trả xe (AdminReturnPage)
 * Chức năng:
 * - Tiếp nhận xe khách trả sau khi kết thúc hợp đồng
 * - Chọn đơn từ danh sách đơn đã xác nhận (status=Confirmed) hoặc nhập mã đơn
 * - Ghi nhận ngày trả xe thực tế, tình trạng xe (Good/Damaged), ghi chú
 * - Gửi yêu cầu hoàn tất trả xe: POST /bookings/:bookingId/return
 * - Hiển thị kết quả tính phí trả chậm và tổng thu:
 *   + Số ngày trễ: {lateDays}
 *   + Phí trễ hạn: {lateFee} ₫
 *   + Tổng thu: {totalCharge} ₫
 */
const AdminReturnPage = () => {
  // Trạng thái Form
  const [bookingId, setBookingId] = useState('');
  const [returnDate, setReturnDate] = useState(() => {
    // Mặc định ngày hôm nay định dạng YYYY-MM-DD
    const today = new Date();
    return today.toISOString().split('T')[0];
  });
  const [condition, setCondition] = useState('Good'); // 'Good' hoặc 'Damaged'
  const [notes, setNotes] = useState('');

  // Trạng thái danh sách đơn đã duyệt (Confirmed) để chọn nhanh
  const [confirmedBookings, setConfirmedBookings] = useState([]);
  const [loadingBookings, setLoadingBookings] = useState(false);

  // Trạng thái submit form
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [result, setResult] = useState(null);

  /**
   * Tải danh sách đơn đặt xe đang hoạt động (Confirmed) để hiển thị trong dropdown
   * API Endpoint: GET /bookings
   */
  const fetchConfirmedBookings = useCallback(async () => {
    try {
      setLoadingBookings(true);
      const response = await axiosClient.get('/bookings');
      const allBookings = Array.isArray(response.data)
        ? response.data
        : (response.data?.items || response.data?.data || []);

      // Lọc các đơn thuê có trạng thái 'Confirmed'
      const activeList = allBookings.filter((b) => b.status === 'Confirmed');
      setConfirmedBookings(activeList);
    } catch (err) {
      console.error('Không thể tải danh sách đơn đã xác nhận:', err);
      // Không chặn người dùng nếu API bị lỗi, họ vẫn có thể nhập ID thủ công
    } finally {
      setLoadingBookings(false);
    }
  }, []);

  useEffect(() => {
    fetchConfirmedBookings();
  }, [fetchConfirmedBookings]);

  // Tìm thông tin đơn được chọn hiện tại
  const selectedBookingDetails = confirmedBookings.find(
    (b) => String(b.id) === String(bookingId)
  );

  // Xử lý khi chọn từ dropdown
  const handleSelectBooking = (e) => {
    const val = e.target.value;
    setBookingId(val);
    setError(null);
  };

  /**
   * Xử lý xác nhận trả xe
   * API Endpoint: POST /bookings/:bookingId/return
   * Request Body: { returnDate, condition, notes }
   */
  const handleSubmitReturn = async (e) => {
    e.preventDefault();

    const cleanId = String(bookingId).trim();
    if (!cleanId) {
      setError('Vui lòng chọn hoặc nhập mã đơn thuê xe.');
      return;
    }

    if (!returnDate) {
      setError('Vui lòng chọn ngày trả xe thực tế.');
      return;
    }

    try {
      setLoading(true);
      setError(null);
      setResult(null);

      // Gọi API POST /bookings/:bookingId/return
      const response = await axiosClient.post(`/bookings/${cleanId}/return`, {
        returnDate,
        condition,
        notes: notes.trim(),
      });

      // API trả về { lateDays, lateFee, totalCharge }
      setResult(response.data);

      // Cập nhật lại danh sách các đơn Confirmed (đơn vừa trả sẽ chuyển thành Completed)
      await fetchConfirmedBookings();
    } catch (err) {
      console.error('Lỗi khi xử lý trả xe:', err);
      setError(
        err.response?.data?.message ||
          'Không thể xử lý trả xe. Vui lòng kiểm tra lại mã đơn thuê và thử lại.'
      );
    } finally {
      setLoading(false);
    }
  };

  // Đặt lại form để xử lý đơn khác
  const handleResetForm = () => {
    setBookingId('');
    const today = new Date();
    setReturnDate(today.toISOString().split('T')[0]);
    setCondition('Good');
    setNotes('');
    setResult(null);
    setError(null);
  };

  // Định dạng hiển thị tiền tệ
  const renderPrice = (amount) => {
    if (amount === null || amount === undefined) return '0 ₫';
    try {
      return formatCurrency(amount);
    } catch {
      return Number(amount || 0).toLocaleString('vi-VN') + ' ₫';
    }
  };

  // Định dạng ngày hiển thị
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

  return (
    <div style={{ maxWidth: '840px', margin: '0 auto', padding: '24px 16px' }}>
      {/* Tiêu đề trang */}
      <div style={{ marginBottom: '24px' }}>
        <h1 style={{ fontSize: '1.75rem', fontWeight: '700', color: '#111827', margin: 0 }}>
          Xử Lý Trả Xe & Quyết Toán
        </h1>
        <p style={{ color: '#6b7280', marginTop: '6px', fontSize: '0.95rem' }}>
          Ghi nhận thông tin xe hoàn trả, kiểm tra tình trạng bàn giao, tính toán số ngày trả trễ và các khoản phí phát sinh.
        </p>
      </div>

      {/* Thông báo lỗi nếu có */}
      {error && (
        <div
          style={{
            backgroundColor: '#fef2f2',
            border: '1px solid #fee2e2',
            color: '#b91c1c',
            padding: '12px 16px',
            borderRadius: '8px',
            marginBottom: '20px',
            fontSize: '0.9rem',
          }}
        >
          {error}
        </div>
      )}

      {/* Hiển thị kết quả trả xe thành công */}
      {result && (
        <div
          style={{
            backgroundColor: '#ecfdf5',
            border: '1px solid #a7f3d0',
            borderRadius: '12px',
            padding: '24px',
            marginBottom: '24px',
            boxShadow: '0 4px 6px -1px rgba(0, 0, 0, 0.05)',
          }}
        >
          <div style={{ display: 'flex', alignItems: 'center', gap: '10px', marginBottom: '16px' }}>
            <span
              style={{
                display: 'inline-flex',
                alignItems: 'center',
                justifyContent: 'center',
                width: '32px',
                height: '32px',
                backgroundColor: '#10b981',
                color: '#fff',
                borderRadius: '50%',
                fontWeight: 'bold',
                fontSize: '1.1rem',
              }}
            >
              ✓
            </span>
            <h2 style={{ margin: 0, fontSize: '1.25rem', color: '#065f46' }}>
              Quyết Toán Trả Xe Thành Công!
            </h2>
          </div>

          <p style={{ margin: '0 0 16px 0', color: '#047857', fontSize: '0.925rem' }}>
            Đơn thuê xe <strong>#{bookingId}</strong> đã được hoàn tất thủ tục bàn giao và quyết toán.
          </p>

          <div
            style={{
              display: 'grid',
              gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))',
              gap: '16px',
              backgroundColor: '#ffffff',
              padding: '18px',
              borderRadius: '8px',
              border: '1px solid #d1fae5',
            }}
          >
            <div>
              <div style={{ fontSize: '0.8125rem', color: '#6b7280', textTransform: 'uppercase', fontWeight: '600' }}>
                Số ngày trễ
              </div>
              <div
                style={{
                  fontSize: '1.4rem',
                  fontWeight: '700',
                  color: (result.lateDays || 0) > 0 ? '#dc2626' : '#059669',
                  marginTop: '4px',
                }}
              >
                {result.lateDays || 0} ngày
              </div>
            </div>

            <div>
              <div style={{ fontSize: '0.8125rem', color: '#6b7280', textTransform: 'uppercase', fontWeight: '600' }}>
                Phí trễ hạn
              </div>
              <div
                style={{
                  fontSize: '1.4rem',
                  fontWeight: '700',
                  color: (result.lateFee || 0) > 0 ? '#dc2626' : '#374151',
                  marginTop: '4px',
                }}
              >
                {renderPrice(result.lateFee)}
              </div>
            </div>

            <div>
              <div style={{ fontSize: '0.8125rem', color: '#6b7280', textTransform: 'uppercase', fontWeight: '600' }}>
                Tổng thu quyết toán
              </div>
              <div
                style={{
                  fontSize: '1.4rem',
                  fontWeight: '800',
                  color: '#2563eb',
                  marginTop: '4px',
                }}
              >
                {renderPrice(result.totalCharge)}
              </div>
            </div>
          </div>

          <div style={{ marginTop: '20px', display: 'flex', justifyContent: 'flex-end' }}>
            <button
              type="button"
              onClick={handleResetForm}
              style={{
                padding: '10px 20px',
                backgroundColor: '#059669',
                color: '#ffffff',
                borderRadius: '6px',
                fontWeight: '600',
                fontSize: '0.9rem',
                cursor: 'pointer',
              }}
            >
              Tiếp tục trả xe khác
            </button>
          </div>
        </div>
      )}

      {/* Biểu mẫu trả xe */}
      <div
        style={{
          backgroundColor: '#ffffff',
          borderRadius: '12px',
          padding: '28px',
          boxShadow: '0 1px 3px rgba(0, 0, 0, 0.08)',
        }}
      >
        <form onSubmit={handleSubmitReturn}>
          {/* Lựa chọn hoặc nhập mã đơn */}
          <div style={{ marginBottom: '20px' }}>
            <label
              htmlFor="bookingSelect"
              style={{ display: 'block', marginBottom: '8px', fontSize: '0.9rem', fontWeight: '600', color: '#374151' }}
            >
              Chọn đơn thuê cần trả xe
            </label>
            <select
              id="bookingSelect"
              value={bookingId}
              onChange={handleSelectBooking}
              disabled={loadingBookings || loading}
              style={{
                width: '100%',
                padding: '10px 14px',
                borderRadius: '6px',
                border: '1px solid #d1d5db',
                fontSize: '0.9rem',
                backgroundColor: '#ffffff',
                outline: 'none',
                cursor: 'pointer',
                boxSizing: 'border-box',
              }}
            >
              <option value="">
                {loadingBookings
                  ? '-- Đang tải danh sách đơn đã xác nhận... --'
                  : '-- Chọn đơn từ danh sách đơn đã xác nhận (Confirmed) --'}
              </option>
              {confirmedBookings.map((b) => (
                <option key={b.id} value={b.id}>
                  #{b.id} - {b.customerName || 'Khách'} ({b.customerPhone || 'N/A'}) - {b.carBrand} {b.carModel} [{b.licensePlate}] - Hạn trả: {formatDate(b.endDate)}
                </option>
              ))}
            </select>
            <p style={{ margin: '6px 0 0 0', fontSize: '0.8rem', color: '#6b7280' }}>
              Hoặc nhập trực tiếp mã số đơn thuê bên dưới nếu cần:
            </p>
          </div>

          <div style={{ marginBottom: '20px' }}>
            <label
              htmlFor="bookingIdInput"
              style={{ display: 'block', marginBottom: '8px', fontSize: '0.9rem', fontWeight: '600', color: '#374151' }}
            >
              Mã số đơn thuê (Booking ID) <span style={{ color: '#dc2626' }}>*</span>
            </label>
            <input
              id="bookingIdInput"
              type="text"
              required
              placeholder="Ví dụ: 12"
              value={bookingId}
              onChange={(e) => setBookingId(e.target.value)}
              disabled={loading}
              style={{
                width: '100%',
                padding: '10px 14px',
                borderRadius: '6px',
                border: '1px solid #d1d5db',
                fontSize: '0.9rem',
                outline: 'none',
                boxSizing: 'border-box',
              }}
            />
          </div>

          {/* Thông tin vắn tắt đơn thuê nếu đã chọn */}
          {selectedBookingDetails && (
            <div
              style={{
                backgroundColor: '#f8fafc',
                border: '1px solid #e2e8f0',
                borderRadius: '8px',
                padding: '14px 16px',
                marginBottom: '20px',
                fontSize: '0.875rem',
              }}
            >
              <div style={{ fontWeight: '600', color: '#1e293b', marginBottom: '6px' }}>
                Thông tin đơn #{selectedBookingDetails.id}:
              </div>
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '8px', color: '#475569' }}>
                <div>
                  <strong>Khách hàng:</strong> {selectedBookingDetails.customerName} ({selectedBookingDetails.customerPhone || 'N/A'})
                </div>
                <div>
                  <strong>Xe thuê:</strong> {selectedBookingDetails.carBrand} {selectedBookingDetails.carModel} ({selectedBookingDetails.licensePlate})
                </div>
                <div>
                  <strong>Ngày nhận xe:</strong> {formatDate(selectedBookingDetails.startDate)}
                </div>
                <div>
                  <strong>Hạn trả dự kiến:</strong> {formatDate(selectedBookingDetails.endDate)}
                </div>
                <div>
                  <strong>Giá thuê thỏa thuận:</strong> {renderPrice(selectedBookingDetails.finalPrice)}
                </div>
              </div>
            </div>
          )}

          {/* Ngày trả xe thực tế */}
          <div style={{ marginBottom: '20px' }}>
            <label
              htmlFor="returnDateInput"
              style={{ display: 'block', marginBottom: '8px', fontSize: '0.9rem', fontWeight: '600', color: '#374151' }}
            >
              Ngày trả xe thực tế <span style={{ color: '#dc2626' }}>*</span>
            </label>
            <input
              id="returnDateInput"
              type="date"
              required
              value={returnDate}
              onChange={(e) => setReturnDate(e.target.value)}
              disabled={loading}
              style={{
                width: '100%',
                padding: '10px 14px',
                borderRadius: '6px',
                border: '1px solid #d1d5db',
                fontSize: '0.9rem',
                outline: 'none',
                backgroundColor: '#ffffff',
                boxSizing: 'border-box',
              }}
            />
            <span style={{ fontSize: '0.8rem', color: '#6b7280', marginTop: '4px', display: 'block' }}>
              Hệ thống sẽ dựa vào ngày này so với ngày hẹn trả để tính số ngày trễ (nếu có).
            </span>
          </div>

          {/* Tình trạng xe */}
          <div style={{ marginBottom: '20px' }}>
            <label
              htmlFor="conditionSelect"
              style={{ display: 'block', marginBottom: '8px', fontSize: '0.9rem', fontWeight: '600', color: '#374151' }}
            >
              Tình trạng xe khi bàn giao <span style={{ color: '#dc2626' }}>*</span>
            </label>
            <select
              id="conditionSelect"
              value={condition}
              onChange={(e) => setCondition(e.target.value)}
              disabled={loading}
              style={{
                width: '100%',
                padding: '10px 14px',
                borderRadius: '6px',
                border: '1px solid #d1d5db',
                fontSize: '0.9rem',
                backgroundColor: '#ffffff',
                outline: 'none',
                cursor: 'pointer',
                boxSizing: 'border-box',
              }}
            >
              <option value="Good">Good (Bình thường / Tình trạng tốt)</option>
              <option value="Damaged">Damaged (Hỏng hóc / Có tổn hại / Trầy xước)</option>
            </select>
          </div>

          {/* Ghi chú */}
          <div style={{ marginBottom: '24px' }}>
            <label
              htmlFor="notesTextarea"
              style={{ display: 'block', marginBottom: '8px', fontSize: '0.9rem', fontWeight: '600', color: '#374151' }}
            >
              Ghi chú biên bản bàn giao
            </label>
            <textarea
              id="notesTextarea"
              rows={4}
              placeholder="Ghi chú chi tiết về tình trạng ngoại thất, nội thất xe, mức nhiên liệu lúc trả xe, các chi phí bồi thường hư hại nếu có..."
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              disabled={loading}
              style={{
                width: '100%',
                padding: '10px 14px',
                borderRadius: '6px',
                border: '1px solid #d1d5db',
                fontSize: '0.9rem',
                outline: 'none',
                resize: 'vertical',
                boxSizing: 'border-box',
              }}
            />
          </div>

          {/* Nút gửi form */}
          <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '12px' }}>
            <button
              type="button"
              onClick={handleResetForm}
              disabled={loading}
              style={{
                padding: '10px 18px',
                backgroundColor: '#f3f4f6',
                color: '#374151',
                borderRadius: '6px',
                fontWeight: '500',
                fontSize: '0.9rem',
                cursor: 'pointer',
                border: '1px solid #d1d5db',
              }}
            >
              Làm mới
            </button>
            <button
              type="submit"
              disabled={loading}
              style={{
                padding: '10px 24px',
                backgroundColor: '#2563eb',
                color: '#ffffff',
                borderRadius: '6px',
                fontWeight: '600',
                fontSize: '0.9rem',
                cursor: loading ? 'not-allowed' : 'pointer',
                boxShadow: '0 2px 4px rgba(37, 99, 235, 0.2)',
              }}
            >
              {loading ? 'Đang xử lý quyết toán...' : 'Xác nhận trả xe & Hoàn tất'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default AdminReturnPage;
