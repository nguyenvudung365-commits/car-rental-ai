import React from 'react';
import { formatCurrency } from '../utils/formatCurrency';

/** Chi tiết giá: tiền thuê, các dòng phụ phí/giảm giá/tài xế/giao nhận, rồi tổng. Dòng giảm giá là số âm. */
export default function PriceBreakdown({ rentalAmount, charges, total, rentalLabel = 'Tiền thuê xe' }) {
  const row = { display: 'flex', justifyContent: 'space-between', marginTop: 6 };
  return (
    <div>
      <div style={row}><span>{rentalLabel}</span><span>{formatCurrency(rentalAmount)}</span></div>
      {(charges || []).map((c, i) => (
        <div key={i} style={{ ...row, color: c.Amount < 0 ? '#15803d' : undefined }}>
          <span>{c.Name}</span>
          <span>{c.Amount < 0 ? '-' : '+'}{formatCurrency(Math.abs(c.Amount))}</span>
        </div>
      ))}
      <div style={{ ...row, fontWeight: 700, borderTop: '1px solid #e5e7eb', paddingTop: 6, marginTop: 8 }}>
        <span>Tổng</span><span>{formatCurrency(total)}</span>
      </div>
    </div>
  );
}
