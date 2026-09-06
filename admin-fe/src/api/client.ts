import type { ProblemDetails } from '../types'

// Trỏ tạm tới server qua IP thuần, HTTP, cổng 80. Đổi qua VITE_API_BASE_URL
// trong .env.local khi cần (ví dụ http://165.99.14.219:8080 nếu API ở cổng 8080,
// hay http://localhost:8080 khi chạy docker tại máy).
const API_BASE = import.meta.env.VITE_API_BASE_URL ?? 'http://165.99.14.219'

const TOKEN_KEY = 'admin_access_token'
const EXPIRES_KEY = 'admin_token_expires_at'

/**
 * Lỗi có mang theo HTTP status, để chỗ gọi phân biệt được
 * "sai dữ liệu" (400/409) với "hết phiên" (401).
 */
export class ApiError extends Error {
  constructor(
    public readonly status: number,
    message: string,
  ) {
    super(message)
  }
}

// ── Lưu token ────────────────────────────────────────────────────────────────
// localStorage là lựa chọn có đánh đổi: XSS đọc được token. Chấp nhận ở đây vì
// Admin FE là trang nội bộ và đây là cách bạn đang dùng sẵn. Với User FE (nơi
// cần device binding) thì phải dùng cookie HttpOnly — JS không đọc được.

export function saveSession(token: string, expiresAt: string): void {
  localStorage.setItem(TOKEN_KEY, token)
  localStorage.setItem(EXPIRES_KEY, expiresAt)
}

export function clearSession(): void {
  localStorage.removeItem(TOKEN_KEY)
  localStorage.removeItem(EXPIRES_KEY)
}

/** Trả token nếu còn hạn, ngược lại dọn luôn và trả null. */
export function getToken(): string | null {
  const token = localStorage.getItem(TOKEN_KEY)
  const expiresAt = localStorage.getItem(EXPIRES_KEY)
  if (!token || !expiresAt) return null

  // Kiểm tra hạn ở FE CHỈ để đỡ gọi một request chắc chắn hỏng.
  // Backend mới là nơi quyết định — chữ ký token đã khóa cứng mốc hết hạn.
  if (new Date(expiresAt).getTime() <= Date.now()) {
    clearSession()
    return null
  }

  return token
}

// ── Gọi API ──────────────────────────────────────────────────────────────────

/** Bóc thông báo lỗi từ ProblemDetails, gộp cả lỗi validation từng field. */
function readProblem(problem: ProblemDetails, fallback: string): string {
  if (problem.errors) {
    const messages = Object.entries(problem.errors).map(
      ([field, errs]) => `${field}: ${errs.join(', ')}`,
    )
    if (messages.length > 0) return messages.join('\n')
  }
  return problem.detail ?? problem.title ?? fallback
}

export async function request<T>(
  path: string,
  options: { method?: string; body?: unknown } = {},
): Promise<T> {
  const { method = 'GET', body } = options

  const headers: Record<string, string> = {}
  const token = getToken()
  if (token) headers['Authorization'] = `Bearer ${token}`
  if (body !== undefined) headers['Content-Type'] = 'application/json'

  let response: Response
  try {
    response = await fetch(`${API_BASE}${path}`, {
      method,
      headers,
      body: body === undefined ? undefined : JSON.stringify(body),
    })
  } catch {
    // fetch chỉ ném khi không nối được máy chủ (tắt API, sai cổng, CORS chặn).
    throw new ApiError(0, `Không kết nối được tới API ở ${API_BASE}. API đã chạy chưa?`)
  }

  if (response.status === 401) {
    clearSession()
    throw new ApiError(401, 'Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.')
  }

  if (response.status === 204) return undefined as T

  const text = await response.text()
  const payload: unknown = text ? JSON.parse(text) : null

  if (!response.ok) {
    throw new ApiError(
      response.status,
      readProblem((payload ?? {}) as ProblemDetails, `Lỗi ${response.status}`),
    )
  }

  return payload as T
}
