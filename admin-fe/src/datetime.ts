/**
 * Cầu nối giữa hai thế giới thời gian:
 *  - Backend/DB: luôn UTC, chuỗi ISO có hậu tố "Z".
 *  - Người dùng và <input type="datetime-local">: luôn GIỜ ĐỊA PHƯƠNG, không có múi giờ.
 *
 * Mọi chỗ chuyển đổi đều nằm trong file này. Rải logic này khắp component là
 * cách chắc chắn nhất để có bug lệch 7 tiếng.
 */

/** ISO UTC -> chuỗi "YYYY-MM-DDTHH:mm" theo giờ máy, cho <input type="datetime-local">. */
export function toLocalInputValue(isoUtc: string): string {
  const date = new Date(isoUtc)
  // getTimezoneOffset() trả về số PHÚT lệch, dấu ngược với trực giác (VN = -420).
  const localMs = date.getTime() - date.getTimezoneOffset() * 60_000
  return new Date(localMs).toISOString().slice(0, 16)
}

/** Giá trị của <input type="datetime-local"> -> ISO UTC để gửi lên API. */
export function fromLocalInputValue(localValue: string): string {
  // new Date("2026-12-31T23:59") hiểu theo giờ máy; toISOString() đổi sang UTC.
  return new Date(localValue).toISOString()
}

/** Mặc định khi tạo user mới: 1 năm kể từ bây giờ. */
export function defaultExpiryInputValue(): string {
  const oneYearFromNow = new Date()
  oneYearFromNow.setFullYear(oneYearFromNow.getFullYear() + 1)
  return toLocalInputValue(oneYearFromNow.toISOString())
}

/** Hiển thị cho người đọc, theo giờ máy. */
export function formatDateTime(isoUtc: string | null): string {
  if (!isoUtc) return '—'
  return new Date(isoUtc).toLocaleString('vi-VN', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}
