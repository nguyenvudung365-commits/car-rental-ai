import React, { useState } from 'react';
import { useNavigate, Link, useLocation } from 'react-router-dom';
import useAuth from '../auth/AuthContext';
import { validateEmail, parseApiError } from '../utils/validators';

/**
 * Trang Đăng nhập (LoginPage)
 * Cho phép khách hàng hoặc quản trị viên đăng nhập vào hệ thống
 */
const LoginPage = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const auth = useAuth();
  // Hỗ trợ cả trường hợp useAuth() trả về hàm login trực tiếp hoặc auth object
  const login = auth?.login;

  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [fieldErrors, setFieldErrors] = useState({});
  const [loading, setLoading] = useState(false);

  // Nhận thông báo chuyển tiếp từ trang khác (ví dụ: đăng ký thành công)
  const successMessage = location.state?.message || '';

  /**
   * Xử lý gửi biểu mẫu đăng nhập
   * API: POST /auth/login -> { email, password }
   */
  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');

    // Kiểm tra tính hợp lệ cơ bản
    const errors = {
      email: validateEmail(email),
      password: password ? '' : 'Vui lòng nhập mật khẩu.',
    };
    setFieldErrors(errors);
    if (errors.email || errors.password) return;

    try {
      setLoading(true);
      // Gọi API đăng nhập qua AuthContext
      const loggedIn = await login(email.trim(), password);
      // Đăng nhập thành công -> trang trước đó; Staff/Admin mặc định vào dashboard
      const role = loggedIn?.Role ?? loggedIn?.role;
      const isManager = role === 'Staff' || role === 'Admin';
      navigate(location.state?.from?.pathname || (isManager ? '/admin' : '/'), { replace: true });
    } catch (err) {
      console.error('Lỗi đăng nhập:', err);
      const { status, message } = parseApiError(err);
      if (status === 401) {
        // Sai email hoặc mật khẩu: báo ngay dưới ô mật khẩu
        setFieldErrors({ password: 'Email hoặc mật khẩu không đúng. Vui lòng thử lại.' });
      } else if (!err.response) {
        setError('Không kết nối được máy chủ. Vui lòng thử lại sau.');
      } else {
        setError(message || 'Đăng nhập không thành công. Vui lòng thử lại.');
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="page-container" style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', minHeight: 'calc(80vh - 64px)' }}>
      <div className="form-container">
        <h1 className="page-title text-center" style={{ marginBottom: '8px' }}>Đăng nhập</h1>
        <p className="text-center text-muted" style={{ marginBottom: '24px', fontSize: '0.95rem' }}>
          Chào mừng quay trở lại hệ thống thuê xe Car Rental AI
        </p>

        {/* Thông báo thành công nếu có chuyển tiếp */}
        {successMessage && !error && (
          <div
            style={{
              padding: '12px 16px',
              backgroundColor: '#ecfdf5',
              border: '1px solid #a7f3d0',
              borderRadius: '8px',
              color: '#065f46',
              fontSize: '0.9rem',
              marginBottom: '16px',
            }}
          >
            {successMessage}
          </div>
        )}

        {/* Thông báo lỗi khi đăng nhập thất bại */}
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
            <label className="form-label" htmlFor="email">
              Địa chỉ Email <span style={{ color: '#dc2626' }}>*</span>
            </label>
            <input
              id="email"
              type="email"
              className={`form-input ${fieldErrors.email ? 'invalid' : ''}`}
              placeholder="name@example.com"
              value={email}
              onChange={(e) => { setEmail(e.target.value); setFieldErrors((f) => ({ ...f, email: '' })); }}
              onBlur={() => setFieldErrors((f) => ({ ...f, email: validateEmail(email) }))}
              disabled={loading}
              autoComplete="email"
              required
            />
            {fieldErrors.email && <span className="field-error" role="alert">{fieldErrors.email}</span>}
          </div>

          <div className="form-group">
            <label className="form-label" htmlFor="password">
              Mật khẩu <span style={{ color: '#dc2626' }}>*</span>
            </label>
            <input
              id="password"
              type="password"
              className={`form-input ${fieldErrors.password ? 'invalid' : ''}`}
              placeholder="••••••••"
              value={password}
              onChange={(e) => { setPassword(e.target.value); setFieldErrors((f) => ({ ...f, password: '' })); }}
              onBlur={() => setFieldErrors((f) => ({ ...f, password: password ? '' : 'Vui lòng nhập mật khẩu.' }))}
              disabled={loading}
              autoComplete="current-password"
              required
            />
            {fieldErrors.password && <span className="field-error" role="alert">{fieldErrors.password}</span>}
          </div>

          <button
            type="submit"
            className="btn btn-primary w-full"
            disabled={loading}
            style={{ marginTop: '8px', padding: '12px', fontSize: '1rem' }}
          >
            {loading ? 'Đang đăng nhập...' : 'Đăng nhập'}
          </button>
        </form>

        <div className="text-center" style={{ marginTop: '24px', fontSize: '0.9rem', color: '#6b7280' }}>
          Chưa có tài khoản?{' '}
          <Link
            to="/register"
            style={{ color: 'var(--primary-color, #2563eb)', fontWeight: '600', textDecoration: 'none' }}
          >
            Đăng ký ngay
          </Link>
        </div>
      </div>
    </div>
  );
};

export default LoginPage;
