import React, { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import dayjs from 'dayjs';
import axiosClient from '../../api/axiosClient';
import { card, input, apiMessage } from '../../utils/adminUi';
import { parseUtc } from '../../utils/utcTime';

const DAY_W = 34;
const NAME_W = 190;
const COLORS = {
  ChoXacNhan: '#f59e0b', DaXacNhan: '#2563eb', DangThue: '#16a34a', DaTraXe: '#9ca3af',
  KeHoach: '#a78bfa', DangThucHien: '#dc2626', HoanThanh: '#9ca3af',
};
const MAINT_LABEL = { BaoDuong: 'Bảo dưỡng', DangKiem: 'Đăng kiểm', BaoHiem: 'Bảo hiểm' };

/** Lịch xe: hàng là xe, cột là ngày (CSS grid + dayjs). Bấm đơn thuê để mở đơn. */
const AdminCalendarPage = () => {
  const navigate = useNavigate();
  const [start, setStart] = useState(() => dayjs().startOf('day'));
  const [span, setSpan] = useState(14);
  const [branchId, setBranchId] = useState('');
  const [branches, setBranches] = useState([]);
  const [rows, setRows] = useState([]);
  const [error, setError] = useState(null);

  useEffect(() => { axiosClient.get('/branches').then((r) => setBranches(r.data || [])).catch(() => setBranches([])); }, []);

  const load = useCallback(async () => {
    try {
      const res = await axiosClient.get('/cars/calendar', {
        params: { from: start.toDate().toISOString(), to: start.add(span, 'day').toDate().toISOString(), branchId: branchId || undefined },
      });
      setRows(res.data || []);
      setError(null);
    } catch (err) {
      setError(apiMessage(err, 'Không tải được lịch xe.'));
    }
  }, [start, span, branchId]);
  useEffect(() => { load(); }, [load]);

  const days = Array.from({ length: span }, (_, i) => start.add(i, 'day'));
  const place = (it) => {
    // Đơn theo ngày là ngày thuần (giờ cục bộ); đơn theo giờ và bảo trì là UTC.
    const pure = (v) => /T00:00:00$/.test(v); // nửa đêm tròn = ngày thuần, không đổi múi giờ
    const t = (v) => dayjs(pure(it.Start) && pure(it.End) ? v : parseUtc(v));
    const s = t(it.Start);
    const e = t(it.End);
    const from = Math.max(0, s.diff(start, 'hour', true) / 24);
    const to = Math.min(span, e.diff(start, 'hour', true) / 24);
    return { left: from * DAY_W, width: Math.max(6, (to - from) * DAY_W - 2) };
  };

  return (
    <div style={{ maxWidth: 1400, margin: '0 auto', padding: '24px 16px' }}>
      <h1 style={{ fontSize: '1.75rem', fontWeight: 700, margin: 0 }}>Lịch xe</h1>
      <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap', margin: '16px 0' }}>
        <button type="button" className="btn" onClick={() => setStart(start.subtract(span === 7 ? 7 : 14, 'day'))}>‹ Trước</button>
        <button type="button" className="btn" onClick={() => setStart(dayjs().startOf('day'))}>Hôm nay</button>
        <button type="button" className="btn" onClick={() => setStart(start.add(span === 7 ? 7 : 14, 'day'))}>Sau ›</button>
        <select style={input} value={span} onChange={(e) => setSpan(Number(e.target.value))}>
          <option value={7}>Tuần (7 ngày)</option>
          <option value={14}>2 tuần</option>
          <option value={30}>Tháng (30 ngày)</option>
        </select>
        <select style={input} value={branchId} onChange={(e) => setBranchId(e.target.value)}>
          <option value="">Tất cả chi nhánh</option>
          {branches.map((b) => <option key={b.Id} value={b.Id}>{b.Name}</option>)}
        </select>
      </div>
      <div style={{ display: 'flex', gap: 12, flexWrap: 'wrap', fontSize: '0.8rem', marginBottom: 8 }}>
        {[['ChoXacNhan', 'Chờ xác nhận'], ['DaXacNhan', 'Đã xác nhận'], ['DangThue', 'Đang thuê'], ['DaTraXe', 'Đã trả'], ['KeHoach', 'Bảo trì (kế hoạch)'], ['DangThucHien', 'Đang bảo trì']].map(([k, l]) => (
          <span key={k}><span style={{ display: 'inline-block', width: 10, height: 10, background: COLORS[k], marginRight: 4 }} />{l}</span>
        ))}
      </div>
      {error && <div style={{ ...card, color: '#b91c1c', marginBottom: 12 }}>{error}</div>}
      <div style={{ ...card, padding: 0, overflowX: 'auto' }}>
        <div style={{ width: NAME_W + span * DAY_W }}>
          <div style={{ display: 'grid', gridTemplateColumns: `${NAME_W}px repeat(${span}, ${DAY_W}px)`, background: '#f9fafb', fontSize: '0.75rem' }}>
            <div style={{ padding: 8 }}>Xe</div>
            {days.map((d) => (
              <div key={d.format('YYYYMMDD')} style={{ textAlign: 'center', padding: '8px 0', background: d.isSame(dayjs(), 'day') ? '#dbeafe' : undefined }}>
                {d.format('DD')}<br />{d.format('MM')}
              </div>
            ))}
          </div>
          {rows.map((car) => (
            <div key={car.CarId} style={{ display: 'flex', borderTop: '1px solid #f3f4f6', height: 38 }}>
              <div style={{ width: NAME_W, padding: '4px 8px', fontSize: '0.8rem', flex: 'none' }}>
                <strong>{car.LicensePlate}</strong><br />{car.Name}
              </div>
              <div style={{ position: 'relative', width: span * DAY_W, flex: 'none' }}>
                {car.Items.map((it) => {
                  const { left, width } = place(it);
                  const isBooking = it.Kind === 'Booking';
                  return (
                    <div
                      key={`${it.Kind}-${it.Id}`}
                      title={`${isBooking ? '' : (MAINT_LABEL[it.Label] || it.Label) + ' · '}${it.Label} · ${dayjs(parseUtc(it.Start)).format('DD/MM HH:mm')} - ${dayjs(parseUtc(it.End)).format('DD/MM HH:mm')}`}
                      onClick={isBooking ? () => navigate('/admin/bookings') : undefined}
                      style={{
                        position: 'absolute', top: 6, height: 26, left, width, background: COLORS[it.Status] || '#6b7280',
                        color: '#fff', fontSize: '0.7rem', borderRadius: 4, padding: '0 4px', overflow: 'hidden', whiteSpace: 'nowrap',
                        lineHeight: '26px', cursor: isBooking ? 'pointer' : 'default',
                      }}
                    >
                      {isBooking ? it.Label : (MAINT_LABEL[it.Label] || it.Label)}
                    </div>
                  );
                })}
              </div>
            </div>
          ))}
          {rows.length === 0 && <p style={{ padding: 16, color: '#6b7280' }}>Không có xe.</p>}
        </div>
      </div>
    </div>
  );
};

export default AdminCalendarPage;
