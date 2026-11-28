import axiosClient from '../api/axiosClient';

// Tải tệp cần JWT qua axios dạng blob rồi kích hoạt tải về. Ném Error có message tiếng Việt khi lỗi.
export async function downloadFile(url, params, fallbackName) {
  try {
    const res = await axiosClient.get(url, { params, responseType: 'blob' });
    const cd = res.headers?.['content-disposition'] || '';
    const name = /filename\*?=(?:UTF-8'')?"?([^";]+)"?/i.exec(cd)?.[1] || fallbackName;
    const href = window.URL.createObjectURL(res.data);
    const a = document.createElement('a');
    a.href = href;
    a.download = decodeURIComponent(name);
    document.body.appendChild(a);
    a.click();
    a.remove();
    window.URL.revokeObjectURL(href);
  } catch (err) {
    let message = 'Không thể tải tệp.';
    if (err.response?.data instanceof Blob) {
      try { message = JSON.parse(await err.response.data.text()).message || message; } catch { /* giữ thông báo chung */ }
    }
    throw new Error(message);
  }
}
