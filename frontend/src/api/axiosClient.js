import axios from 'axios';

/**
 * Cấu hình Axios Client giao tiếp với Backend Car Rental API
 * Base URL: /api (được chuyển tiếp proxy đến https://localhost:5001 qua vite.config.js)
 */
const axiosClient = axios.create({
  baseURL: '/api',
  headers: { 'Content-Type': 'application/json' },
});

// Request interceptor: tự động đính kèm Bearer token vào Header mỗi khi gửi yêu cầu
axiosClient.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('token');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// Response interceptor: tự động xử lý khi phiên đăng nhập hết hạn hoặc không hợp lệ (mã lỗi 401)
axiosClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      // Xóa thông tin đăng nhập và điều hướng người dùng về trang đăng nhập
      localStorage.removeItem('token');
      localStorage.removeItem('user');
      window.location.href = '/login';
    }
    return Promise.reject(error);
  }
);

export default axiosClient;
