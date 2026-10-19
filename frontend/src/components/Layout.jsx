import React from 'react';
import { Outlet } from 'react-router-dom';
import Navbar from './Navbar';
import ChatWidget from './ChatWidget';

/**
 * Layout chung cho toàn bộ ứng dụng Car Rental AI.
 * Bao gồm: Thanh điều hướng (Navbar), nội dung chính (Outlet),
 * chân trang (Footer), và Trợ lý AI (ChatWidget).
 */
const Layout = () => {
  return (
    <div className="layout">
      <Navbar />
      <main className="main-content">
        <Outlet />
      </main>
      <footer className="footer">© 2025 Car Rental AI</footer>
      <ChatWidget />
    </div>
  );
};

export default Layout;
