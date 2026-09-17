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

const request = (path, method = 'GET', data = {}) => new Promise((resolve, reject) => {
  uni.request({
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
})

export const get = (path, params) => request(`${path}${buildQuery(params)}`)
export const post = (path, data) => request(path, 'POST', data)
export { handleUnauthorized }
