import React, { useState, useEffect, useCallback, useRef } from 'react';
import axiosClient from '../api/axiosClient';
import useAuth from '../auth/AuthContext';
import TierBadge, { DOC_STATUS, TIERS } from '../components/TierBadge';
import DocumentUploadForm from '../components/DocumentUploadForm';
import SecureImage from '../components/SecureImage';
import { toWebp } from '../utils/toWebp';
import { imageUrl } from '../utils/imageUrl';
import { card, input, apiMessage } from '../utils/adminUi';

const DOC_TYPES = { 1: 'CCCD', 2: 'Bằng lái' };
const STATUS_COLOR = { 1: '#92400e', 2: '#047857', 3: '#b91c1c' };
const STATUS_TEXT = { 1: 'Chờ duyệt', 2: 'Đã duyệt', 3: 'Bị từ chối' };

/** Hồ sơ cá nhân: thông tin, đổi mật khẩu, giấy tờ xác minh, điểm thưởng. Dùng cho mọi vai trò. */
const ProfilePage = () => {
  const { refreshProfile } = useAuth();
  const [tab, setTab] = useState('info');
  const [profile, setProfile] = useState(null);
  const [loyalty, setLoyalty] = useState(null);
  const [docs, setDocs] = useState([]);
  const [msg, setMsg] = useState(null);
  const [error, setError] = useState(null);
  const [pw, setPw] = useState({ current: '', next: '' });
  const [uploadType, setUploadType] = useState(null);
  const fileRef = useRef(null);

  const load = useCallback(async () => {
    try {
      const [p, l, d] = await Promise.all([
        axiosClient.get('/auth/me'), axiosClient.get('/me/loyalty'), axiosClient.get('/me/documents'),
      ]);
      setProfile(p.data); setLoyalty(l.data); setDocs(d.data);
      refreshProfile?.();
    } catch (err) { setError(apiMessage(err, 'Không tải được hồ sơ.')); }
  }, [refreshProfile]);

  useEffect(() => { load(); }, []); // eslint-disable-line react-hooks/exhaustive-deps

  const run = async (fn, ok) => {
    try { setError(null); setMsg(null); await fn(); setMsg(ok); await load(); }
    catch (err) { setError(apiMessage(err, 'Thao tác thất bại.')); }
  };

  const saveInfo = (e) => {
    e.preventDefault();
    run(() => axiosClient.put('/auth/me', {
      FullName: profile.FullName.trim(),
      IdentityNumber: profile.IdentityNumber?.trim() || null,
      Address: profile.Address?.trim() || null,
    }), 'Đã lưu thông tin.');
  };

  const changePw = (e) => {
    e.preventDefault();
    run(async () => {
      await axiosClient.post('/auth/me/password', { CurrentPassword: pw.current, NewPassword: pw.next });
      setPw({ current: '', next: '' });
    }, 'Đã đổi mật khẩu.');
  };

  const onAvatar = (e) => {
    const f = e.target.files[0];
    if (!f) return;
    run(async () => {
      const body = new FormData();
      body.append('file', await toWebp(f), 'avatar.webp');
      await axiosClient.post('/auth/me/avatar', body, { headers: { 'Content-Type': undefined } });
    }, 'Đã cập nhật ảnh đại diện.');
    if (fileRef.current) fileRef.current.value = '';
  };

  if (!profile) return <div style={{ padding: 24 }}>{error || 'Đang tải...'}</div>;
  const set = (k) => (e) => setProfile((p) => ({ ...p, [k]: e.target.value }));
  const tabBtn = (k, t) => (
    <button type="button" className={`btn ${tab === k ? 'btn-primary' : ''}`} onClick={() => setTab(k)}>{t}</button>
  );

  return (
    <div style={{ maxWidth: 900, margin: '0 auto', padding: '24px 16px' }}>
      <div style={{ ...card, display: 'flex', gap: 16, alignItems: 'center', marginBottom: 16 }}>
        {profile.AvatarKey
          ? <img src={imageUrl({ FilePath: profile.AvatarKey })} alt="Ảnh đại diện" style={{ width: 72, height: 72, borderRadius: '50%', objectFit: 'cover' }} />
          : <div style={{ width: 72, height: 72, borderRadius: '50%', background: '#e5e7eb' }} />}
        <div style={{ flex: 1 }}>
          <h1 style={{ margin: 0, fontSize: '1.4rem' }}>{profile.FullName} <TierBadge tier={profile.Tier} /></h1>
          <div style={{ color: '#6b7280' }}>{profile.Email} · {profile.PhoneNumber}</div>
          <div style={{ fontSize: '0.9rem' }}>Giấy tờ: <strong>{DOC_STATUS[profile.DocumentsStatus]}</strong></div>
        </div>
        <label className="btn btn-secondary btn-sm" style={{ cursor: 'pointer' }}>
          Đổi ảnh
          <input ref={fileRef} type="file" accept="image/*" onChange={onAvatar} style={{ display: 'none' }} />
        </label>
      </div>

      {loyalty && (
        <div style={{ ...card, marginBottom: 16 }}>
          <strong>Điểm thưởng: {loyalty.Points}</strong>
          <span style={{ color: '#6b7280' }}> (tích lũy {loyalty.LifetimePoints}; 1% giá trị đơn, cộng khi trả xe)</span>
          {loyalty.NextTier && (
            <div style={{ fontSize: '0.9rem' }}>Còn {loyalty.PointsToNextTier} điểm để lên hạng {TIERS[loyalty.NextTier]?.label}.</div>
          )}
          {loyalty.History?.length > 0 && (
            <ul style={{ margin: '8px 0 0', paddingLeft: 18, fontSize: '0.85rem' }}>
              {loyalty.History.slice(0, 5).map((h) => (
                <li key={h.Id}>{h.Points > 0 ? '+' : ''}{h.Points} điểm{h.BookingId ? ` (đơn #${h.BookingId})` : ''}{h.Note ? ` - ${h.Note}` : ''}</li>
              ))}
            </ul>
          )}
        </div>
      )}

      <div style={{ display: 'flex', gap: 8, marginBottom: 12 }}>
        {tabBtn('info', 'Thông tin')}{tabBtn('pw', 'Đổi mật khẩu')}{tabBtn('docs', 'Giấy tờ')}
      </div>
      {msg && <div style={{ marginBottom: 12, padding: 12, borderRadius: 8, background: '#ecfdf5', color: '#065f46' }}>{msg}</div>}
      {error && <div role="alert" style={{ marginBottom: 12, padding: 12, borderRadius: 8, background: '#fee2e2', color: '#991b1b' }}>{error}</div>}

      <div style={card}>
        {tab === 'info' && (
          <form onSubmit={saveInfo} style={{ display: 'grid', gap: 8, maxWidth: 420 }}>
            <input style={input} value={profile.FullName || ''} onChange={set('FullName')} placeholder="Họ tên" required />
            <input style={input} value={profile.IdentityNumber || ''} onChange={set('IdentityNumber')} placeholder="Số CCCD" />
            <input style={input} value={profile.Address || ''} onChange={set('Address')} placeholder="Địa chỉ" />
            <small style={{ color: '#6b7280' }}>Đổi số CCCD sẽ cần xác minh lại giấy tờ.</small>
            <button type="submit" className="btn btn-primary">Lưu</button>
          </form>
        )}
        {tab === 'pw' && (
          <form onSubmit={changePw} style={{ display: 'grid', gap: 8, maxWidth: 420 }}>
            <input style={input} type="password" placeholder="Mật khẩu hiện tại" value={pw.current} onChange={(e) => setPw({ ...pw, current: e.target.value })} required />
            <input style={input} type="password" placeholder="Mật khẩu mới (tối thiểu 8 ký tự)" minLength={8} value={pw.next} onChange={(e) => setPw({ ...pw, next: e.target.value })} required />
            <button type="submit" className="btn btn-primary">Đổi mật khẩu</button>
          </form>
        )}
        {tab === 'docs' && (
          <div>
            {docs.map((d) => (
              <div key={d.Id} style={{ marginBottom: 14, paddingBottom: 12, borderBottom: '1px solid #f3f4f6' }}>
                <strong>{DOC_TYPES[d.Type]}</strong> {d.Number}{' '}
                <span style={{ color: STATUS_COLOR[d.Status] }}>{STATUS_TEXT[d.Status]}</span>
                {d.Status === 3 && d.RejectReason && <div style={{ color: '#b91c1c' }}>Lý do: {d.RejectReason}</div>}
                <div style={{ display: 'flex', gap: 8, marginTop: 6 }}>
                  <SecureImage docId={d.Id} style={{ maxHeight: 120 }} />
                  {d.HasBack && <SecureImage docId={d.Id} side="back" style={{ maxHeight: 120 }} />}
                </div>
              </div>
            ))}
            {docs.length === 0 && <p>Chưa nộp giấy tờ nào. Cần CCCD và bằng lái đã xác minh để nhận xe.</p>}
            {uploadType
              ? <DocumentUploadForm type={uploadType} onDone={() => { setUploadType(null); setMsg('Đã nộp, chờ duyệt.'); load(); }} />
              : (
                <div style={{ display: 'flex', gap: 8 }}>
                  <button type="button" className="btn" onClick={() => setUploadType(1)}>Nộp / nộp lại CCCD</button>
                  <button type="button" className="btn" onClick={() => setUploadType(2)}>Nộp / nộp lại bằng lái</button>
                </div>
              )}
          </div>
        )}
      </div>
    </div>
  );
};

export default ProfilePage;
