import React from 'react';

/**
 * Phân trang (Pagination)
 *
 * @param {Object} props
 * @param {number} props.currentPage - Trang hiện tại (bắt đầu từ 1)
 * @param {number} props.totalCount - Tổng số bản ghi
 * @param {number} [props.pageSize=12] - Số bản ghi trên mỗi trang
 * @param {Function} props.onPageChange - Hàm callback khi chuyển trang (page: number) => void
 */
const Pagination = ({
  currentPage = 1,
  totalCount = 0,
  pageSize = 12,
  onPageChange,
}) => {
  const effectivePageSize = pageSize > 0 ? pageSize : 12;
  const totalPages = Math.ceil(totalCount / effectivePageSize);

  // Không hiển thị phân trang nếu không có dữ liệu hoặc chỉ có 1 trang
  if (totalPages <= 1) {
    return null;
  }

  // Tính toán tối đa 5 số trang xung quanh trang hiện tại
  const getPageNumbers = () => {
    const maxVisible = 5;
    if (totalPages <= maxVisible) {
      return Array.from({ length: totalPages }, (_, i) => i + 1);
    }

    let start = Math.max(1, currentPage - Math.floor(maxVisible / 2));
    let end = start + maxVisible - 1;

    if (end > totalPages) {
      end = totalPages;
      start = Math.max(1, end - maxVisible + 1);
    }

    const pages = [];
    for (let i = start; i <= end; i++) {
      pages.push(i);
    }
    return pages;
  };

  const pages = getPageNumbers();

  const handlePageClick = (page) => {
    if (page !== currentPage && page >= 1 && page <= totalPages) {
      onPageChange(page);
    }
  };

  return (
    <nav className="pagination-container" aria-label="Phân trang danh sách">
      <ul className="pagination">
        {/* Nút lùi về trang trước */}
        <li className="pagination-item">
          <button
            type="button"
            className="pagination-btn pagination-prev"
            disabled={currentPage <= 1}
            onClick={() => handlePageClick(currentPage - 1)}
            aria-label="Trang trước"
          >
            &laquo; Trước
          </button>
        </li>

        {/* Các số trang */}
        {pages.map((page) => (
          <li key={page} className="pagination-item">
            <button
              type="button"
              className={`pagination-btn pagination-num ${page === currentPage ? 'active' : ''}`}
              onClick={() => handlePageClick(page)}
              aria-current={page === currentPage ? 'page' : undefined}
            >
              {page}
            </button>
          </li>
        ))}

        {/* Nút chuyển trang tiếp theo */}
        <li className="pagination-item">
          <button
            type="button"
            className="pagination-btn pagination-next"
            disabled={currentPage >= totalPages}
            onClick={() => handlePageClick(currentPage + 1)}
            aria-label="Trang sau"
          >
            Sau &raquo;
          </button>
        </li>
      </ul>
    </nav>
  );
};

export default Pagination;
