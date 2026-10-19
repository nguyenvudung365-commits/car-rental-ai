import React from 'react';
import './App.css';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider } from './auth/AuthContext';
import Layout from './components/Layout';
import PrivateRoute from './components/PrivateRoute';

// Các trang Khách hàng & Công khai
import LoginPage from './pages/LoginPage';
import RegisterPage from './pages/RegisterPage';
import CarListPage from './pages/CarListPage';
import CarDetailPage from './pages/CarDetailPage';
import BookingPage from './pages/BookingPage';
import MyBookingsPage from './pages/MyBookingsPage';

// Các trang Quản trị viên (Admin)
import AdminBookingsPage from './pages/admin/AdminBookingsPage';
import AdminReturnPage from './pages/admin/AdminReturnPage';
import AdminCarImagesPage from './pages/admin/AdminCarImagesPage';
import AdminReportsPage from './pages/admin/AdminReportsPage';

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
            <Route path="/" element={<Navigate to="/cars" replace />} />
            <Route path="/cars" element={<CarListPage />} />
            <Route path="/cars/:id" element={<CarDetailPage />} />
            <Route path="/login" element={<LoginPage />} />
            <Route path="/register" element={<RegisterPage />} />

            {/* Customer Routes - Yêu cầu đăng nhập tài khoản Customer hoặc Admin */}
            <Route element={<PrivateRoute roles={['Customer', 'Admin']} />}>
              <Route path="/booking/:carId" element={<BookingPage />} />
              <Route path="/my-bookings" element={<MyBookingsPage />} />
            </Route>

            {/* Admin Routes - Chỉ dành riêng cho quyền quản trị viên Admin */}
            <Route element={<PrivateRoute roles={['Admin']} />}>
              <Route path="/admin/bookings" element={<AdminBookingsPage />} />
              <Route path="/admin/returns" element={<AdminReturnPage />} />
              <Route path="/admin/car-images" element={<AdminCarImagesPage />} />
              <Route path="/admin/reports" element={<AdminReportsPage />} />
            </Route>

            {/* Bắt tất cả các đường dẫn không hợp lệ -> Chuyển hướng về danh sách xe */}
            <Route path="*" element={<Navigate to="/cars" replace />} />
          </Route>
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  );
}

export default App;
