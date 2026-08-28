import { get } from './request'
import { applyTheme, getPlatformCode } from './store'

const fallback = {
  theme: { primary: '#ff4d6d', background: '#f5f7fb', tabColor: '#ff4d6d' },
  home: { appName: '精选商城', slogan: '', notice: '', banners: [], modules: [] },
  tabs: { home: '首页', category: '分类', cart: '购物车', profile: '我的' }
}

export const loadPlatformDesign = async () => {
  const code = getPlatformCode()
  if (!code) return null
  try {
    const data = await get('/platform-configs/MiniApp', { platformCode: code })
    const design = { ...fallback, ...(data.design || {}) }
    applyTheme(design.theme)
    uni.setStorageSync('platform_design', design)
    return design
  } catch {
    return uni.getStorageSync('platform_design') || null
  }
}
