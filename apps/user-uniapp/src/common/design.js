import { get } from './request'
import { applyTheme, getPlatformCode } from './store'

const fallback = {
  theme: { primary: '#0071e3', background: '#f5f5f7', tabColor: '#0071e3' },
  home: { appName: '精选商城', slogan: '', notice: '', banners: [], modules: [] },
  // 我的服务默认项：后台"小程序装修 → 我的服务"可按平台覆盖（icon/title/linkType/linkValue）。
  profile: {
    services: [
      { icon: '🧾', title: '我的订单', linkType: 'orders', linkValue: '' },
      { icon: '🎫', title: '优惠券', linkType: 'coupons', linkValue: '' },
      { icon: '🛒', title: '购物车', linkType: 'cart', linkValue: '' },
      { icon: '📍', title: '收货地址', linkType: 'address', linkValue: '' },
      { icon: '⭐', title: '我的收藏', linkType: 'favorites', linkValue: '' },
      { icon: '🎁', title: '领券中心', linkType: 'coupon-center', linkValue: '' },
      { icon: '↻', title: '刷新资料', linkType: 'refresh', linkValue: '' },
      { icon: '💬', title: '联系客服', linkType: 'service', linkValue: '' }
    ]
  },
  tabs: { home: '首页', category: '分类', cart: '购物车', profile: '我的' }
}

export const loadPlatformDesign = async () => {
  const code = getPlatformCode()
  if (!code) return null
  try {
    const data = await get('/platform-configs/MiniApp', { platformCode: code })
    const design = { ...fallback, ...(data.design || {}) }
    design.profile = { ...fallback.profile, ...(data.design?.profile || {}) }
    applyTheme(design.theme)
    uni.setStorageSync('platform_design', design)
    return design
  } catch {
    return uni.getStorageSync('platform_design') || null
  }
}
