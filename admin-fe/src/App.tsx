import { useCallback, useEffect, useState } from 'react'
import { fetchProfile, logout } from './api/admin'
import { getToken } from './api/client'
import { formatDateTime } from './datetime'
import { LoginPage } from './components/LoginPage'
import { UsersPage } from './components/UsersPage'
import type { AdminProfile } from './types'

type Session =
  | { status: 'checking' }
  | { status: 'anonymous' }
  | { status: 'signed-in'; profile: AdminProfile }

export function App() {
  const [session, setSession] = useState<Session>({ status: 'checking' })

  // Mở app: nếu localStorage còn token thì hỏi backend xem có dùng được không.
  // Không tự tin vào token chỉ vì nó tồn tại — khóa ký có thể đã đổi.
  const loadProfile = useCallback(() => {
    if (!getToken()) {
      setSession({ status: 'anonymous' })
      return
    }
    fetchProfile()
      .then((profile) => setSession({ status: 'signed-in', profile }))
      .catch(() => setSession({ status: 'anonymous' }))
  }, [])

  useEffect(loadProfile, [loadProfile])

  const handleSignOut = useCallback(() => {
    logout()
    setSession({ status: 'anonymous' })
  }, [])

  if (session.status === 'checking') {
    return <p className="muted center">Đang kiểm tra phiên đăng nhập…</p>
  }

  if (session.status === 'anonymous') {
    return <LoginPage onSuccess={loadProfile} />
  }

  const { profile } = session

  return (
    <div className="app">
      <header>
        <h1>MyApi Admin</h1>
        <div className="header-right">
          <span className="account">
            <strong>{profile.email}</strong>
            {/* Không có refresh token nên admin nên biết trước lúc nào bị đá ra. */}
            {profile.tokenExpiresAt && (
              <small className="muted">
                Phiên hết hạn {formatDateTime(profile.tokenExpiresAt)}
              </small>
            )}
          </span>
          <button onClick={handleSignOut}>Đăng xuất</button>
        </div>
      </header>
      <main>
        <UsersPage onSessionExpired={handleSignOut} />
      </main>
    </div>
  )
}
