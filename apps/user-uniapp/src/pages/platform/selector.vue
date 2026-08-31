<template>
  <view class="page">
    <view class="brand">
      <view class="title">选择商城平台</view>
      <view class="desc">每个平台拥有独立首页、主题和运营内容</view>
    </view>
    <view v-if="loading" class="empty">正在加载平台...</view>
    <view v-else-if="!platforms.length" class="empty">暂无可用平台</view>
    <view v-else class="list">
      <view v-for="platform in platforms" :key="platform.id" class="platform" @tap="choose(platform)">
        <view class="logo">{{ platform.platformName.slice(0, 1) }}</view>
        <view class="info"><view class="name">{{ platform.platformName }}</view><view class="code">平台编码 {{ platform.platformCode }}</view></view>
        <text class="arrow">›</text>
      </view>
    </view>
  </view>
</template>

<script setup>
import { onShow } from '@dcloudio/uni-app'
import { ref } from 'vue'
import { get } from '@/common/request'
import { setPlatform } from '@/common/store'

const platforms = ref([]); const loading = ref(true)
const choose = platform => {
  setPlatform(platform)
  uni.removeStorageSync('platform_design')
  uni.switchTab({ url: '/pages/home/home' })
}
onShow(async () => {
  loading.value = true
  try { platforms.value = await get('/platform-configs/MiniAppPlatforms') || [] } finally { loading.value = false }
})
</script>

<style scoped>
.page { min-height: 100vh; background: #f5f5f7; padding: 48rpx 32rpx; }
.brand { margin: 20rpx 0 44rpx; }
.title { font-size: 52rpx; font-weight: 800; letter-spacing: -.02em; color: #1d1d1f; }
.desc { color: #86868b; margin-top: 12rpx; }
.empty { text-align: center; color: #86868b; padding: 160rpx 0; }
.list { display: grid; gap: 24rpx; }
.platform { display: flex; align-items: center; gap: 24rpx; background: #fff; border-radius: 28rpx; padding: 28rpx; box-shadow: 0 2rpx 12rpx rgba(0, 0, 0, .04); transition: transform .15s ease; }
.platform:active { transform: scale(.98); }
.logo { width: 92rpx; height: 92rpx; border-radius: 24rpx; display: grid; place-items: center; background: linear-gradient(135deg, #0a84ff, #5e5ce6); color: #fff; font-size: 38rpx; font-weight: 800; box-shadow: 0 4rpx 14rpx rgba(10, 132, 255, .3); }
.info { flex: 1; } .name { font-size: 32rpx; font-weight: 700; letter-spacing: -.01em; } .code { color: #86868b; font-size: 24rpx; margin-top: 8rpx; }
.arrow { color: #c7c7cc; font-size: 40rpx; font-weight: 500; }
</style>
