import React from 'react';
import { Navigate, Outlet, useLocation } from 'react-router-dom';
import useAuth from '../auth/AuthContext';

/**
 * Route bảo vệ (PrivateRoute)
 * Kiểm tra trạng thái đăng nhập và quyền truy cập (roles) của người dùng:
 * - Chưa đăng nhập -> Chuyển hướng tới trang /login (kèm location để quay lại sau khi đăng nhập)
 * - Đã đăng nhập nhưng không có quyền (role) phù hợp -> Chuyển hướng về trang chủ /
 * - Hợp lệ -> Render các component con thông qua <Outlet />
 *
 * @param {Object} props
 * @param {string[]} [props.roles] Danh sách quyền được phép truy cập (vd: ['Admin', 'Customer'])
 */
const PrivateRoute = ({ roles }) => {
  const auth = useAuth?.() || {};
  const { user, token, loading } = auth;
  const location = useLocation();

  // Đang kiểm tra token hoặc tải thông tin người dùng từ server/storage
  if (loading) {
    return (
      <div className="loading-spinner-container" style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', minHeight: '50vh' }}>
        <div className="loading-spinner" style={{ fontSize: '1rem', color: '#6b7280' }}>
          Đang xác thực thông tin...
        </div>
      </div>
    );
  }

  const role = user?.role || auth.role;
  const isAuthenticated = Boolean(auth.isAuthenticated ?? (user || token));

  // Người dùng chưa đăng nhập -> Chuyển về trang đăng nhập
  if (!isAuthenticated) {
    return <Navigate to="/login" state={{ from: location }} replace />;
  }

  // Nếu route yêu cầu vai trò cụ thể mà người dùng không có -> Chuyển về trang chủ
  if (roles && Array.isArray(roles) && roles.length > 0) {
    if (!role || !roles.includes(role)) {
      return <Navigate to="/" replace />;
    }
  }

  // Người dùng hợp lệ -> Hiển thị các route con
  return <Outlet />;
};

export default PrivateRoute;
