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
  'customer.view': ['Staff', 'Admin'],
  'customer.manage': ['Admin'],
  'document.review': ['Staff', 'Admin'],
  'report.export': ['Admin'],
  'car.import': ['Staff', 'Admin'],
  'maintenance.manage': ['Staff', 'Admin'],
};

export const can = (role, action) => (ACTIONS[action] || []).includes(role);

// Menu quản lý: Navbar render từ đây; roles lấy cùng nguồn với adminRoutes.
export const NAV_ITEMS = [
  { to: '/admin', label: 'Tổng quan', roles: ['Staff', 'Admin'] },
  { to: '/admin/calendar', label: 'Lịch xe', roles: ['Staff', 'Admin'] },
  { to: '/admin/maintenance', label: 'Bảo trì', roles: ['Staff', 'Admin'] },
  { to: '/admin/bookings', label: 'Quản lý đơn', roles: ['Staff', 'Admin'] },
  { to: '/admin/returns', label: 'Trả xe', roles: ['Staff', 'Admin'] },
  { to: '/admin/cars', label: 'Quản lý xe', roles: ['Staff', 'Admin'] },
  { to: '/admin/customers', label: 'Khách hàng', roles: ['Staff', 'Admin'] },
  { to: '/admin/documents', label: 'Duyệt giấy tờ', roles: ['Staff', 'Admin'] },
  { to: '/admin/blacklist', label: 'Danh sách đen', roles: ['Staff', 'Admin'] },
  { to: '/admin/reports', label: 'Báo cáo', roles: ['Admin'] },
  { to: '/admin/pricing', label: 'Giá & chi nhánh', roles: ['Admin'] },
  { to: '/admin/users', label: 'Người dùng', roles: ['Admin'] },
  { to: '/admin/audit-logs', label: 'Nhật ký', roles: ['Admin'] },
];
