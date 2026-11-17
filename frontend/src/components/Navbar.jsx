import React, { useState } from 'react';
import { Link, useNavigate, useLocation } from 'react-router-dom';
import { FiMenu, FiX, FiTruck } from 'react-icons/fi';
import useAuth from '../auth/AuthContext';
import { roleLabel } from '../utils/roleLabels';

/**
 * Navbar - Thanh điều hướng chính của ứng dụng
 * Hiển thị logo, menu theo vai trò (Khách hàng / Quản trị viên) và trạng thái đăng nhập
 */
const Navbar = () => {
  const auth = useAuth?.() || {};
  const { user, token, logout } = auth;
  const navigate = useNavigate();
  const location = useLocation();

  const [menuOpen, setMenuOpen] = useState(false);

  // Xác thực và phân quyền người dùng
  const role = user?.role || auth.role;
  const fullName = user?.fullName || auth.fullName || user?.name || user?.email || 'Tài khoản';
  const isAuthenticated = Boolean(auth.isAuthenticated ?? (user || token));
  const isStaff = role === 'Staff';
  const isManager = role === 'Admin' || isStaff;
  const isAdmin = role === 'Admin';

  // Xử lý đăng xuất người dùng
  const handleLogout = async () => {
    try {
      if (typeof logout === 'function') {
        await logout();
      }
    } catch (err) {
      console.error('Lỗi khi đăng xuất:', err);
    } finally {
      setMenuOpen(false);
      navigate('/login');
    }
  };

  const closeMenu = () => setMenuOpen(false);

  const isActive = (path) => location.pathname === path;

  return (
    <nav className="navbar">
      <div className="navbar-container">
        {/* Logo */}
        <Link to="/" className="navbar-logo" onClick={closeMenu}>
          <span className="logo-icon" aria-hidden="true"><FiTruck /></span>
          <span className="logo-text">Car Rental AI</span>
        </Link>

        {/* Nút Hamburger trên màn hình di động */}
        <button
          type="button"
          className="navbar-hamburger"
          aria-label="Mở menu điều hướng"
          onClick={() => setMenuOpen(!menuOpen)}
        >
          {menuOpen ? <FiX size={24} /> : <FiMenu size={24} />}
        </button>

        {/* Menu điều hướng chính trên Desktop */}
        <div className={`navbar-menu ${menuOpen ? 'is-active' : ''}`}>
          <div className="navbar-links">
            <Link
              to="/"
              className={`navbar-link ${isActive('/') ? 'active' : ''}`}
              onClick={closeMenu}
            >
              Trang chủ
            </Link>
            <Link
              to="/cars"
              className={`navbar-link ${isActive('/cars') ? 'active' : ''}`}
              onClick={closeMenu}
            >
              Danh sách xe
            </Link>

            {/* Lịch sử thuê: mọi tài khoản đã đăng nhập */}
            {isAuthenticated && (
              <Link
                to="/my-bookings"
                className={`navbar-link ${isActive('/my-bookings') ? 'active' : ''}`}
                onClick={closeMenu}
              >
                Lịch sử thuê
              </Link>
            )}

            {/* Menu quản lý: Admin và Staff */}
            {isAuthenticated && isManager && (
              <>
                <Link
                  to="/admin/bookings"
                  className={`navbar-link ${isActive('/admin/bookings') ? 'active' : ''}`}
                  onClick={closeMenu}
                >
                  Quản lý đơn
                </Link>
                <Link
                  to="/admin/returns"
                  className={`navbar-link ${isActive('/admin/returns') ? 'active' : ''}`}
                  onClick={closeMenu}
                >
                  Trả xe
                </Link>
                <Link
                  to="/admin/cars"
                  className={`navbar-link ${isActive('/admin/cars') ? 'active' : ''}`}
                  onClick={closeMenu}
                >
                  Quản lý xe
                </Link>
                {isAdmin && (
                  <Link
                    to="/admin/reports"
                    className={`navbar-link ${isActive('/admin/reports') ? 'active' : ''}`}
                    onClick={closeMenu}
                  >
                    Báo cáo
                  </Link>
                )}
              </>
            )}
          </div>

          {/* Khu vực trạng thái tài khoản phía bên phải */}
          <div className="navbar-auth">
            {isAuthenticated ? (
              <div className="auth-user-info">
                <span className="user-name">
                  {fullName}
                  {isManager && <span className="admin-badge">{roleLabel(role)}</span>}
                </span>
                <button
                  type="button"
                  onClick={handleLogout}
                  className="btn btn-logout"
                >
                  Đăng xuất
                </button>
              </div>
            ) : (
              <div className="auth-actions">
                <Link
                  to="/login"
                  className="btn btn-login"
                  onClick={closeMenu}
                >
                  Đăng nhập
                </Link>
                <Link
                  to="/register"
                  className="btn btn-register"
                  onClick={closeMenu}
                >
                  Đăng ký
                </Link>
              </div>
            )}
          </div>
        </div>
      </div>
    </nav>
  );
};

export default Navbar;
