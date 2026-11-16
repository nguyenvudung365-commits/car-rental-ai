import React, { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import useAuth from '../auth/AuthContext';
import {
  validateFullName,
  validateEmail,
  validatePhone,
  validatePassword,
  validateConfirmPassword,
  parseApiError,
} from '../utils/validators';

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
  const [fieldErrors, setFieldErrors] = useState({});
  const [loading, setLoading] = useState(false);

  /**
   * Xử lý gửi biểu mẫu đăng ký
   * API: POST /auth/register -> { fullName, email, phone, password }
   */
  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');

    // Kiểm tra tính hợp lệ của dữ liệu đầu vào
    const errors = {
      fullName: validateFullName(fullName),
      email: validateEmail(email),
      phone: validatePhone(phone),
      password: validatePassword(password),
      confirmPassword: validateConfirmPassword(password, confirmPassword),
    };
    setFieldErrors(errors);
    if (Object.values(errors).some(Boolean)) return;

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
      const { code, message } = parseApiError(err);
      if (code === 'EMAIL_EXISTS') {
        setFieldErrors({ email: 'Email này đã được đăng ký. Hãy dùng email khác hoặc đăng nhập.' });
      } else if (code === 'PHONE_EXISTS') {
        setFieldErrors({ phone: 'Số điện thoại này đã được đăng ký.' });
      } else if (!err.response) {
        setError('Không kết nối được máy chủ. Vui lòng thử lại sau.');
      } else {
        setError(message || 'Đăng ký không thành công. Vui lòng thử lại với thông tin khác.');
      }
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
              className={`form-input ${fieldErrors.fullName ? 'invalid' : ''}`}
              placeholder="Nguyễn Văn A"
              value={fullName}
              onChange={(e) => { setFullName(e.target.value); setFieldErrors((f) => ({ ...f, fullName: '' })); }}
              onBlur={() => setFieldErrors((f) => ({ ...f, fullName: validateFullName(fullName) }))}
              disabled={loading}
              autoComplete="name"
              required
            />
            {fieldErrors.fullName && <span className="field-error" role="alert">{fieldErrors.fullName}</span>}
          </div>

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
            <label className="form-label" htmlFor="phone">
              Số điện thoại <span style={{ color: '#dc2626' }}>*</span>
            </label>
            <input
              id="phone"
              type="tel"
              className={`form-input ${fieldErrors.phone ? 'invalid' : ''}`}
              placeholder="0912345678"
              value={phone}
              onChange={(e) => { setPhone(e.target.value); setFieldErrors((f) => ({ ...f, phone: '' })); }}
              onBlur={() => setFieldErrors((f) => ({ ...f, phone: validatePhone(phone) }))}
              disabled={loading}
              autoComplete="tel"
              required
            />
            {fieldErrors.phone && <span className="field-error" role="alert">{fieldErrors.phone}</span>}
          </div>

          <div className="form-group">
            <label className="form-label" htmlFor="password">
              Mật khẩu <span style={{ color: '#dc2626' }}>*</span>
            </label>
            <input
              id="password"
              type="password"
              className={`form-input ${fieldErrors.password ? 'invalid' : ''}`}
              placeholder="Ít nhất 8 ký tự"
              value={password}
              onChange={(e) => { setPassword(e.target.value); setFieldErrors((f) => ({ ...f, password: '' })); }}
              onBlur={() => setFieldErrors((f) => ({ ...f, password: validatePassword(password) }))}
              disabled={loading}
              autoComplete="new-password"
              required
            />
            {fieldErrors.password && <span className="field-error" role="alert">{fieldErrors.password}</span>}
          </div>

          <div className="form-group">
            <label className="form-label" htmlFor="confirmPassword">
              Xác nhận mật khẩu <span style={{ color: '#dc2626' }}>*</span>
            </label>
            <input
              id="confirmPassword"
              type="password"
              className={`form-input ${fieldErrors.confirmPassword ? 'invalid' : ''}`}
              placeholder="Nhập lại mật khẩu"
              value={confirmPassword}
              onChange={(e) => { setConfirmPassword(e.target.value); setFieldErrors((f) => ({ ...f, confirmPassword: '' })); }}
              onBlur={() => setFieldErrors((f) => ({ ...f, confirmPassword: validateConfirmPassword(password, confirmPassword) }))}
              disabled={loading}
              autoComplete="new-password"
              required
            />
            {fieldErrors.confirmPassword && <span className="field-error" role="alert">{fieldErrors.confirmPassword}</span>}
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
