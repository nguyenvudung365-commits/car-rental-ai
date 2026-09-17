import React, { createContext, useContext, useState, useEffect } from 'react';
import axiosClient from '../api/axiosClient';

// Khởi tạo Context cho xác thực người dùng
const AuthContext = createContext(null);

export const AuthProvider = ({ children }) => {
  const [user, setUser] = useState(null);
  const [token, setToken] = useState(null);
  const [loading, setLoading] = useState(true);

  // Khi ứng dụng khởi chạy, đọc token và thông tin user từ localStorage
  useEffect(() => {
    try {
      const storedToken = localStorage.getItem('token');
      const storedUser = localStorage.getItem('user');

      if (storedToken) {
        setToken(storedToken);
      }
      if (storedUser) {
        setUser(JSON.parse(storedUser));
      }
    } catch (error) {
      console.error('Lỗi khi đọc thông tin đăng nhập từ localStorage:', error);
      localStorage.removeItem('token');
      localStorage.removeItem('user');
    } finally {
      setLoading(false);
    }
  }, []);

  /**
   * Đăng nhập người dùng
   * Endpoint: POST /auth/login
   * Body: { email, password }
   * Response: { token, role, fullName }
   */
  const login = async (email, password) => {
    const response = await axiosClient.post('/auth/login', { email, password });
    const { token: receivedToken, role, fullName } = response.data;

    const userData = { fullName, role };

    // Lưu vào state
    setToken(receivedToken);
    setUser(userData);

    // Lưu vào localStorage để duy trì phiên đăng nhập
    localStorage.setItem('token', receivedToken);
    localStorage.setItem('user', JSON.stringify(userData));

    return response.data;
  };

  /**
   * Đăng ký tài khoản mới
   * Endpoint: POST /auth/register
   * Body: { fullName, email, phone, password }
   * Response: 201 { id, email, role }
   */
  const register = async (fullName, email, phone, password) => {
    const response = await axiosClient.post('/auth/register', {
      fullName,
      email,
      phone,
      password,
    });
    return response.data;
  };

  /**
   * Đăng xuất người dùng, xóa bỏ session và token
   */
  const logout = () => {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    setToken(null);
    setUser(null);
  };

  // Các biến tiện ích kiểm tra quyền
  const isAdmin = user?.role === 'Admin';
  const isAuthenticated = !!token;

  const value = {
    user,
    token,
    loading,
    isAdmin,
    isAuthenticated,
    login,
    register,
    logout,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};

// Custom Hook sử dụng thông tin xác thực dễ dàng trong các component
export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth phải được sử dụng bên trong AuthProvider');
  }
  return context;
};

export { AuthContext };
export default useAuth;
