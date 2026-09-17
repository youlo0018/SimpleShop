export const setSession = (token, user) => { uni.setStorageSync('token', token); uni.setStorageSync('user', user || {}) }
export const getUser = () => uni.getStorageSync('user') || {}
export const getUserId = () => {
  const id = getUser().id
  if (!id || !isLogin()) throw new Error('请先登录')
  return id
}
export const isLogin = () => Boolean(uni.getStorageSync('token'))
export const logout = () => { uni.removeStorageSync('token'); uni.removeStorageSync('user') }

export const getPlatformCode = () => uni.getStorageSync('platform_code') || ''
export const getPlatform = () => uni.getStorageSync('platform') || null
export const setPlatform = platform => {
  uni.setStorageSync('platform', platform || {})
  uni.setStorageSync('platform_code', platform?.platformCode || '')
}
export const clearPlatform = () => { uni.removeStorageSync('platform'); uni.removeStorageSync('platform_code') }
export const requireLogin = () => {
  if (isLogin()) return true
  uni.navigateTo({ url: '/pages/auth/login' })
  return false
}

export const applyTheme = theme => {
  const value = theme || {}
  const applied = {
    primary: value.primary || '#0071e3',
    background: value.background || '#f5f5f7',
    tabColor: value.tabColor || value.primary || '#0071e3'
  }
  uni.setStorageSync('theme', applied)
  // 平台换肤时同步底部 TabBar 选中色（小程序/H5 均支持，失败不影响主流程）。
  try { uni.setTabBarStyle({ color: '#8e8e93', selectedColor: applied.tabColor, backgroundColor: '#ffffff', borderStyle: 'black' }) } catch (error) { /* 非 tabBar 页面忽略 */ }
}
export const getTheme = () => uni.getStorageSync('theme') || { primary: '#0071e3', background: '#f5f5f7', tabColor: '#0071e3' }
