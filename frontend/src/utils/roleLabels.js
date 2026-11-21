// Vai trò trong API: Customer / Staff / Admin -> nhãn hiển thị tiếng Việt.
const ROLE_LABEL = { Customer: 'Khách thuê', Staff: 'Nhân viên', Admin: 'Quản trị viên' };

export const roleLabel = (role) => ROLE_LABEL[role] ?? 'Khách thuê';
