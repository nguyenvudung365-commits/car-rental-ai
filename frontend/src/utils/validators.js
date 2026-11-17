// Các hàm kiểm tra dữ liệu nhập; trả về chuỗi lỗi hoặc '' nếu hợp lệ.

const EMAIL_RE = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
const PHONE_RE = /^(0|\+84)\d{9}$/;

export const validateEmail = (value) => {
  const v = value.trim();
  if (!v) return 'Vui lòng nhập địa chỉ email.';
  if (!EMAIL_RE.test(v)) return 'Email không đúng định dạng (ví dụ: name@example.com).';
  return '';
};

export const validatePhone = (value) => {
  const v = value.trim().replace(/[\s.-]/g, '');
  if (!v) return 'Vui lòng nhập số điện thoại.';
  if (!PHONE_RE.test(v)) return 'Số điện thoại phải gồm 10 chữ số, bắt đầu bằng 0 (hoặc +84).';
  return '';
};

export const validateFullName = (value) => {
  const v = value.trim();
  if (!v) return 'Vui lòng nhập họ và tên.';
  if (v.length < 2) return 'Họ và tên quá ngắn.';
  return '';
};

export const validatePassword = (value) => {
  if (!value) return 'Vui lòng nhập mật khẩu.';
  if (value.length < 8) return 'Mật khẩu phải có ít nhất 8 ký tự.';
  return '';
};

export const validateConfirmPassword = (password, confirm) => {
  if (!confirm) return 'Vui lòng nhập lại mật khẩu.';
  if (password !== confirm) return 'Mật khẩu xác nhận không khớp.';
  return '';
};

// Lấy thông báo + mã lỗi từ phản hồi của API ({ code, message })
export const parseApiError = (err) => {
  const data = err.response?.data;
  return {
    status: err.response?.status,
    code: data?.code,
    message:
      data?.message ||
      data?.title ||
      (typeof data === 'string' ? data : '') ||
      '',
  };
};
