/**
 * Định dạng số tiền sang chuẩn tiền tệ Việt Nam Đồng (VND)
 * Ví dụ: 1200000 -> "1.200.000 ₫"
 * 
 * @param {number|null|undefined} amount - Số tiền cần định dạng
 * @returns {string} Chuỗi hiển thị tiền tệ
 */
export function formatCurrency(amount) {
  if (amount == null) return '—';
  return new Intl.NumberFormat('vi-VN').format(amount) + ' ₫';
}

export default formatCurrency;
