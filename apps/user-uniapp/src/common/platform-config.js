import { get } from '@/common/request'
import { getPlatform, setPlatform } from '@/common/store'

/**
 * 小程序平台配置（发布时只改这里）
 *
 * 每个平台独立发布一个小程序包：把 PLATFORM_CODE 改成目标平台的 6 位编码，
 * 启动时按此编码定位平台并锁定，用户无需（也不能）在应用内切换平台。
 */
export const PLATFORM_CODE = 'DEMOPL'

/**
 * 确保当前平台与发布配置一致：命中缓存直接返回，否则拉取公开平台列表按编码定位并写入本地。
 * 平台列表暂不可用时返回 null（页面重试），不抛出异常阻塞启动。
 */
export const ensurePlatform = async () => {
  const cached = getPlatform()
  if (cached && cached.platformCode === PLATFORM_CODE && cached.id) return cached

  const platforms = await get('/platform-configs/MiniAppPlatforms').catch(() => null)
  const platform = (platforms || []).find(item => item.platformCode === PLATFORM_CODE)
  if (!platform) return null
  setPlatform(platform)
  // 平台切换（含首次启动）必须丢弃旧装修缓存，避免沿用上一平台的首页/主题。
  uni.removeStorageSync('platform_design')
  return platform
}
