// API trả ngày giờ không kèm múi giờ; đơn theo giờ lưu UTC nên thêm 'Z' khi parse.
export const parseUtc = (s) => new Date(/Z|[+-]\d\d:\d\d$/.test(String(s)) ? s : `${s}Z`);
export const formatLocalDateTime = (s) =>
  parseUtc(s).toLocaleString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' });
