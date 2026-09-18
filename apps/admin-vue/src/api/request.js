import axios from 'axios'
import { ElMessage } from 'element-plus'

const request = axios.create({
  baseURL: import.meta.env.VITE_API_BASE || 'http://127.0.0.1:5008/gateway',
  timeout: 15000
})

const onLoginPage = () => window.location.hash.startsWith('#/login')

let lastUnauthorizedAt = 0
const redirectToLogin = () => {
  if (Date.now() - lastUnauthorizedAt < 3000) return
  lastUnauthorizedAt = Date.now()
  localStorage.removeItem('admin_token')
  localStorage.removeItem('admin_user')
  if (!onLoginPage()) {
    ElMessage.warning('登录已过期，请重新登录')
    window.location.hash = '#/login'
  }
}

request.interceptors.request.use((config) => {
  const token = localStorage.getItem('admin_token')
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

request.interceptors.response.use((response) => {
  const body = response.data
  // OAuth 风格响应（AuthService OpenIddict 令牌端点）没有业务信封，直接透传。
  if (body && body.access_token) return body
  if (body && Number(body.code) !== 200) {
    if (Number(body.code) === 401 && !onLoginPage()) {
      redirectToLogin()
      return Promise.reject(new Error(body.message || '请先登录'))
    }
    const validation = body.errors || body.data?.errors || null
    const details = Object.values(validation || {}).flat().filter(Boolean).join('；')
    const message = details ? `${body.message || '输入验证失败'}：${details}` : body.message || '请求失败'
    if (details) ElMessage.error(message)
    else ElMessage.error(body.message || '请求失败')
    const error = new Error(message)
    error.validation = validation
    return Promise.reject(error)
  }
  return body?.data ?? body
}, (error) => {
  const body = error.response?.data || {}
  if (error.response?.status === 401 && !onLoginPage()) {
    redirectToLogin()
    error.validation = null
    return Promise.reject(error)
  }
  const validation = body.errors || null
  if (validation) {
    const details = Object.values(validation).flat().filter(Boolean).join('；')
    if (details) body.message = `${body.message || '输入验证失败'}：${details}`
  }
  error.validation = validation
  const message = body.error_description || body.message || error.message || '网络异常'
  ElMessage.error(message)
  return Promise.reject(error)
})

export default request
