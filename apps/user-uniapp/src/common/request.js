const BASE_URL = 'http://127.0.0.1:5008/gateway'

const buildQuery = params => {
  const query = Object.entries(params || {})
    .filter(([, value]) => value !== '' && value !== null && value !== undefined)
    .map(([key, value]) => `${encodeURIComponent(key)}=${encodeURIComponent(value)}`)
    .join('&')
  return query ? `?${query}` : ''
}

const request = (path, method = 'GET', data = {}) => new Promise((resolve, reject) => {
  uni.request({
    url: BASE_URL + path, method, data,
    header: { Authorization: `Bearer ${uni.getStorageSync('token') || ''}` },
    success: ({ statusCode, data }) => {
      if (Number(data?.code) === 200) return resolve(data.data)
      uni.showToast({ title: data?.message || '请求失败', icon: 'none' })
      reject(new Error(data?.message || `HTTP ${statusCode}`))
    },
    fail: error => { uni.showToast({ title: '网络异常', icon: 'none' }); reject(error) }
  })
})

export const get = (path, params) => request(`${path}${buildQuery(params)}`)
export const post = (path, data) => request(path, 'POST', data)
