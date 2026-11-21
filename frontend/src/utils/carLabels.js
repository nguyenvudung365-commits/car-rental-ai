const CAR_TYPE_BY_NUMBER = { 1: 'Sedan', 2: 'SUV', 3: 'Hatchback', 4: 'MPV', 5: 'Pickup', 6: 'Luxury' };
const CAR_STATUS_BY_NUMBER = { 1: 'SanSang', 2: 'DangThue', 3: 'BaoTri', 4: 'NgungHoatDong', 5: 'ChoDuyet' };

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
  ChoDuyet: 'Chờ duyệt',
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

// Hộp số / nhiên liệu: API trả enum dạng số (1,2,...); form admin dùng khóa chuỗi.
export const TRANSMISSIONS = { SoSan: 1, SoTuDong: 2 };
export const FUEL_TYPES = { Xang: 1, Dau: 2, Dien: 3, Hybrid: 4 };
const TRANSMISSION_LABEL = { SoSan: 'Số sàn', SoTuDong: 'Số tự động' };
const FUEL_LABEL = { Xang: 'Xăng', Dau: 'Dầu diesel', Dien: 'Điện', Hybrid: 'Hybrid' };
const keyOf = (map, value) => Object.keys(map).find((k) => map[k] === value || k === value) ?? '';

export const normalizeTransmission = (value) => keyOf(TRANSMISSIONS, value);
export const normalizeFuelType = (value) => keyOf(FUEL_TYPES, value);
export const transmissionLabel = (value) => TRANSMISSION_LABEL[normalizeTransmission(value)] ?? '—';
export const fuelLabel = (value) => FUEL_LABEL[normalizeFuelType(value)] ?? '—';
