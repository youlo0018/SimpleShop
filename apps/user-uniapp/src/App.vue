<script>
import { PLATFORM_CODE, ensurePlatform } from '@/common/platform-config'

export default {
  onLaunch(options = {}) {
    // 平台由发布配置决定：启动即锁定，不再支持应用内切换平台。
    uni.setStorageSync('platform_code', PLATFORM_CODE)
    if (options.query?.platformCode && options.query.platformCode !== PLATFORM_CODE) {
      console.warn('[platform] 启动参数平台与配置文件不一致，以配置文件为准', options.query.platformCode)
    }
    ensurePlatform().catch(() => { /* 平台列表暂不可用时由各页面重试 */ })
  },
  onShow() {
    // 兜底：冷启动时把已缓存的平台主题同步到底部 TabBar。
    const theme = uni.getStorageSync('theme')
    if (theme?.tabColor) {
      try { uni.setTabBarStyle({ color: '#8e8e93', selectedColor: theme.tabColor, backgroundColor: '#ffffff', borderStyle: 'black' }) } catch (error) { /* ignore */ }
    }
  }
}
</script>
<style>
/* Apple 设计基线：#f5f5f7 底色、iOS 系统字色、SF 字体栈；设计变量供各页面复用 */
page {
  --bg: #f5f5f7;
  --surface: #ffffff;
  --text-1: #1d1d1f;
  --text-2: #6e6e73;
  --text-3: #a1a1a6;
  --separator: rgba(60, 60, 67, .08);
  --radius-lg: 28rpx;
  --radius-md: 20rpx;
  --radius-sm: 14rpx;
  --shadow-card: 0 6rpx 24rpx rgba(0, 0, 0, .05);
  --shadow-float: 0 10rpx 30rpx rgba(0, 0, 0, .08);
  background: var(--bg);
  color: var(--text-1);
  font-size: 28rpx;
  font-family: -apple-system, BlinkMacSystemFont, "SF Pro Text", "Helvetica Neue", "PingFang SC", "Hiragino Sans GB", "Microsoft YaHei", system-ui, sans-serif;
  -webkit-font-smoothing: antialiased;
}
button::after { border: 0; }
button { border-radius: 980px; font-weight: 600; letter-spacing: .01em; }
.safe-bottom { padding-bottom: calc(18rpx + env(safe-area-inset-bottom)); }
/* #ifdef H5 */
uni-page-body { background: #f5f5f7; }
/* #endif */
/* 通用卡片与空状态，页面内直接复用 */
.apple-card { background: var(--surface); border-radius: var(--radius-lg); box-shadow: var(--shadow-card); }
.empty-state { display: grid; justify-items: center; gap: 18rpx; padding: 120rpx 0; color: var(--text-3); font-size: 26rpx; }
.empty-state .emoji { font-size: 88rpx; line-height: 1; }
</style>
