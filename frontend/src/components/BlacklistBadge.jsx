import React, { useState, useEffect } from 'react';
import axiosClient from '../api/axiosClient';

// Cache theo customerId (lưu promise để các dòng cùng khách không gọi lặp). Lỗi thì bỏ khỏi cache.
const cache = new Map();
const check = (customerId) => {
  if (!cache.has(customerId)) {
    cache.set(customerId, axiosClient.get('/admin/blacklist/check', { params: { customerId } })
      .then((r) => r.data)
      .catch(() => { cache.delete(customerId); return null; }));
  }
  return cache.get(customerId);
};

/** Huy hiệu cảnh báo/chặn cạnh tên khách (GET /admin/blacklist/check). Lỗi thì không hiển thị gì. */
const BlacklistBadge = ({ customerId }) => {
  const [res, setRes] = useState(null);
  useEffect(() => {
    if (!customerId) return undefined;
    let alive = true;
    check(customerId).then((data) => { if (alive) setRes(data); });
    return () => { alive = false; };
  }, [customerId]);

  const level = res?.Level;
  if (!level) return null;
  const block = level === 2;
  return (
    <span
      title={res.Reason || ''}
      style={{
        marginLeft: 6, padding: '1px 8px', borderRadius: 999, fontSize: '0.75rem', fontWeight: 600,
        background: block ? '#fee2e2' : '#fef3c7', color: block ? '#991b1b' : '#92400e',
      }}
    >
      {block ? 'Bị chặn' : 'Cảnh báo'}
    </span>
  );
};

export default BlacklistBadge;
