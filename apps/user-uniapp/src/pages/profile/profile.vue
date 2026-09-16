<template>
  <view class="page">
    <view class="header">
      <view class="avatar">{{ avatarText }}</view>
      <view class="who">
        <view class="name">{{ user.userName || '未登录' }}</view>
        <view class="sub">{{ user.phone || '登录后享受完整服务' }}</view>
      </view>
    </view>

    <view class="group">
      <view class="cell" @tap="go('/pages/orders/orders')">
        <image class="cell-icon" src="/static/tabbar/cell-order.png" mode="aspectFit" />
        <text class="cell-label">我的订单</text><text class="chevron">›</text>
      </view>
      <view class="cell cell-divider" @tap="go('/pages/address/address')">
        <image class="cell-icon" src="/static/tabbar/cell-pin.png" mode="aspectFit" />
        <text class="cell-label">收货地址</text><text class="chevron">›</text>
      </view>
      <view class="cell cell-divider" @tap="goCart">
        <image class="cell-icon" src="/static/tabbar/cell-cart.png" mode="aspectFit" />
        <text class="cell-label">购物车</text><text class="chevron">›</text>
      </view>
      <view class="cell cell-divider" @tap="go('/pages/coupon/center')">
        <image class="cell-icon" src="/static/tabbar/cell-coupon.png" mode="aspectFit" />
        <text class="cell-label">领券中心</text><text class="chevron">›</text>
      </view>
      <view class="cell cell-divider" @tap="go('/pages/coupon/mine')">
        <image class="cell-icon" src="/static/tabbar/cell-coupon.png" mode="aspectFit" />
        <text class="cell-label">我的券包</text><text class="chevron">›</text>
      </view>
      <view class="cell cell-divider" @tap="refresh">
        <image class="cell-icon" src="/static/tabbar/cell-sync.png" mode="aspectFit" />
        <text class="cell-label">刷新资料</text><text class="chevron">›</text>
      </view>
    </view>

    <view class="group">
      <view class="cell" @tap="switchPlatform">
        <view class="platform-info">
          <view class="p-name">{{ platform?.platformName || '未选择平台' }}</view>
          <view class="p-code">{{ platform?.platformCode || '返回首页选择平台' }}</view>
        </view>
        <text class="chevron">›</text>
      </view>
    </view>

    <button v-if="logged" class="exit safe-bottom" @tap="exit">退出登录</button>
    <button v-else class="login safe-bottom" @tap="go('/pages/auth/login')">立即登录</button>
  </view>
</template>

<script setup>
import { onShow } from '@dcloudio/uni-app'
import { computed, ref } from 'vue'
import { clearPlatform, getPlatform, getUser, isLogin, logout, requireLogin } from '@/common/store'

const user = ref({}); const platform = ref(null); const logged = ref(false)
const avatarText = computed(() => (user.value.userName || 'U').slice(0, 1).toUpperCase())
const go = url => uni.navigateTo({ url: logged.value ? url : '/pages/auth/login' })
const goCart = () => { requireLogin(); uni.switchTab({ url: '/pages/cart/cart' }) }
const switchPlatform = () => { clearPlatform(); uni.navigateTo({ url: '/pages/platform/selector' }) }
const refresh = () => { user.value = getUser(); logged.value = isLogin(); uni.showToast({ title: '已刷新' }) }
const exit = () => { logout(); user.value = {}; logged.value = false; uni.showToast({ title: '已退出' }) }
onShow(() => { user.value = getUser(); platform.value = getPlatform(); logged.value = isLogin() })
</script>

<style scoped>
.page { min-height: 100vh; background: #f5f5f7; padding-bottom: 50px; }

/* 头部：渐变 + 首字母头像 */
.header { display: flex; align-items: center; gap: 26rpx; padding: 64rpx 44rpx 56rpx; }
.avatar {
  width: 116rpx; height: 116rpx;
  display: grid; place-items: center;
  border-radius: 50%;
  background: linear-gradient(135deg, #0a84ff, #5e5ce6);
  color: #fff; font-size: 44rpx; font-weight: 700;
  box-shadow: 0 8rpx 24rpx rgba(10, 132, 255, .28);
}
.who { flex: 1; } .name { font-size: 40rpx; font-weight: 700; letter-spacing: -.02em; color: #1d1d1f; }
.sub { color: #86868b; margin-top: 8rpx; font-size: 26rpx; }

/* iOS 设置风格分组列表 */
.group { background: #fff; border-radius: 28rpx; margin: 0 24rpx 24rpx; overflow: hidden; box-shadow: 0 2rpx 12rpx rgba(0, 0, 0, .04); }
.cell { display: flex; align-items: center; gap: 24rpx; padding: 24rpx 30rpx; transition: background .15s ease; }
.cell:active { background: #f5f5f7; }
.cell-divider { border-top: 1rpx solid rgba(0, 0, 0, .05); }
.cell-icon { width: 64rpx; height: 64rpx; border-radius: 16rpx; }
.cell-label { flex: 1; font-size: 30rpx; font-weight: 500; color: #1d1d1f; }
.chevron { color: #c7c7cc; font-size: 40rpx; line-height: 1; font-weight: 400; }

.platform-info { flex: 1; }
.p-name { font-size: 30rpx; font-weight: 600; letter-spacing: -.01em; color: #1d1d1f; }
.p-code { color: #86868b; font-size: 24rpx; margin-top: 6rpx; }

.login, .exit {
  margin: 48rpx 24rpx; background: #0071e3; color: #fff;
  height: 96rpx; display: flex; align-items: center; justify-content: center;
  font-size: 30rpx; box-shadow: 0 8rpx 24rpx rgba(0, 113, 227, .26);
}
.exit { background: #fff; color: #ff3b30; box-shadow: 0 2rpx 12rpx rgba(0, 0, 0, .05); }
</style>
