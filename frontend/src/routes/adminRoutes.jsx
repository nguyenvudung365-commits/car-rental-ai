import React from 'react';
import AdminBookingsPage from '../pages/admin/AdminBookingsPage';
import AdminReturnPage from '../pages/admin/AdminReturnPage';
import AdminCarsPage from '../pages/admin/AdminCarsPage';
import AdminReportsPage from '../pages/admin/AdminReportsPage';
import AdminUsersPage from '../pages/admin/AdminUsersPage';
import AdminAuditLogsPage from '../pages/admin/AdminAuditLogsPage';
import AdminBlacklistPage from '../pages/admin/AdminBlacklistPage';

export const adminRoutes = [
  { path: '/admin/bookings', element: <AdminBookingsPage />, roles: ['Staff', 'Admin'] },
  { path: '/admin/returns', element: <AdminReturnPage />, roles: ['Staff', 'Admin'] },
  { path: '/admin/cars', element: <AdminCarsPage />, roles: ['Staff', 'Admin'] },
  { path: '/admin/blacklist', element: <AdminBlacklistPage />, roles: ['Staff', 'Admin'] },
  { path: '/admin/reports', element: <AdminReportsPage />, roles: ['Admin'] },
  { path: '/admin/users', element: <AdminUsersPage />, roles: ['Admin'] },
  { path: '/admin/audit-logs', element: <AdminAuditLogsPage />, roles: ['Admin'] },
];
