import React from 'react';
import AdminBookingsPage from '../pages/admin/AdminBookingsPage';
import AdminReturnPage from '../pages/admin/AdminReturnPage';
import AdminCarsPage from '../pages/admin/AdminCarsPage';
import AdminReportsPage from '../pages/admin/AdminReportsPage';
import AdminUsersPage from '../pages/admin/AdminUsersPage';
import AdminAuditLogsPage from '../pages/admin/AdminAuditLogsPage';
import AdminBlacklistPage from '../pages/admin/AdminBlacklistPage';
import AdminDocumentsPage from '../pages/admin/AdminDocumentsPage';
import AdminCustomersPage from '../pages/admin/AdminCustomersPage';
import AdminCustomerDetailPage from '../pages/admin/AdminCustomerDetailPage';
import AdminHandoverPage from '../pages/admin/AdminHandoverPage';
import AdminPricingPage from '../pages/admin/AdminPricingPage';

import AdminDashboardPage from '../pages/admin/AdminDashboardPage';
import AdminCalendarPage from '../pages/admin/AdminCalendarPage';
import AdminMaintenancePage from '../pages/admin/AdminMaintenancePage';

export const adminRoutes = [
  { path: '/admin', element: <AdminDashboardPage />, roles: ['Staff', 'Admin'] },
  { path: '/admin/calendar', element: <AdminCalendarPage />, roles: ['Staff', 'Admin'] },
  { path: '/admin/maintenance', element: <AdminMaintenancePage />, roles: ['Staff', 'Admin'] },
  { path: '/admin/bookings', element: <AdminBookingsPage />, roles: ['Staff', 'Admin'] },
  { path: '/admin/bookings/:id/handover', element: <AdminHandoverPage />, roles: ['Staff', 'Admin'] },
  { path: '/admin/returns', element: <AdminReturnPage />, roles: ['Staff', 'Admin'] },
  { path: '/admin/cars', element: <AdminCarsPage />, roles: ['Staff', 'Admin'] },
  { path: '/admin/customers', element: <AdminCustomersPage />, roles: ['Staff', 'Admin'] },
  { path: '/admin/customers/:id', element: <AdminCustomerDetailPage />, roles: ['Staff', 'Admin'] },
  { path: '/admin/documents', element: <AdminDocumentsPage />, roles: ['Staff', 'Admin'] },
  { path: '/admin/blacklist', element: <AdminBlacklistPage />, roles: ['Staff', 'Admin'] },
  { path: '/admin/reports', element: <AdminReportsPage />, roles: ['Admin'] },
  { path: '/admin/pricing', element: <AdminPricingPage />, roles: ['Admin'] },
  { path: '/admin/users', element: <AdminUsersPage />, roles: ['Admin'] },
  { path: '/admin/audit-logs', element: <AdminAuditLogsPage />, roles: ['Admin'] },
];
