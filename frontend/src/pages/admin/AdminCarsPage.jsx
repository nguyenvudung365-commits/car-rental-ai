import React, { useState, useEffect, useCallback } from 'react';
import axiosClient from '../../api/axiosClient';
import { carTypeLabel, carStatusLabel, normalizeCarType, normalizeCarStatus, normalizeTransmission, normalizeFuelType, TRANSMISSIONS, FUEL_TYPES, transmissionLabel, fuelLabel } from '../../utils/carLabels';
import CarImagesManager from '../../components/CarImagesManager';

/**
 * Trang Quản lý xe cho Admin/Staff (CRAI-36)
 * - Danh sách xe: GET /cars
 * - Thêm xe: POST /cars
 * - Sửa xe: PUT /cars/:id
 * - Xóa xe: DELETE /cars/:id (backend chặn xóa xe đang thuê)
 */
const CAR_TYPES = { Sedan: 1, SUV: 2, Hatchback: 3, MPV: 4, Pickup: 5, Luxury: 6 };
const CAR_STATUSES = { SanSang: 1, DangThue: 2, BaoTri: 3, NgungHoatDong: 4 };

const EMPTY_FORM = {
  carType: 'Sedan',
  brand: '',
  model: '',
  licensePlate: '',
  seats: 4,
  carAgeYears: 0,
  basePricePerDay: '',
  status: 'SanSang',
  description: '',
  transmission: '',
  fuelType: '',
  manufactureYear: '',
};

const card = { backgroundColor: '#fff', borderRadius: '10px', padding: '20px', boxShadow: '0 1px 3px rgba(0,0,0,0.08)' };
const input = { width: '100%', padding: '8px 10px', borderRadius: '6px', border: '1px solid #d1d5db', fontSize: '0.95rem' };
const th = { textAlign: 'left', padding: '10px', borderBottom: '1px solid #e5e7eb', fontSize: '0.85rem', color: '#6b7280' };
const td = { padding: '10px', borderBottom: '1px solid #f3f4f6', fontSize: '0.92rem' };

const AdminCarsPage = () => {
  const [cars, setCars] = useState([]);
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [total, setTotal] = useState(0);
  const pageSize = 10;

  const [form, setForm] = useState(EMPTY_FORM);
  const [editingId, setEditingId] = useState(null);
  const [showForm, setShowForm] = useState(false);
  const [saving, setSaving] = useState(false);

  const [notification, setNotification] = useState(null);
  const [error, setError] = useState(null);
  const [fieldErrors, setFieldErrors] = useState({});

  useEffect(() => {
    if (!notification) return undefined;
    const timer = setTimeout(() => setNotification(null), 4000);
    return () => clearTimeout(timer);
  }, [notification]);

  const fetchCars = useCallback(async () => {
    try {
      setLoading(true);
      const response = await axiosClient.get('/cars', { params: { page, pageSize } });
      setCars(response.data?.items || []);
      setTotal(response.data?.total || 0);
    } catch (err) {
      console.error('Lỗi khi tải danh sách xe:', err);
      setError('Không thể tải danh sách xe. Vui lòng thử lại!');
    } finally {
      setLoading(false);
    }
  }, [page]);

  useEffect(() => {
    fetchCars();
  }, [fetchCars]);

  const openCreate = () => {
    setEditingId(null);
    setForm(EMPTY_FORM);
    setShowForm(true);
    setError(null);
    setFieldErrors({});
  };

  const openEdit = (car) => {
    setEditingId(car.Id ?? car.id);
    setForm({
      carType: normalizeCarType(car.CarType ?? car.carType),
      brand: car.Brand ?? car.brand ?? '',
      model: car.Model ?? car.model ?? '',
      licensePlate: car.LicensePlate ?? car.licensePlate ?? '',
      seats: car.Seats ?? car.seats ?? 4,
      carAgeYears: car.CarAgeYears ?? car.carAgeYears ?? 0,
      basePricePerDay: car.BasePricePerDay ?? car.basePricePerDay ?? '',
      status: normalizeCarStatus(car.Status ?? car.status) || 'SanSang',
      description: car.Description ?? car.description ?? '',
      transmission: normalizeTransmission(car.Transmission ?? car.transmission),
      fuelType: normalizeFuelType(car.FuelType ?? car.fuelType),
      manufactureYear: car.ManufactureYear ?? car.manufactureYear ?? '',
    });
    setShowForm(true);
    setError(null);
    setFieldErrors({});
  };

  const closeForm = () => {
    setShowForm(false);
    setEditingId(null);
  };

  const setField = (name) => (e) => {
    setForm((f) => ({ ...f, [name]: e.target.value }));
    setFieldErrors((f) => ({ ...f, [name]: '' }));
  };

  const blurField = (name) => () => setFieldErrors((f) => ({ ...f, [name]: validate()[name] || '' }));

  const inputStyle = (name) => (fieldErrors[name] ? { ...input, borderColor: '#dc2626' } : input);
  const fieldError = (name) =>
    fieldErrors[name] ? <span role="alert" style={{ display: 'block', color: '#dc2626', fontSize: '0.8rem', marginTop: 2 }}>{fieldErrors[name]}</span> : null;

  const validate = () => {
    const e = {};
    if (!form.brand.trim()) e.brand = 'Hãng xe không được để trống.';
    if (!form.model.trim()) e.model = 'Dòng xe không được để trống.';
    if (!form.licensePlate.trim()) e.licensePlate = 'Biển số không được để trống.';
    else if (!/^[0-9]{2}[A-Za-z][A-Za-z0-9]?[-\s.]?[0-9.\-\s]{4,8}$/.test(form.licensePlate.trim()))
      e.licensePlate = 'Biển số không đúng định dạng (ví dụ: 51A-123.45).';
    if (!Number.isInteger(Number(form.seats)) || Number(form.seats) < 1 || Number(form.seats) > 16) e.seats = 'Số chỗ phải từ 1 đến 16.';
    if (!Number.isInteger(Number(form.carAgeYears)) || Number(form.carAgeYears) < 0 || Number(form.carAgeYears) > 50) e.carAgeYears = 'Tuổi xe phải từ 0 đến 50.';
    if (!(Number(form.basePricePerDay) >= 1)) e.basePricePerDay = 'Giá thuê gốc mỗi ngày phải lớn hơn 0.';
    const y = Number(form.manufactureYear);
    if (form.manufactureYear !== '' && (!Number.isInteger(y) || y < 1990 || y > new Date().getFullYear() + 1)) e.manufactureYear = 'Năm sản xuất không hợp lệ.';
    return e;
  };

  const apiMessage = (err, fallback) =>
    err.response?.data?.message || err.response?.data?.Message || fallback;

  const handleSubmit = async (e) => {
    e.preventDefault();
    const errors = validate();
    setFieldErrors(errors);
    if (Object.keys(errors).length > 0) return;

    // API dùng PascalCase và enum dạng số
    const body = {
      CarType: CAR_TYPES[form.carType],
      Brand: form.brand.trim(),
      Model: form.model.trim(),
      LicensePlate: form.licensePlate.trim(),
      Seats: Number(form.seats),
      CarAgeYears: Number(form.carAgeYears),
      BasePricePerDay: Number(form.basePricePerDay),
      Description: form.description.trim() || null,
      Transmission: TRANSMISSIONS[form.transmission] ?? null,
      FuelType: FUEL_TYPES[form.fuelType] ?? null,
      ManufactureYear: form.manufactureYear === '' ? null : Number(form.manufactureYear),
    };

    try {
      setSaving(true);
      setError(null);
      if (editingId) {
        await axiosClient.put(`/cars/${editingId}`, { ...body, Status: CAR_STATUSES[form.status] });
      } else {
        await axiosClient.post('/cars', body);
      }
      setNotification(editingId ? 'Đã cập nhật xe.' : 'Đã thêm xe mới.');
      closeForm();
      await fetchCars();
    } catch (err) {
      console.error('Lỗi khi lưu xe:', err);
      if (err.response?.data?.code === 'PLATE_EXISTS') {
        setFieldErrors({ licensePlate: apiMessage(err, 'Biển số đã tồn tại.') });
      } else {
        setError(apiMessage(err, 'Không thể lưu xe. Vui lòng thử lại.'));
      }
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = async (car) => {
    const id = car.Id ?? car.id;
    if (!window.confirm(`Xóa xe ${car.Brand ?? car.brand} ${car.Model ?? car.model}?`)) return;
    try {
      setError(null);
      await axiosClient.delete(`/cars/${id}`);
      setNotification('Đã xóa xe.');
      if (cars.length === 1 && page > 1) setPage(page - 1);
      else await fetchCars();
    } catch (err) {
      console.error('Lỗi khi xóa xe:', err);
      setError(apiMessage(err, 'Không thể xóa xe (xe đang thuê hoặc đã có đơn).'));
    }
  };

  const totalPages = Math.max(1, Math.ceil(total / pageSize));

  return (
    <div style={{ maxWidth: '1200px', margin: '0 auto', padding: '24px 16px' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '24px' }}>
        <div>
          <h1 style={{ fontSize: '1.75rem', fontWeight: 700, color: '#111827', margin: 0 }}>Quản Lý Xe</h1>
          <p style={{ color: '#6b7280', marginTop: '6px', fontSize: '0.95rem' }}>Thêm, sửa và xóa xe trong hệ thống.</p>
        </div>
        {!showForm && (
          <button type="button" className="btn btn-primary" onClick={openCreate}>+ Thêm xe</button>
        )}
      </div>

      {notification && (
        <div style={{ marginBottom: '16px', padding: '12px 16px', borderRadius: '8px', backgroundColor: '#ecfdf5', color: '#065f46', border: '1px solid #a7f3d0' }}>
          {notification}
        </div>
      )}
      {error && (
        <div role="alert" style={{ marginBottom: '16px', padding: '12px 16px', borderRadius: '8px', backgroundColor: '#fef2f2', color: '#991b1b', border: '1px solid #fee2e2' }}>
          {error}
        </div>
      )}

      {showForm && (
        <form onSubmit={handleSubmit} noValidate style={{ ...card, marginBottom: '24px' }}>
          <h2 style={{ fontSize: '1.2rem', marginTop: 0 }}>{editingId ? 'Sửa thông tin xe' : 'Thêm xe mới'}</h2>
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))', gap: '14px' }}>
            <label>Loại xe
              <select style={input} value={form.carType} onChange={setField('carType')}>
                {Object.keys(CAR_TYPES).map((k) => <option key={k} value={k}>{carTypeLabel(k)}</option>)}
              </select>
            </label>
            <label>Hãng
              <input style={inputStyle('brand')} value={form.brand} onChange={setField('brand')} onBlur={blurField('brand')} required />
              {fieldError('brand')}
            </label>
            <label>Dòng xe
              <input style={inputStyle('model')} value={form.model} onChange={setField('model')} onBlur={blurField('model')} required />
              {fieldError('model')}
            </label>
            <label>Biển số
              <input style={inputStyle('licensePlate')} value={form.licensePlate} onChange={setField('licensePlate')} onBlur={blurField('licensePlate')} required />
              {fieldError('licensePlate')}
            </label>
            <label>Số chỗ
              <input style={inputStyle('seats')} type="number" min="1" max="16" value={form.seats} onChange={setField('seats')} onBlur={blurField('seats')} />
              {fieldError('seats')}
            </label>
            <label>Tuổi xe (năm)
              <input style={inputStyle('carAgeYears')} type="number" min="0" max="50" value={form.carAgeYears} onChange={setField('carAgeYears')} onBlur={blurField('carAgeYears')} />
              {fieldError('carAgeYears')}
            </label>
            <label>Giá thuê gốc / ngày (VNĐ)
              <input style={inputStyle('basePricePerDay')} type="number" min="1" value={form.basePricePerDay} onChange={setField('basePricePerDay')} onBlur={blurField('basePricePerDay')} required />
              {fieldError('basePricePerDay')}
            </label>
            <label>Hộp số
              <select style={input} value={form.transmission} onChange={setField('transmission')}>
                <option value="">— Chưa rõ —</option>
                {Object.keys(TRANSMISSIONS).map((k) => <option key={k} value={k}>{transmissionLabel(k)}</option>)}
              </select>
            </label>
            <label>Nhiên liệu
              <select style={input} value={form.fuelType} onChange={setField('fuelType')}>
                <option value="">— Chưa rõ —</option>
                {Object.keys(FUEL_TYPES).map((k) => <option key={k} value={k}>{fuelLabel(k)}</option>)}
              </select>
            </label>
            <label>Năm sản xuất
              <input style={inputStyle('manufactureYear')} type="number" min="1990" value={form.manufactureYear} onChange={setField('manufactureYear')} onBlur={blurField('manufactureYear')} />
              {fieldError('manufactureYear')}
            </label>
            {editingId && (
              <label>Trạng thái
                <select style={input} value={form.status} onChange={setField('status')}>
                  {Object.keys(CAR_STATUSES).map((k) => <option key={k} value={k}>{carStatusLabel(k)}</option>)}
                </select>
              </label>
            )}
          </div>
          <label style={{ display: 'block', marginTop: '14px' }}>Mô tả
            <textarea style={input} rows={3} value={form.description} onChange={setField('description')} />
          </label>
          {editingId ? <CarImagesManager carId={editingId} /> : <p className="text-muted" style={{ fontSize: '0.85rem', marginTop: 14 }}>Lưu xe xong sẽ thêm được ảnh.</p>}
          <div style={{ marginTop: '16px', display: 'flex', gap: '10px' }}>
            <button type="submit" className="btn btn-primary" disabled={saving}>{saving ? 'Đang lưu...' : 'Lưu'}</button>
            <button type="button" className="btn" onClick={closeForm} disabled={saving}>Hủy</button>
          </div>
        </form>
      )}

      <div style={{ ...card, overflowX: 'auto' }}>
        {loading ? (
          <p>Đang tải...</p>
        ) : cars.length === 0 ? (
          <p>Chưa có xe nào.</p>
        ) : (
          <table style={{ width: '100%', borderCollapse: 'collapse' }}>
            <thead>
              <tr>
                <th style={th}>Xe</th>
                <th style={th}>Biển số</th>
                <th style={th}>Loại</th>
                <th style={th}>Số chỗ</th>
                <th style={th}>Giá gốc/ngày</th>
                <th style={th}>Trạng thái</th>
                <th style={th}></th>
              </tr>
            </thead>
            <tbody>
              {cars.map((car) => {
                const id = car.Id ?? car.id;
                return (
                  <tr key={id}>
                    <td style={td}>{car.Brand ?? car.brand} {car.Model ?? car.model}</td>
                    <td style={td}>{car.LicensePlate ?? car.licensePlate}</td>
                    <td style={td}>{carTypeLabel(car.CarType ?? car.carType)}</td>
                    <td style={td}>{car.Seats ?? car.seats}</td>
                    <td style={td}>{Number(car.BasePricePerDay ?? car.basePricePerDay).toLocaleString('vi-VN')}đ</td>
                    <td style={td}>{carStatusLabel(car.Status ?? car.status)}</td>
                    <td style={{ ...td, whiteSpace: 'nowrap' }}>
                      <button type="button" className="btn" onClick={() => openEdit(car)}>Sửa</button>{' '}
                      <button type="button" className="btn" onClick={() => handleDelete(car)} style={{ color: '#b91c1c' }}>Xóa</button>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        )}

        {totalPages > 1 && (
          <div style={{ display: 'flex', justifyContent: 'center', gap: '12px', marginTop: '16px', alignItems: 'center' }}>
            <button type="button" className="btn" disabled={page <= 1} onClick={() => setPage(page - 1)}>Trước</button>
            <span>Trang {page}/{totalPages}</span>
            <button type="button" className="btn" disabled={page >= totalPages} onClick={() => setPage(page + 1)}>Sau</button>
          </div>
        )}
      </div>
    </div>
  );
};

export default AdminCarsPage;
