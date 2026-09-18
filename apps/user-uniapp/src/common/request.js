const BASE_URL = 'http://127.0.0.1:5008/gateway'

const buildQuery = params => {
  const query = Object.entries(params || {})
    .filter(([, value]) => value !== '' && value !== null && value !== undefined)
    .map(([key, value]) => `${encodeURIComponent(key)}=${encodeURIComponent(value)}`)
    .join('&')
  return query ? `?${query}` : ''
}

const currentRoute = () => {
  const pages = getCurrentPages()
  return pages.length ? `${pages[pages.length - 1].route}` : ''
}

// 登录失效统一处理：清会话 → 提示 → 跳登录（3 秒去重防并发请求重复跳转；登录页内不跳）。
let lastUnauthorizedAt = 0
const handleUnauthorized = () => {
  if (Date.now() - lastUnauthorizedAt < 3000) return
  lastUnauthorizedAt = Date.now()
  uni.removeStorageSync('token')
  uni.removeStorageSync('user')
  if (currentRoute().includes('pages/auth/login')) return
  uni.showToast({ title: '登录已过期，请重新登录', icon: 'none' })
  setTimeout(() => {
    uni.navigateTo({
      url: '/pages/auth/login',
      fail: () => uni.reLaunch({ url: '/pages/auth/login' })
    })
  }, 400)
}

// base64url 解码（小程序无 atob）：仅用于读取 JWT 的 exp，不校验签名。
const base64UrlDecode = input => {
  const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/'
  let text = String(input || '').replace(/-/g, '+').replace(/_/g, '/')
  while (text.length % 4) text += '='
  let output = ''
  for (let index = 0; index < text.length; index += 4) {
    const enc1 = chars.indexOf(text[index]); const enc2 = chars.indexOf(text[index + 1])
    const enc3 = chars.indexOf(text[index + 2]); const enc4 = chars.indexOf(text[index + 3])
    output += String.fromCharCode((enc1 << 2) | (enc2 >> 4))
    if (enc3 > -1) output += String.fromCharCode(((enc2 & 15) << 4) | (enc3 >> 2))
    if (enc4 > -1) output += String.fromCharCode(((enc3 & 3) << 6) | enc4)
  }
  return output
}
const tokenExpireAt = token => {
  try {
    const matched = base64UrlDecode(String(token).split('.')[1] || '').match(/"exp":(\d+)/)
    return matched ? Number(matched[1]) * 1000 : 0
  } catch { return 0 }
}

// 临近过期（30 分钟内）自动刷新令牌：Redis 会话滑动续期，活跃用户不掉线。
let refreshing = null
const refreshTokenIfNeeded = () => {
  const token = uni.getStorageSync('token')
  if (!token) return Promise.resolve()
  const expireAt = tokenExpireAt(token)
  if (!expireAt || expireAt - Date.now() > 30 * 60 * 1000) return Promise.resolve()
  if (!refreshing) {
    refreshing = new Promise(resolve => {
      uni.request({
        url: `${BASE_URL}/customers/RefreshToken`, method: 'POST',
        header: { Authorization: `Bearer ${token}` },
        success: ({ data }) => {
          if (Number(data?.code) === 200 && data.data?.token) {
            uni.setStorageSync('token', data.data.token)
            if (data.data.user) uni.setStorageSync('user', data.data.user)
          }
          resolve()
        },
        fail: () => resolve()
      })
    }).finally(() => { refreshing = null })
  }
  return refreshing
}

const request = (path, method = 'GET', data = {}) => new Promise((resolve, reject) => {
  const skipRefresh = ['/customers/Login', '/customers/Register', '/customers/RefreshToken'].some(item => path.startsWith(item))
  const send = () => uni.request({
    url: BASE_URL + path, method, data,
    header: { Authorization: `Bearer ${uni.getStorageSync('token') || ''}` },
    success: ({ statusCode, data }) => {
      // 网关鉴权失败是 HTTP 401；部分服务登录态错误是 HTTP 200 + body.code=401，两种都要跳登录页。
      if (statusCode === 401 || Number(data?.code) === 401) {
        handleUnauthorized()
        return reject(new Error(data?.message || '请先登录'))
      }
      if (Number(data?.code) === 200) return resolve(data.data)
      uni.showToast({ title: data?.message || '请求失败', icon: 'none' })
      reject(new Error(data?.message || `HTTP ${statusCode}`))
    },
    fail: error => { uni.showToast({ title: '网络异常', icon: 'none' }); reject(error) }
  })
  // 临近过期先刷新再发请求；登录/注册/刷新接口自身不触发刷新，避免递归。
  if (skipRefresh) send()
  else refreshTokenIfNeeded().finally(send)
})

export const get = (path, params) => request(`${path}${buildQuery(params)}`)
export const post = (path, data) => request(path, 'POST', data)
export { handleUnauthorized }
