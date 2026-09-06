import { useCallback, useEffect, useMemo, useState } from 'react'
import { deleteUser, listUsers, resetDevice } from '../api/admin'
import { formatDateTime } from '../datetime'
import type { User } from '../types'
import { UserFormDialog } from './UserFormDialog'

interface Props {
  onSessionExpired: () => void
}

type DialogState = { open: false } | { open: true; user: User | null }

export function UsersPage({ onSessionExpired }: Props) {
  const [users, setUsers] = useState<User[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [search, setSearch] = useState('')
  const [dialog, setDialog] = useState<DialogState>({ open: false })
  const [busyId, setBusyId] = useState<string | null>(null)

  const reload = useCallback(async () => {
    setError(null)
    try {
      setUsers(await listUsers())
    } catch (err) {
      // Hết phiên thì đá về màn đăng nhập thay vì hiện lỗi đỏ vô nghĩa.
      if (err instanceof Error && err.message.includes('hết hạn')) {
        onSessionExpired()
        return
      }
      setError(err instanceof Error ? err.message : 'Không tải được danh sách.')
    } finally {
      setLoading(false)
    }
  }, [onSessionExpired])

  useEffect(() => {
    void reload()
  }, [reload])

  const visibleUsers = useMemo(() => {
    const keyword = search.trim().toLowerCase()
    if (!keyword) return users
    return users.filter((u) => u.email.toLowerCase().includes(keyword))
  }, [users, search])

  async function handleResetDevice(user: User) {
    if (!confirm(`Gỡ thiết bị đang gắn của ${user.email}?\n\nLần đăng nhập kế tiếp ở BẤT KỲ máy nào sẽ chiếm suất này.`)) {
      return
    }
    setBusyId(user.id)
    try {
      await resetDevice(user.id)
      await reload()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Gỡ thiết bị thất bại.')
    } finally {
      setBusyId(null)
    }
  }

  async function handleDelete(user: User) {
    if (!confirm(`Xóa vĩnh viễn ${user.email}? Không khôi phục được.`)) return
    setBusyId(user.id)
    try {
      await deleteUser(user.id)
      await reload()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Xóa thất bại.')
    } finally {
      setBusyId(null)
    }
  }

  return (
    <section>
      <div className="toolbar">
        <input
          className="search"
          type="search"
          placeholder="Tìm theo email…"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
        <span className="muted">{visibleUsers.length} tài khoản</span>
        <button className="primary" onClick={() => setDialog({ open: true, user: null })}>
          + Tạo tài khoản
        </button>
      </div>

      {error && <p className="error">{error}</p>}

      {loading ? (
        <p className="muted">Đang tải…</p>
      ) : visibleUsers.length === 0 ? (
        <p className="muted">Chưa có tài khoản nào.</p>
      ) : (
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Email</th>
                <th>Vai trò</th>
                <th>Trạng thái</th>
                <th>Thiết bị</th>
                <th>Hết hạn</th>
                <th>Tạo lúc</th>
                <th aria-label="Thao tác" />
              </tr>
            </thead>
            <tbody>
              {visibleUsers.map((user) => (
                <tr key={user.id} className={busyId === user.id ? 'busy' : undefined}>
                  <td className="mono">{user.email}</td>
                  <td>
                    <span className={`badge role-${user.role}`}>{user.role}</span>
                  </td>
                  <td>
                    {!user.isActive ? (
                      <span className="badge danger">đã khóa</span>
                    ) : user.isExpired ? (
                      <span className="badge warn">hết hạn</span>
                    ) : (
                      <span className="badge ok">hoạt động</span>
                    )}
                  </td>
                  <td>
                    {user.deviceId ? (
                      <span title={`Gắn lúc ${formatDateTime(user.deviceBoundAt)}`}>
                        <span className="badge ok">đã gắn</span>
                        <code className="device-id">{user.deviceId.slice(0, 10)}…</code>
                      </span>
                    ) : (
                      <span className="badge muted-badge">trống</span>
                    )}
                  </td>
                  <td>{formatDateTime(user.expiredAt)}</td>
                  <td className="muted">{formatDateTime(user.createdAt)}</td>
                  <td className="row-actions">
                    <button onClick={() => setDialog({ open: true, user })}>Sửa</button>
                    <button
                      onClick={() => void handleResetDevice(user)}
                      disabled={!user.deviceId || busyId === user.id}
                      title={user.deviceId ? 'Gỡ thiết bị đang gắn' : 'Chưa gắn thiết bị nào'}
                    >
                      Gỡ thiết bị
                    </button>
                    <button
                      className="danger-btn"
                      onClick={() => void handleDelete(user)}
                      disabled={busyId === user.id}
                    >
                      Xóa
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {dialog.open && (
        <UserFormDialog
          user={dialog.user}
          onClose={() => setDialog({ open: false })}
          onSaved={() => {
            setDialog({ open: false })
            void reload()
          }}
        />
      )}
    </section>
  )
}
