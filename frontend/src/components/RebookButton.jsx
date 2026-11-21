import React from 'react';
import { useNavigate } from 'react-router-dom';

// Ngày theo giờ máy khách (toISOString là UTC, lệch ngày ở múi giờ +7 buổi sáng).
const iso = (d) => `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;

/** Đặt lại cùng xe, cùng số ngày thuê, bắt đầu từ ngày mai; khách vẫn xem/sửa lại ở trang đặt xe. */
const RebookButton = ({ booking }) => {
  const navigate = useNavigate();
  const go = () => {
    const start = new Date();
    start.setDate(start.getDate() + 1);
    const end = new Date(start);
    end.setDate(end.getDate() + Math.max(1, booking.RentalDays));
    navigate(`/booking/${booking.CarId}?start=${iso(start)}&end=${iso(end)}`);
  };
  return <button onClick={go} className="btn btn-sm btn-primary">Đặt lại</button>;
};

export default RebookButton;
