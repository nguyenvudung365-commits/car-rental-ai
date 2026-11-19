// Ma trận quyền phía giao diện (chỉ để ẩn/hiện; backend vẫn là nơi quyết định).
const ACTIONS = {
  'car.create': ['Staff', 'Admin'],
  'car.edit': ['Staff', 'Admin'],
  'car.price': ['Admin'],
  'car.approve': ['Admin'],
  'car.delete': ['Admin'],
  'blacklist.view': ['Staff', 'Admin'],
  'blacklist.manage': ['Admin'],
  'user.manage': ['Admin'],
  'audit.view': ['Admin'],
  'report.view': ['Admin'],
};

export const can = (role, action) => (ACTIONS[action] || []).includes(role);

// Menu quản lý: Navbar render từ đây; roles lấy cùng nguồn với adminRoutes.
export const NAV_ITEMS = [
  { to: '/admin/bookings', label: 'Quản lý đơn', roles: ['Staff', 'Admin'] },
  { to: '/admin/returns', label: 'Trả xe', roles: ['Staff', 'Admin'] },
  { to: '/admin/cars', label: 'Quản lý xe', roles: ['Staff', 'Admin'] },
  { to: '/admin/blacklist', label: 'Danh sách đen', roles: ['Staff', 'Admin'] },
  { to: '/admin/reports', label: 'Báo cáo', roles: ['Admin'] },
  { to: '/admin/users', label: 'Người dùng', roles: ['Admin'] },
  { to: '/admin/audit-logs', label: 'Nhật ký', roles: ['Admin'] },
];
