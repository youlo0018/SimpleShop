import axios from 'axios'
import { ElMessage } from 'element-plus'

const request = axios.create({
  baseURL: import.meta.env.VITE_API_BASE || 'http://127.0.0.1:5008/gateway',
  timeout: 15000
})

request.interceptors.request.use((config) => {
  const token = localStorage.getItem('admin_token')
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

request.interceptors.response.use((response) => {
  const body = response.data
  if (body && Number(body.code) !== 200) {
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
  const validation = body.errors || null
  if (validation) {
    const details = Object.values(validation).flat().filter(Boolean).join('；')
    if (details) body.message = `${body.message || '输入验证失败'}：${details}`
  }
  error.validation = validation
  const message = body.message || error.message || '网络异常'
  ElMessage.error(message)
  return Promise.reject(error)
})

export default request
