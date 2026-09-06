import { useState, type FormEvent } from 'react'
import { createUser, updateUser } from '../api/admin'
import { defaultExpiryInputValue, fromLocalInputValue, toLocalInputValue } from '../datetime'
import type { User } from '../types'

interface Props {
  /** null = đang tạo mới; có giá trị = đang sửa user đó. */
  user: User | null
  onClose: () => void
  onSaved: () => void
}

export function UserFormDialog({ user, onClose, onSaved }: Props) {
  const isEditing = user !== null

  const [email, setEmail] = useState(user?.email ?? '')
  const [password, setPassword] = useState('')
  const [role, setRole] = useState<'admin' | 'user'>(user?.role ?? 'user')
  const [expiredAt, setExpiredAt] = useState(
    user ? toLocalInputValue(user.expiredAt) : defaultExpiryInputValue(),
  )
  const [isActive, setIsActive] = useState(user?.isActive ?? true)
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setError(null)
    setBusy(true)
    try {
      if (user) {
        await updateUser(user.id, {
          email,
          role,
          expiredAt: fromLocalInputValue(expiredAt),
          isActive,
        })
      } else {
        await createUser({
          email,
          password,
          expiredAt: fromLocalInputValue(expiredAt),
        })
      }
      onSaved()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Lưu thất bại.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="overlay" onClick={onClose}>
      <form className="card dialog" onClick={(e) => e.stopPropagation()} onSubmit={handleSubmit}>
        <h2>{isEditing ? 'Sửa tài khoản' : 'Tạo tài khoản'}</h2>

        <label>
          Email
          <input
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            required
            autoFocus
          />
        </label>

        {/* Mật khẩu chỉ có lúc tạo. Đổi mật khẩu cần luồng riêng (xác minh mật khẩu cũ). */}
        {!isEditing && (
          <label>
            Mật khẩu
            <input
              type="text"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              minLength={6}
              required
            />
            <small className="muted">Tối thiểu 6 ký tự. Backend băm trước khi lưu.</small>
          </label>
        )}

        {isEditing && (
          <label>
            Vai trò
            <select value={role} onChange={(e) => setRole(e.target.value as 'admin' | 'user')}>
              <option value="user">user</option>
              <option value="admin">admin</option>
            </select>
          </label>
        )}

        <label>
          Ngày hết hạn
          <input
            type="datetime-local"
            value={expiredAt}
            onChange={(e) => setExpiredAt(e.target.value)}
            required
          />
          <small className="muted">
            Nhập theo giờ máy bạn; gửi lên API đã quy đổi sang UTC.
          </small>
        </label>

        {isEditing && (
          <label className="checkbox">
            <input
              type="checkbox"
              checked={isActive}
              onChange={(e) => setIsActive(e.target.checked)}
            />
            Đang hoạt động
          </label>
        )}

        {!isEditing && (
          <p className="hint">
            Tài khoản mới có <code>deviceId = null</code> — chưa gắn thiết bị. Thiết bị được
            gắn ở lần user đăng nhập đầu tiên.
          </p>
        )}

        {error && <p className="error">{error}</p>}

        <div className="dialog-actions">
          <button type="button" onClick={onClose} disabled={busy}>
            Hủy
          </button>
          <button type="submit" className="primary" disabled={busy}>
            {busy ? 'Đang lưu…' : isEditing ? 'Lưu thay đổi' : 'Tạo'}
          </button>
        </div>
      </form>
    </div>
  )
}
