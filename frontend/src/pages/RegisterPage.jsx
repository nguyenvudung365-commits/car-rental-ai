import React, { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import useAuth from '../auth/AuthContext';

/**
 * Trang Đăng ký (RegisterPage)
 * Cho phép người dùng đăng ký tài khoản khách hàng mới vào hệ thống
 */
const RegisterPage = () => {
  const navigate = useNavigate();
  const auth = useAuth();
  const register = auth?.register;

  const [fullName, setFullName] = useState('');
  const [email, setEmail] = useState('');
  const [phone, setPhone] = useState('');
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  /**
   * Xử lý gửi biểu mẫu đăng ký
   * API: POST /auth/register -> { fullName, email, phone, password }
   */
  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');

    // Kiểm tra tính hợp lệ của dữ liệu đầu vào
    if (!fullName.trim()) {
      setError('Vui lòng nhập họ và tên.');
      return;
    }
    if (!email.trim()) {
      setError('Vui lòng nhập địa chỉ email.');
      return;
    }
    if (!phone.trim()) {
      setError('Vui lòng nhập số điện thoại.');
      return;
    }
    if (!password) {
      setError('Vui lòng nhập mật khẩu.');
      return;
    }
    if (password.length < 8) {
      setError('Mật khẩu phải có ít nhất 8 ký tự.');
      return;
    }
    if (password !== confirmPassword) {
      setError('Mật khẩu xác nhận không khớp với mật khẩu.');
      return;
    }

    try {
      setLoading(true);
      // Gọi API đăng ký qua AuthContext
      await register(fullName.trim(), email.trim(), phone.trim(), password);

      // Đăng ký thành công -> chuyển hướng về trang đăng nhập với thông báo
      navigate('/login', {
        state: { message: 'Đăng ký tài khoản thành công! Vui lòng đăng nhập để tiếp tục.' },
      });
    } catch (err) {
      console.error('Lỗi khi đăng ký tài khoản:', err);
      // Trích xuất lỗi trả về từ API backend
      const serverMessage =
        err.response?.data?.message ||
        err.response?.data?.title ||
        (typeof err.response?.data === 'string' ? err.response?.data : null);

      setError(serverMessage || 'Đăng ký không thành công. Vui lòng thử lại với thông tin khác.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="page-container" style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', minHeight: 'calc(80vh - 64px)' }}>
      <div className="form-container">
        <h1 className="page-title text-center" style={{ marginBottom: '8px' }}>Đăng ký tài khoản</h1>
        <p className="text-center text-muted" style={{ marginBottom: '24px', fontSize: '0.95rem' }}>
          Tạo tài khoản để trải nghiệm đặt xe thông minh và nhanh chóng
        </p>

        {/* Thông báo lỗi */}
        {error && (
          <div
            className="form-error"
            style={{
              padding: '12px 16px',
              backgroundColor: '#fee2e2',
              border: '1px solid #fca5a5',
              borderRadius: '8px',
              color: '#b91c1c',
              fontSize: '0.9rem',
              marginBottom: '16px',
            }}
          >
            {error}
          </div>
        )}

        <form onSubmit={handleSubmit} noValidate>
          <div className="form-group">
            <label className="form-label" htmlFor="fullName">
              Họ và tên <span style={{ color: '#dc2626' }}>*</span>
            </label>
            <input
              id="fullName"
              type="text"
              className="form-input"
              placeholder="Nguyễn Văn A"
              value={fullName}
              onChange={(e) => setFullName(e.target.value)}
              disabled={loading}
              autoComplete="name"
              required
            />
          </div>

          <div className="form-group">
            <label className="form-label" htmlFor="email">
              Địa chỉ Email <span style={{ color: '#dc2626' }}>*</span>
            </label>
            <input
              id="email"
              type="email"
              className="form-input"
              placeholder="name@example.com"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              disabled={loading}
              autoComplete="email"
              required
            />
          </div>

          <div className="form-group">
            <label className="form-label" htmlFor="phone">
              Số điện thoại <span style={{ color: '#dc2626' }}>*</span>
            </label>
            <input
              id="phone"
              type="tel"
              className="form-input"
              placeholder="0912345678"
              value={phone}
              onChange={(e) => setPhone(e.target.value)}
              disabled={loading}
              autoComplete="tel"
              required
            />
          </div>

          <div className="form-group">
            <label className="form-label" htmlFor="password">
              Mật khẩu <span style={{ color: '#dc2626' }}>*</span>
            </label>
            <input
              id="password"
              type="password"
              className="form-input"
              placeholder="Ít nhất 8 ký tự"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              disabled={loading}
              autoComplete="new-password"
              required
            />
          </div>

          <div className="form-group">
            <label className="form-label" htmlFor="confirmPassword">
              Xác nhận mật khẩu <span style={{ color: '#dc2626' }}>*</span>
            </label>
            <input
              id="confirmPassword"
              type="password"
              className="form-input"
              placeholder="Nhập lại mật khẩu"
              value={confirmPassword}
              onChange={(e) => setConfirmPassword(e.target.value)}
              disabled={loading}
              autoComplete="new-password"
              required
            />
          </div>

          <button
            type="submit"
            className="btn btn-primary w-full"
            disabled={loading}
            style={{ marginTop: '12px', padding: '12px', fontSize: '1rem' }}
          >
            {loading ? 'Đang tạo tài khoản...' : 'Đăng ký'}
          </button>
        </form>

        <div className="text-center" style={{ marginTop: '24px', fontSize: '0.9rem', color: '#6b7280' }}>
          Đã có tài khoản?{' '}
          <Link
            to="/login"
            style={{ color: 'var(--primary-color, #2563eb)', fontWeight: '600', textDecoration: 'none' }}
          >
            Đăng nhập ngay
          </Link>
        </div>
      </div>
    </div>
  );
};

export default RegisterPage;
