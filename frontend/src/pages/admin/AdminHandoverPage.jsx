import React, { useState } from 'react';
import { useParams, useSearchParams, useNavigate } from 'react-router-dom';
import axiosClient from '../../api/axiosClient';
import HandoverForm from '../../components/HandoverForm';
import DepositPanel from '../../components/DepositPanel';

/** /admin/bookings/:id/handover?type=giao|nhan. Giao xe: thu cọc, lập biên bản rồi chuyển đơn sang DangThue. */
const AdminHandoverPage = () => {
  const { id } = useParams();
  const [params] = useSearchParams();
  const navigate = useNavigate();
  const type = params.get('type') === 'nhan' ? 'Nhan' : 'Giao';
  const [saved, setSaved] = useState(false);
  const [error, setError] = useState('');

  const pickup = async () => {
    setError('');
    try {
      await axiosClient.post(`/bookings/${id}/pickup`);
      navigate('/admin/bookings');
    } catch (err) {
      setError(err.response?.data?.message || 'Không thể giao xe.');
    }
  };

  return (
    <div style={{ maxWidth: 640, margin: '0 auto', padding: '24px 16px' }}>
      <h1>{type === 'Giao' ? 'Giao xe' : 'Nhận xe'} - đơn #{id}</h1>
      {type === 'Giao' && <DepositPanel booking={{ Id: Number(id), Status: 'DaXacNhan' }} canManage />}
      <HandoverForm bookingId={id} type={type} onDone={() => setSaved(true)} />
      {saved && type === 'Giao' && (
        <div style={{ marginTop: 16 }}>
          <p>Đã lưu biên bản. Xác nhận giao xe cho khách (cần đã thu cọc nếu xe có đặt cọc).</p>
          <button className="btn btn-primary" onClick={pickup}>Xác nhận giao xe</button>
        </div>
      )}
      {saved && type === 'Nhan' && <p>Đã lưu biên bản nhận xe. Hãy hoàn tất trả xe ở trang Trả xe.</p>}
      {error && <p className="form-error">{error}</p>}
    </div>
  );
};

export default AdminHandoverPage;
