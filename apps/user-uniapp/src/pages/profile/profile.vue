<template>
  <view class="page">
    <view class="header"><image :src="user.avatar || fallback" class="avatar" /><view><view class="name">{{ user.userName || '未登录' }}</view><view class="sub">{{ user.phone || '登录后享受完整服务' }}</view></view></view>
    <view class="grid">
      <view @tap="go('/pages/orders/orders')"><text class="icon">📦</text><span>订单</span></view><view @tap="go('/pages/address/address')"><text class="icon">📍</text><span>地址</span></view>
      <view @tap="goCart"><text class="icon">🛒</text><span>购物车</span></view><view @tap="refresh"><text class="icon">🔄</text><span>刷新</span></view>
    </view>
    <view class="platform"><view><view class="p-name">{{ platform?.platformName || '未选择平台' }}</view><view class="p-code">{{ platform?.platformCode || '返回首页选择' }}</view></view><button @tap="switchPlatform">切换平台</button></view>
    <button v-if="isLogged" class="exit" @tap="exit">退出登录</button><button v-else class="login" @tap="go('/pages/auth/login')">立即登录</button>
  </view>
</template>

<script setup>
import { onShow } from '@dcloudio/uni-app'
import { ref } from 'vue'
import { clearPlatform, getPlatform, getUser, isLogin, logout, requireLogin } from '@/common/store'

const fallback = 'https://dummyimage.com/120x120/3b82f6/fff&text=U'
const user = ref({}); const platform = ref(null); const logged = ref(false)
const go = url => uni.navigateTo({ url: logged.value ? url : '/pages/auth/login' })
const goCart = () => { requireLogin(); uni.switchTab({ url: '/pages/cart/cart' }) }
const switchPlatform = () => { clearPlatform(); uni.navigateTo({ url: '/pages/platform/selector' }) }
const refresh = () => { user.value = getUser(); logged.value = isLogin(); uni.showToast({ title: '已刷新' }) }
const exit = () => { logout(); user.value = {}; logged.value = false; uni.showToast({ title: '已退出' }) }
onShow(() => { user.value = getUser(); platform.value = getPlatform(); logged.value = isLogin() })
</script>

<style scoped>.page { min-height: 100vh; background: #f5f7fb; padding-bottom: 50px; }
.header { display: flex; align-items: center; gap: 24rpx; background: linear-gradient(135deg,#3b82f6,#8b5cf6); padding: 70rpx 40rpx 90rpx; color: #fff; }
.avatar { width: 110rpx; height: 110rpx; border-radius: 50%; border: 4rpx solid rgba(255,255,255,.6); } .name { font-size: 36rpx; font-weight: 700; } .sub { opacity: .82; margin-top: 8rpx; }
.grid { display: grid; grid-template-columns: repeat(4,1fr); gap: 16rpx; margin: -50rpx 24rpx 0; }
.grid view { background: #fff; border-radius: 20rpx; padding: 26rpx 12rpx; display: grid; justify-items: center; gap: 10rpx; } .icon { font-size: 40rpx; } span { font-size: 24rpx; color: #667085; }
.platform { margin: 20rpx 24rpx; background: #fff; border-radius: 20rpx; padding: 26rpx; display: flex; align-items: center; justify-content: space-between; }
.p-name { font-weight: 700; } .p-code { color: #8a94a6; font-size: 24rpx; margin-top: 8rpx; } .platform button { background: #f1f5f9; color: #334155; font-size: 24rpx; padding: 0 24rpx; height: 58rpx; line-height: 58rpx; }
.login,.exit { margin: 40rpx 24rpx; background: #ff4d6d; color: #fff; } .exit { background: #fff; color: #e11d48; }</style>
