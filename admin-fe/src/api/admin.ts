import { request, saveSession, clearSession } from './client'
import type {
  AdminProfile,
  CreateUserRequest,
  LoginResponse,
  UpdateUserRequest,
  User,
} from '../types'

// ── Đăng nhập ────────────────────────────────────────────────────────────────

export async function login(email: string, password: string): Promise<LoginResponse> {
  const result = await request<LoginResponse>('/api/admin/auth/login', {
    method: 'POST',
    body: { email, password },
  })
  saveSession(result.accessToken, result.expiresAt)
  return result
}

/**
 * Không có API logout ở backend: access token là stateless, server không giữ
 * danh sách phiên nên không "hủy" được. Đăng xuất = xóa token ở máy client.
 * Đánh đổi của việc bỏ refresh token — token bị lộ vẫn sống tới lúc hết hạn.
 */
export function logout(): void {
  clearSession()
}

/** Gọi lúc mở app để biết token còn dùng được không. */
export function fetchProfile(): Promise<AdminProfile> {
  return request('/api/admin/auth/me')
}

// ── Quản lý user ─────────────────────────────────────────────────────────────

export function listUsers(): Promise<User[]> {
  return request('/api/admin/users')
}

export function createUser(payload: CreateUserRequest): Promise<User> {
  return request('/api/admin/users', { method: 'POST', body: payload })
}

export function updateUser(id: string, payload: UpdateUserRequest): Promise<User> {
  return request(`/api/admin/users/${id}`, { method: 'PUT', body: payload })
}

/** Gỡ thiết bị đang gắn để user đăng nhập được trên máy mới. */
export function resetDevice(id: string): Promise<void> {
  return request(`/api/admin/users/${id}/reset-device`, { method: 'POST' })
}

export function deleteUser(id: string): Promise<void> {
  return request(`/api/admin/users/${id}`, { method: 'DELETE' })
}
