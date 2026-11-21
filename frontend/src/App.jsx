import React from 'react';
import './App.css';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider } from './auth/AuthContext';
import Layout from './components/Layout';
import PrivateRoute from './components/PrivateRoute';

// Các trang Khách hàng & Công khai
import HomePage from './pages/HomePage';
import LoginPage from './pages/LoginPage';
import RegisterPage from './pages/RegisterPage';
import CarListPage from './pages/CarListPage';
import CarDetailPage from './pages/CarDetailPage';
import BookingPage from './pages/BookingPage';
import MyBookingsPage from './pages/MyBookingsPage';
import ProfilePage from './pages/ProfilePage';

// Các trang Quản trị viên (Admin)
import { adminRoutes } from './routes/adminRoutes';

/**
 * Component định tuyến chính (App Router) của hệ thống Thuê Xe Car Rental AI.
 * Quản lý phiên làm việc AuthContext, chia nhóm định tuyến Public, Customer và Admin.
 */
function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          <Route element={<Layout />}>
            {/* Public Routes - Khách vãng lai và mọi người dùng */}
            <Route path="/" element={<HomePage />} />
            <Route path="/cars" element={<CarListPage />} />
            <Route path="/cars/:id" element={<CarDetailPage />} />
            <Route path="/login" element={<LoginPage />} />
            <Route path="/register" element={<RegisterPage />} />

            {/* Customer Routes - Yêu cầu đăng nhập tài khoản Customer hoặc Admin */}
            <Route element={<PrivateRoute roles={['Customer', 'Staff', 'Admin']} />}>
              <Route path="/booking/:carId" element={<BookingPage />} />
              <Route path="/my-bookings" element={<MyBookingsPage />} />
              <Route path="/profile" element={<ProfilePage />} />
            </Route>

            {/* Admin Routes - Chỉ dành riêng cho quyền quản trị viên Admin */}
            {[['Staff', 'Admin'], ['Admin']].map((roles) => (
              <Route key={roles.join()} element={<PrivateRoute roles={roles} />}>
                {adminRoutes
                  .filter((r) => r.roles.join() === roles.join())
                  .map((r) => <Route key={r.path} path={r.path} element={r.element} />)}
              </Route>
            ))}

            {/* Bắt tất cả các đường dẫn không hợp lệ -> Chuyển hướng về danh sách xe */}
            <Route path="*" element={<Navigate to="/" replace />} />
          </Route>
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  );
}

export default App;
