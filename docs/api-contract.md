\# API CONTRACT — Car Rental AI (.NET 10, port 5001)



Quy ước chung:

\- Base URL: `/api`. Body/Response: JSON. Money: VNĐ, số nguyên (decimal 12,0).

\- Auth: JWT Bearer, header `Authorization: Bearer <token>`. Quyền: Public / Customer / Staff / Admin.

\- Lỗi trả về dạng chung:

&#x20; { "errorCode": "BOOKING\_OVERLAP", "message": "Xe đã có đơn trùng lịch" }

\- Status code: 200 OK | 201 Created | 400 Invalid input | 401 Unauthenticated

&#x20; | 403 Forbidden | 404 Not found | 409 Conflict.



\## 1. Auth

\### POST /api/auth/register — Public

Request:  { "fullName": "...", "phone": "09...", "email": "...", "password": "..." }

Response: 201 { "customerId": 1, "fullName": "...", "role": "customer" }

Lỗi: 409 PHONE\_EXISTS / EMAIL\_EXISTS; 400 (mật khẩu < 8 ký tự)



\### POST /api/auth/login — Public

Request:  { "email": "...", "password": "..." }

Response: 200 { "token": "<jwt>", "role": "customer|staff|admin", "fullName": "..." }

Lỗi: 401 INVALID\_CREDENTIALS



\## 2. Cars

\### GET /api/cars — Public

Query: carType, minPrice, maxPrice, status, page=1, pageSize=12

Response: 200 { "items": \[ { "carId":1, "carType":"sedan", "brand":"Toyota",

&#x20; "model":"Vios", "licensePlate":"30A-123.45", "seats":5, "basePricePerDay":500000,

&#x20; "status":"san\_sang", "primaryImageUrl":"..." } ], "totalCount": 40 }



\### GET /api/cars/{id} — Public

Response: 200 (như item trên + "images": \[{carImageId, imageUrl, isPrimary, sortOrder}], "carAgeYears": 3)

Lỗi: 404



\### POST /api/cars — Admin/Staff

Request:  { "carType","brand","model","licensePlate","seats","carAgeYears","basePricePerDay" }

Response: 201 (car object). Lỗi: 409 PLATE\_EXISTS



\### PUT /api/cars/{id} — Admin/Staff | DELETE /api/cars/{id} — Admin

Response: 200 / 204. DELETE chặn (409) nếu xe còn booking Active.



\### POST /api/cars/{id}/images — Admin/Staff (multipart, field "file")

Response: 201 { "carImageId": 5, "imageUrl": "/uploads/car-5/abc.jpg" }

\### DELETE /api/cars/{id}/images/{imageId} — Admin/Staff → 204

\### PUT /api/cars/{id}/images/{imageId}/primary — Admin/Staff → 200



\## 3. Bookings

\### POST /api/bookings — Customer

Request:  { "carId": 1, "startDate": "2026-09-10", "endDate": "2026-09-13", "hasDriver": false }

Response: 201 { "bookingId": 10, "status": "Pending", "rentalDays": 3,

&#x20; "predictedPrice": 585000, "isFallback": false, "overridePrice": null,

&#x20; "finalPricePerDay": 585000, "totalEstimate": 1755000 }

Lỗi: 400 (startDate >= endDate | ngày quá khứ); 409 BOOKING\_OVERLAP; 409 CAR\_NOT\_AVAILABLE

Rule (Application layer): finalPricePerDay = COALESCE(override, predicted, basePricePerDay),

clamp \[400000, 1500000]. Predicted lấy từ /api/ai/quote, fallback = basePricePerDay.



\### GET /api/bookings/mine — Customer

Response: 200 \[ { bookingId, carLicensePlate, startDate, endDate, status, finalPricePerDay } ]

\### GET /api/bookings — Staff/Admin (filter status, page)

\### PUT /api/bookings/{id}/confirm — Staff/Admin: Pending→Confirmed. Lỗi 409 nếu lệch trạng thái/overlap

\### PUT /api/bookings/{id}/cancel  — Customer (chỉ khi Pending|Confirmed) / Staff (mọi trạng thái chưa Completed)

\### GET /api/bookings/{id}/contract — Customer/Staff → application/pdf (QuestPDF, chỉ khi Confirmed trở đi)



\## 4. Payments (1 Booking n Payment)

\### POST /api/bookings/{bookingId}/payments — Customer/Staff

Request:  { "amount": 500000, "method": "cash|transfer|momo" }

Response: 201 { "paymentId": 3, "status": "Pending", ... }

\### PUT /api/payments/{id}/mark-paid — Staff → 200 (status Paid, set paidAt)

\### GET /api/bookings/{bookingId}/payments — Customer/Staff → 200 \[...]



\## 5. Returns

\### POST /api/bookings/{bookingId}/return — Staff

Request:  { "returnDate": "2026-09-15T10:00:00", "damageNote": "xước cản sau" }

Response: 201 { "returnRecordId": 2, "lateDays": 2, "lateFee": 1170000 }

Booking → Completed, Car → san\_sang. lateFee = lateDays \* finalPricePerDay (hệ số 1).

Lỗi: 409 (booking chưa Active / đã trả rồi — ReturnRecord unique theo BookingId)



\## 6. AI (proxy từ .NET → FastAPI :5002, timeout 2s, retry 2)

\### POST /api/ai/quote — Public

Request:  { "carId": 1, "startDate": "...", "endDate": "...", "hasDriver": false }

Response: 200 { "predictedPrice": 585000, "isFallback": false, "correlationId": "..." }

AI lỗi/quá hạn → 200 { "predictedPrice": <basePricePerDay>, "isFallback": true } — KHÔNG 5xx.



\### POST /api/ai/chat — Public

Request:  { "message": "Tôi cần xe 7 chỗ đi Đà Lạt cuối tuần", "sessionId": "abc" }

Response: 200 { "reply": "..." } | Gemini lỗi → 200 { "reply": "<thông báo thân thiện + hướng dẫn liên hệ nhân viên>", "sessionId": "..." }

