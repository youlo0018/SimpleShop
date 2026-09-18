<template>
  <view class="page">
    <!-- 头部：头像 + 昵称/手机号 + 客服/扫码（参考凯德星我的页） -->
    <view class="head">
      <view class="avatar" @tap="goEdit">
        <image v-if="user.avatar" class="avatar-img" :src="assetUrl(user.avatar)" mode="aspectFill" />
        <text v-else class="avatar-icon">👤</text>
      </view>
      <view class="who" @tap="goEdit">
        <view class="name">{{ user.userName || '未登录' }}</view>
        <view class="sub">{{ logged ? maskedPhone : '登录后享受完整服务' }}</view>
      </view>
      <view class="head-actions">
        <view class="head-btn" @tap="goService({ linkType: 'service' })"><text class="head-btn-icon">🎧</text></view>
        <view class="head-btn" @tap="refresh"><text class="head-btn-icon">⟳</text></view>
      </view>
    </view>

    <!-- 会员卡：渐变 + 升级进度 + 权益中心（参考图） -->
    <view class="tier-card">
      <view class="tier-dots" />
      <view class="tier-head">
        <view class="tier-left">
          <text class="tier-name">{{ tier.name }}</text>
          <text class="tier-cycle">升级周期 {{ cycleText }}</text>
        </view>
        <view class="tier-link" @tap="goBenefits">会员权益中心</view>
      </view>
      <view class="tier-progress">
        <view class="tier-dot">⚡</view>
        <view class="tier-track">
          <view class="tier-fill" :style="{ width: tier.percent + '%' }" />
        </view>
        <text class="tier-target">¥{{ tier.next ? nextThreshold : tier.spent.toFixed(0) }}</text>
      </view>
      <text class="tier-hint">{{ tier.next ? `再消费${tier.remain.toFixed(2)}元即可升级${tier.next}` : '已达最高等级，感谢一路相伴' }}</text>
    </view>

    <!-- 权益行：4 个圆形入口（参考图） -->
    <view class="benefits">
      <text class="benefits-title">{{ platform?.platformName || '本平台' }}{{ tier.name }}，享受以下权益</text>
      <view class="benefits-row">
        <view v-for="(item, index) in benefits" :key="`${item.title}-${index}`" class="benefit" @tap="goService(item)">
          <view class="benefit-circle">
            <image v-if="isImageIcon(item.icon)" class="benefit-icon" :src="item.icon" mode="aspectFit" />
            <text v-else class="benefit-emoji">{{ item.icon }}</text>
          </view>
          <text class="benefit-label">{{ item.title }}</text>
        </view>
      </view>
    </view>

    <!-- 我的账户：券 / 收藏 / 订单（参考图三栏） -->
    <view class="card account-card">
      <view class="card-head">
        <text class="card-title">我的账户</text>
        <text class="card-more" @tap="go('/pages/orders/orders')">全部 ›</text>
      </view>
      <view class="account-stats">
        <view class="stat" @tap="go('/pages/coupon/mine')"><text class="stat-num">{{ stats.coupons }}<text class="stat-unit">张</text></text><text class="stat-label">优惠券</text></view>
        <view class="stat" @tap="go('/pages/favorites/favorites')"><text class="stat-num">{{ stats.favorites }}</text><text class="stat-label">我的收藏</text></view>
        <view class="stat" @tap="go('/pages/orders/orders')"><text class="stat-num">{{ stats.orders }}</text><text class="stat-label">订单</text></view>
      </view>
    </view>

    <!-- 我的服务：5 列线性图标宫格（参考图，后台按平台配置） -->
    <view class="card services-card">
      <view class="card-head"><text class="card-title">我的服务</text></view>
      <view class="services-grid">
        <view v-for="(item, index) in services" :key="`${item.title}-${index}`" class="service" @tap="goService(item)">
          <image v-if="isImageIcon(item.icon)" class="service-icon" :src="item.icon" mode="aspectFit" />
          <text v-else class="service-emoji">{{ item.icon || '•' }}</text>
          <text class="service-label">{{ item.title }}</text>
        </view>
      </view>
    </view>

    <button v-if="logged" class="exit" @tap="exit">退出登录</button>
    <button v-else class="login" :style="{ background: theme.primary }" @tap="go('/pages/auth/login')">立即登录</button>
  </view>
</template>

<script setup>
import { computed, ref } from 'vue'
import { onShow } from '@dcloudio/uni-app'
import { get, post } from '@/common/request'
import { loadPlatformDesign } from '@/common/design'
import { isImageIcon } from '@/common/icon'
import { memberTierOf, MEMBER_TIERS } from '@/common/member'
import { getPlatform, getTheme, getUser, isLogin, logout } from '@/common/store'
import { ensurePlatform } from '@/common/platform-config'

const BASE_URL = 'http://127.0.0.1:5008/gateway'
const user = ref({}); const platform = ref(null); const logged = ref(false)
const theme = ref(getTheme()); const services = ref([]); const benefits = ref([])
const stats = ref({ coupons: 0, favorites: 0, orders: 0, spent: 0 })

const assetUrl = path => (!path ? '' : (String(path).startsWith('http') ? path : BASE_URL.replace('/gateway', '') + path))
const maskedPhone = computed(() => {
  const phone = String(user.value.phone || '')
  return phone.length >= 11 ? `${phone.slice(0, 3)}****${phone.slice(-4)}` : phone || '未绑定手机号'
})
const tier = computed(() => memberTierOf(stats.value.spent))
const nextThreshold = computed(() => MEMBER_TIERS.find(item => item.name === tier.value.next)?.threshold || 0)
const cycleText = computed(() => {
  const now = new Date()
  const start = new Date(now.getFullYear(), now.getMonth(), 1)
  const end = new Date(now.getFullYear() + 1, now.getMonth(), 1)
  const fmt = value => `${value.getFullYear()}年${String(value.getMonth() + 1).padStart(2, '0')}月${String(value.getDate()).padStart(2, '0')}日`
  return `${fmt(start)} ~ ${fmt(end)}`
})

const go = url => uni.navigateTo({ url: logged.value ? url : '/pages/auth/login' })
const goEdit = () => go('/pages/profile/edit')
const refresh = () => { user.value = getUser(); logged.value = isLogin(); loadStats(); uni.showToast({ title: '已刷新' }) }
const goBenefits = () => uni.showToast({ title: '权益中心筹备中', icon: 'none' })
const exit = async () => {
  // 先吊销 Redis 会话（服务端即时失效），再清本地存储；网络失败不阻塞退出。
  await post('/customers/Logout').catch(() => {})
  logout()
  user.value = {}; logged.value = false; stats.value = { coupons: 0, favorites: 0, orders: 0, spent: 0 }
  uni.showToast({ title: '已退出' })
}

const loadStats = async () => {
  if (!logged.value) return
  const [coupons, favorites, orders] = await Promise.all([
    get('/marketing/MyCoupons').catch(() => ({ items: [] })),
    get('/customers/Favorites').catch(() => []),
    get('/orders/List', { customerId: 0, page: 1, pageSize: 100 }).catch(() => ({ items: [], total: 0 }))
  ])
  const paid = (orders.items || []).filter(item => Number(item.orderStatus) >= 20 && Number(item.orderStatus) !== 60)
  stats.value = {
    coupons: (coupons.items || []).filter(item => Number(item.status) === 1).length,
    favorites: (favorites || []).length,
    orders: Number(orders.total || 0),
    spent: paid.reduce((sum, item) => sum + Number(item.paymentPrice || 0), 0)
  }
}

/** 权益与我的服务来自平台装修；未装修时使用内置默认（与后端 DefaultDesign 一致）。 */
const loadServices = async () => {
  const design = await loadPlatformDesign().catch(() => null)
  benefits.value = design?.profile?.benefits?.length ? design.profile.benefits : [
    { icon: '/static/line-color/points.png', title: '积分回馈', linkType: 'coupons' },
    { icon: '/static/line-color/benefit.png', title: '专属活动', linkType: 'coupon-center' },
    { icon: '/static/line-color/star.png', title: '我的收藏', linkType: 'favorites' },
    { icon: '/static/line-color/card.png', title: '更多权益', linkType: 'service' }
  ]
  services.value = design?.profile?.services?.length ? design.profile.services : [
    { icon: '/static/line/order.png', title: '我的订单', linkType: 'orders' },
    { icon: '/static/line/cart.png', title: '购物车', linkType: 'cart' },
    { icon: '/static/line/record.png', title: '消费记录', linkType: 'orders' },
    { icon: '/static/line/gift.png', title: '我的活动', linkType: 'coupon-center' },
    { icon: '/static/line/service.png', title: '客服帮助', linkType: 'service' },
    { icon: '/static/line/review.png', title: '评价中心', linkType: 'service' },
    { icon: '/static/line/points.png', title: '积分指南', linkType: 'coupons' },
    { icon: '/static/line/pin.png', title: '收货地址', linkType: 'address' },
    { icon: '/static/line/invoice.png', title: '发票信息', linkType: 'service' },
    { icon: '/static/line/mall.png', title: '关于我们', linkType: 'service' }
  ]
}

onShow(async () => {
  user.value = getUser()
  platform.value = await ensurePlatform().catch(() => null) || getPlatform()
  logged.value = isLogin(); theme.value = getTheme()
  await loadServices()
  await loadStats()
})
</script>

<style scoped>
.page { min-height: 100vh; background: linear-gradient(180deg, #e9f5fb 0%, #f5f7f8 420rpx, #f5f7f8 100%); padding-bottom: 60rpx; }

/* 头部 */
.head { display: flex; align-items: center; gap: 24rpx; padding: 40rpx 36rpx 28rpx; }
.avatar { width: 116rpx; height: 116rpx; border-radius: 50%; background: #fff; display: grid; place-items: center; box-shadow: 0 6rpx 18rpx rgba(0, 0, 0, .06); overflow: hidden; }
.avatar-img { width: 100%; height: 100%; }
.avatar-icon { font-size: 54rpx; color: #c7c7cc; }
.who { flex: 1; min-width: 0; }
.name { font-size: 42rpx; font-weight: 800; letter-spacing: -.02em; color: #1a1a1a; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.sub { color: #8a8f99; margin-top: 10rpx; font-size: 27rpx; }
.head-actions { display: flex; gap: 14rpx; }
.head-btn { width: 68rpx; height: 68rpx; border-radius: 50%; background: #fff; display: grid; place-items: center; box-shadow: 0 6rpx 18rpx rgba(0, 0, 0, .06); }
.head-btn-icon { font-size: 30rpx; color: #1a1a1a; }

/* 会员卡 */
.tier-card { position: relative; overflow: hidden; margin: 6rpx 24rpx 0; padding: 34rpx 34rpx 30rpx; border-radius: 30rpx; background: linear-gradient(120deg, #8fd3e8 0%, #9db6f0 48%, #c6a4e8 100%); box-shadow: 0 18rpx 42rpx rgba(120, 140, 220, .28); }
.tier-dots { position: absolute; inset: 0; background-image: radial-gradient(circle, rgba(255, 255, 255, .38) 2rpx, transparent 2rpx); background-size: 20rpx 20rpx; opacity: .5; }
.tier-head { position: relative; display: flex; align-items: flex-start; justify-content: space-between; }
.tier-left { display: grid; gap: 8rpx; }
.tier-name { color: #fff; font-size: 40rpx; font-weight: 800; letter-spacing: -.02em; }
.tier-cycle { color: rgba(255, 255, 255, .85); font-size: 22rpx; }
.tier-link { background: #fff; color: #1a1a1a; font-size: 23rpx; font-weight: 600; padding: 12rpx 24rpx; border-radius: 12rpx; box-shadow: 0 6rpx 16rpx rgba(0, 0, 0, .08); }
.tier-progress { position: relative; display: flex; align-items: center; gap: 10rpx; margin-top: 34rpx; }
.tier-dot { width: 40rpx; height: 40rpx; border-radius: 50%; background: #2ec5e8; display: grid; place-items: center; font-size: 20rpx; color: #fff; flex-shrink: 0; }
.tier-track { flex: 1; height: 16rpx; border-radius: 980px; background: rgba(48, 52, 92, .78); overflow: hidden; }
.tier-fill { height: 100%; border-radius: 980px; background: linear-gradient(90deg, #bfe8ff, #ffffff); }
.tier-target { color: #fff; font-size: 25rpx; font-weight: 700; font-variant-numeric: tabular-nums; }
.tier-hint { position: relative; display: block; margin-top: 20rpx; color: rgba(255, 255, 255, .94); font-size: 24rpx; }

/* 权益行 */
.benefits { padding: 40rpx 24rpx 10rpx; }
.benefits-title { display: block; text-align: center; color: #3a3a3c; font-size: 27rpx; margin-bottom: 32rpx; }
.benefits-row { display: grid; grid-template-columns: repeat(4, 1fr); }
.benefit { display: grid; justify-items: center; gap: 16rpx; }
.benefit-circle { width: 116rpx; height: 116rpx; border-radius: 50%; background: #fff; display: grid; place-items: center; box-shadow: 0 8rpx 20rpx rgba(0, 0, 0, .05); }
.benefit-icon { width: 58rpx; height: 58rpx; }
.benefit-emoji { font-size: 46rpx; }
.benefit-label { font-size: 24rpx; color: #1a1a1a; }

/* 卡片通用 */
.card { background: #fff; border-radius: 28rpx; margin: 24rpx; padding: 30rpx 32rpx; box-shadow: 0 8rpx 24rpx rgba(0, 0, 0, .04); }
.card-head { display: flex; align-items: center; justify-content: space-between; margin-bottom: 8rpx; }
.card-title { font-size: 34rpx; font-weight: 800; letter-spacing: -.02em; color: #1a1a1a; }
.card-more { font-size: 24rpx; color: #9aa0a8; }

/* 我的账户 */
.account-card { padding-bottom: 18rpx; }
.account-stats { display: flex; margin-top: 24rpx; }
.stat { flex: 1; display: grid; justify-items: center; gap: 8rpx; }
.stat-num { font-size: 46rpx; font-weight: 800; font-variant-numeric: tabular-nums; color: #1a1a1a; }
.stat-unit { font-size: 24rpx; font-weight: 600; margin-left: 4rpx; }
.stat-label { font-size: 24rpx; color: #8a8f99; }

/* 我的服务 */
.services-card { padding-bottom: 34rpx; }
.services-grid { display: grid; grid-template-columns: repeat(5, 1fr); gap: 34rpx 8rpx; margin-top: 26rpx; }
.service { display: grid; justify-items: center; gap: 12rpx; }
.service-icon { width: 56rpx; height: 56rpx; }
.service-emoji { width: 56rpx; height: 56rpx; display: grid; place-items: center; font-size: 36rpx; }
.service-label { font-size: 22rpx; color: #1a1a1a; }

.exit, .login { margin: 40rpx 24rpx 0; height: 92rpx; display: flex; align-items: center; justify-content: center; font-size: 30rpx; border-radius: 980px; }
.exit { background: #fff; color: #e93b3d; }
.login { color: #fff; }
</style>
