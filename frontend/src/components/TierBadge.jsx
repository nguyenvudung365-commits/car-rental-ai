import React from 'react';

export const TIERS = {
  1: { label: 'Thành viên', bg: '#f3f4f6', color: '#374151' },
  2: { label: 'Bạc', bg: '#e5e7eb', color: '#4b5563' },
  3: { label: 'Vàng', bg: '#fef3c7', color: '#92400e' },
  4: { label: 'Kim cương', bg: '#dbeafe', color: '#1e40af' },
};

export const DOC_STATUS = { 1: 'Chưa nộp', 2: 'Chờ duyệt', 3: 'Bị từ chối', 4: 'Đã xác minh' };

const TierBadge = ({ tier }) => {
  const t = TIERS[tier] || TIERS[1];
  return (
    <span style={{ background: t.bg, color: t.color, padding: '2px 8px', borderRadius: 999, fontSize: '0.8rem', fontWeight: 600 }}>
      {t.label}
    </span>
  );
};

export default TierBadge;
