const CAR_TYPE_BY_NUMBER = { 1: 'Sedan', 2: 'SUV', 3: 'Hatchback', 4: 'MPV', 5: 'Pickup', 6: 'Luxury' };
const CAR_STATUS_BY_NUMBER = { 1: 'SanSang', 2: 'DangThue', 3: 'BaoTri', 4: 'NgungHoatDong' };

const CAR_TYPE_LABEL = {
  Sedan: 'Sedan',
  SUV: 'SUV',
  Hatchback: 'Hatchback',
  MPV: 'MPV (Đa dụng)',
  Pickup: 'Pickup (Bán tải)',
  Luxury: 'Luxury (Hạng sang)',
};

const CAR_STATUS_LABEL = {
  SanSang: 'Sẵn sàng',
  DangThue: 'Đang thuê',
  BaoTri: 'Bảo trì',
  NgungHoatDong: 'Ngừng hoạt động',
};

// API có thể trả enum dạng số (1) hoặc chuỗi ("SanSang"); chuẩn hóa về khóa chuỗi.
export const normalizeCarType = (value) => CAR_TYPE_BY_NUMBER[value] ?? value ?? '';
export const normalizeCarStatus = (value) => CAR_STATUS_BY_NUMBER[value] ?? value ?? '';

export const carTypeLabel = (value) => {
  const key = normalizeCarType(value);
  return CAR_TYPE_LABEL[key] ?? (key || '—');
};

export const carStatusLabel = (value) => {
  const key = normalizeCarStatus(value);
  return CAR_STATUS_LABEL[key] ?? (key || 'Chưa rõ');
};
