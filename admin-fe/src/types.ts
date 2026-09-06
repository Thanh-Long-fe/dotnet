/** Khớp 1-1 với UserResponse ở backend (MyApi/Features/Users/UserDtos.cs). */
export interface User {
  id: string
  email: string
  role: 'admin' | 'user'
  /** null = chưa gắn thiết bị nào. Chỉ backend gán, lúc user đăng nhập lần đầu. */
  deviceId: string | null
  deviceBoundAt: string | null
  expiredAt: string
  isActive: boolean
  /** Backend tự tính theo giờ UTC của server — không tính lại ở FE. */
  isExpired: boolean
  createdAt: string
  updatedAt: string | null
}

export interface CreateUserRequest {
  email: string
  password: string
  /** ISO 8601 có múi giờ, ví dụ "2026-12-31T16:59:59.000Z". */
  expiredAt: string
}

export interface UpdateUserRequest {
  email: string
  role: 'admin' | 'user'
  expiredAt: string
  isActive: boolean
}

export interface LoginResponse {
  accessToken: string
  expiresAt: string
  email: string
}

/** Khớp AdminProfileResponse — kết quả GET /api/admin/auth/me. */
export interface AdminProfile {
  email: string
  role: string
  tokenIssuedAt: string | null
  /** Đọc từ claim "exp" của chính token — đáng tin hơn giá trị FE tự lưu. */
  tokenExpiresAt: string | null
}

/** ProblemDetails (RFC 7807) — dạng lỗi chuẩn mà ASP.NET Core trả về. */
export interface ProblemDetails {
  title?: string
  detail?: string
  status?: number
  errors?: Record<string, string[]>
}
