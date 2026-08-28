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
page { color: #17233d; font-size: 14px; }
button::after { border: 0; }
.safe-bottom { padding-bottom: calc(18rpx + env(safe-area-inset-bottom)); }
</style>
