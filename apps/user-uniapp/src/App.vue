<script>
export default {
  onLaunch(options = {}) {
    const query = options.query || options
    if (query.platformCode) {
      uni.setStorageSync('platform_code', String(query.platformCode))
      uni.removeStorageSync('platform_design')
    }
  }
}
</script>
<style>
/* Apple 设计基线：#f5f5f7 底色、iOS 系统字色、SF 字体栈 */
page {
  background: #f5f5f7;
  color: #1d1d1f;
  font-size: 28rpx;
  font-family: -apple-system, BlinkMacSystemFont, "SF Pro Text", "Helvetica Neue", "PingFang SC", "Hiragino Sans GB", "Microsoft YaHei", system-ui, sans-serif;
  -webkit-font-smoothing: antialiased;
}
button::after { border: 0; }
button { border-radius: 980px; font-weight: 600; letter-spacing: .01em; }
.safe-bottom { padding-bottom: calc(18rpx + env(safe-area-inset-bottom)); }
</style>
