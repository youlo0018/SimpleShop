<template>
  <view class="page">
    <!-- 会员中心头部（参考图）：头像 + 名称 + 掩码手机号 -->
    <view class="member-head">
      <view class="avatar">{{ avatarText }}</view>
      <view class="who">
        <view class="name">{{ user.userName || '未登录' }}</view>
        <view class="sub">{{ maskedPhone || '登录后享受完整服务' }}</view>
      </view>
      <view v-if="logged" class="head-badge">◉</view>
    </view>

    <!-- 会员卡（参考图）：渐变底 + 等级 + 升级进度 + 权益入口 -->
    <view class="tier-card" :style="{ background: tierGradient }">
      <view class="tier-glow tier-glow-a" />
      <view class="tier-glow tier-glow-b" />
      <view class="tier-head">
        <view class="tier-left">
          <text class="tier-name">{{ tier.name }}</text>
          <text class="tier-cycle">升级周期 {{ cycleText }}</text>
        </view>
        <view class="tier-link" @tap="goBenefits">会员权益中心</view>
      </view>
      <view class="tier-progress">
        <view class="tier-track"><view class="tier-fill" :style="{ width: tier.percent + '%' }" /></view>
        <text class="tier-target">¥{{ tier.next ? nextThreshold : tier.spent.toFixed(0) }}</text>
      </view>
      <text v-if="tier.next" class="tier-hint">再消费{{ tier.remain.toFixed(2) }}元即可升级{{ tier.next }}</text>
      <text v-else class="tier-hint">已达最高等级，感谢一路相伴</text>
    </view>

    <!-- 权益行（参考图 4 个圆形权益） -->
    <view class="benefits">
      <text class="benefits-title">{{ platform?.platformName || '本平台' }}{{ tier.name }}，享受以下权益</text>
      <view class="benefits-row">
        <view v-for="(item, index) in benefits" :key="`${item.title}-${index}`" class="benefit" @tap="goService(item)">
          <image v-if="isImageIcon(item.icon)" class="benefit-icon" :src="item.icon" mode="aspectFit" />
          <text v-else class="benefit-emoji">{{ item.icon }}</text>
          <text class="benefit-label">{{ item.title }}</text>
        </view>
      </view>
    </view>

    <!-- 我的账户（参考图）：券 / 收藏 / 订单 -->
    <view class="account-card">
      <view class="account-head">
        <text class="account-title">我的账户</text>
        <text class="account-more" :style="{ color: theme.primary }" @tap="go('/pages/orders/orders')">全部 ›</text>
      </view>
      <view class="account-stats">
        <view class="stat" @tap="go('/pages/coupon/mine')"><text class="stat-num">{{ stats.coupons }}<text class="stat-unit">张</text></text><text class="stat-label">优惠券</text></view>
        <view class="stat" @tap="go('/pages/favorites/favorites')"><text class="stat-num">{{ stats.favorites }}</text><text class="stat-label">我的收藏</text></view>
        <view class="stat" @tap="go('/pages/orders/orders')"><text class="stat-num">{{ stats.orders }}</text><text class="stat-label">订单</text></view>
      </view>
    </view>

    <!-- 我的服务（参考图 5 列线性图标宫格，后台按平台配置） -->
    <view class="group services-card">
      <view class="services-title">我的服务</view>
      <view class="services-grid">
        <view v-for="(item, index) in services" :key="`${item.title}-${index}`" class="service" @tap="goService(item)">
          <image v-if="isImageIcon(item.icon)" class="service-icon-img" :src="item.icon" mode="aspectFit" />
          <text v-else class="service-icon" :style="{ background: theme.primary + '12' }">{{ item.icon || '•' }}</text>
          <text class="service-label">{{ item.title }}</text>
        </view>
      </view>
    </view>

    <view class="group">
      <view class="cell" @tap="switchPlatform">
        <image class="cell-icon" src="/static/tabbar/cell-pin.png" mode="aspectFit" />
        <view class="platform-info">
          <view class="p-name">{{ platform?.platformName || '未选择平台' }}</view>
          <view class="p-code">{{ platform?.platformCode || '返回首页选择平台' }}</view>
        </view>
        <text class="chevron">›</text>
      </view>
    </view>

    <button v-if="logged" class="exit safe-bottom" @tap="exit">退出登录</button>
    <button v-else class="login safe-bottom" :style="{ background: theme.primary }" @tap="go('/pages/auth/login')">立即登录</button>
  </view>
</template>

<script setup>
import { computed, ref } from 'vue'
import { onShow } from '@dcloudio/uni-app'
import { get } from '@/common/request'
import { loadPlatformDesign } from '@/common/design'
import { isImageIcon } from '@/common/icon'
import { memberTierOf, MEMBER_TIERS } from '@/common/member'
import { clearPlatform, getPlatform, getTheme, getUser, isLogin, logout } from '@/common/store'

const user = ref({}); const platform = ref(null); const logged = ref(false)
const theme = ref(getTheme()); const services = ref([]); const benefits = ref([])
const stats = ref({ coupons: 0, favorites: 0, orders: 0, spent: 0 })

const avatarText = computed(() => (user.value.userName || 'U').slice(0, 1).toUpperCase())
const maskedPhone = computed(() => {
  const phone = String(user.value.phone || '')
  return phone.length >= 11 ? `${phone.slice(0, 3)}****${phone.slice(-4)}` : phone
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
// 会员卡渐变：参考图的青→紫，跟随平台主题色微调为 主色→紫。
const tierGradient = computed(() => {
  const primary = theme.value.primary || '#0071e3'
  return `linear-gradient(120deg, #57c7c2 0%, ${primary}B3 48%, #8f7ae5 100%)`
})

const go = url => uni.navigateTo({ url: logged.value ? url : '/pages/auth/login' })
const switchPlatform = () => { clearPlatform(); uni.navigateTo({ url: '/pages/platform/selector' }) }
const refresh = () => { user.value = getUser(); logged.value = isLogin(); uni.showToast({ title: '已刷新' }) }
const exit = () => { logout(); user.value = {}; logged.value = false; stats.value = { coupons: 0, favorites: 0, orders: 0, spent: 0 }; uni.showToast({ title: '已退出' }) }
const goBenefits = () => uni.showToast({ title: '权益中心筹备中', icon: 'none' })

/** 我的服务/权益跳转：linkType 由后台装修配置，未知类型回退首页。 */
const goService = item => {
  if (item.linkType === 'refresh') return refresh()
  if (item.linkType === 'service') return uni.showToast({ title: '功能筹备中', icon: 'none' })
  if (item.linkType === 'cart') return uni.switchTab({ url: '/pages/cart/cart' })
  if (item.linkType === 'orders') return go('/pages/orders/orders')
  if (item.linkType === 'address') return go('/pages/address/address')
  if (item.linkType === 'coupons') return go('/pages/coupon/mine')
  if (item.linkType === 'coupon-center') return go('/pages/coupon/center')
  if (item.linkType === 'favorites') return go('/pages/favorites/favorites')
  if (item.linkType === 'category') return uni.switchTab({ url: '/pages/category/category' })
  uni.switchTab({ url: '/pages/home/home' })
}

const loadStats = async () => {
  if (!logged.value) return
  const [coupons, favorites, orders] = await Promise.all([
    get('/marketing/MyCoupons').catch(() => ({ items: [] })),
    get('/users/Favorites').catch(() => []),
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
    { icon: '/static/line/points.png', title: '积分回馈', linkType: 'coupons' },
    { icon: '/static/line/benefit.png', title: '专属活动', linkType: 'coupon-center' },
    { icon: '/static/line/star.png', title: '我的收藏', linkType: 'favorites' },
    { icon: '/static/line/card.png', title: '更多权益', linkType: 'service' }
  ]
  services.value = design?.profile?.services?.length ? design.profile.services : [
    { icon: '/static/line/order.png', title: '我的订单', linkType: 'orders' },
    { icon: '/static/line/cart.png', title: '购物车', linkType: 'cart' },
    { icon: '/static/line/record.png', title: '消费记录', linkType: 'orders' },
    { icon: '/static/line/gift.png', title: '领券中心', linkType: 'coupon-center' },
    { icon: '/static/line/service.png', title: '客服帮助', linkType: 'service' },
    { icon: '/static/line/heart.png', title: '我的收藏', linkType: 'favorites' },
    { icon: '/static/line/card.png', title: '我的券包', linkType: 'coupons' },
    { icon: '/static/line/pin.png', title: '收货地址', linkType: 'address' },
    { icon: '/static/line/invoice.png', title: '发票信息', linkType: 'service' },
    { icon: '/static/line/info.png', title: '关于我们', linkType: 'service' }
  ]
}

onShow(async () => {
  user.value = getUser(); platform.value = getPlatform(); logged.value = isLogin(); theme.value = getTheme()
  await loadServices()
  await loadStats()
})
</script>

<style scoped>
.page { min-height: 100vh; background: #f5f5f7; padding-bottom: 50px; }

/* 会员中心头部：浅底 + 灰头像（参考图） */
.member-head { display: flex; align-items: center; gap: 24rpx; padding: 36rpx 40rpx 26rpx; }
.avatar { width: 108rpx; height: 108rpx; display: grid; place-items: center; border-radius: 50%; background: #e5e5ea; color: #86868b; font-size: 44rpx; font-weight: 700; }
.who { flex: 1; min-width: 0; }
.name { font-size: 42rpx; font-weight: 800; letter-spacing: -.02em; color: #1d1d1f; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; max-width: 440rpx; }
.sub { color: #86868b; margin-top: 8rpx; font-size: 27rpx; }
.head-badge { color: #c7c7cc; font-size: 40rpx; }

/* 会员卡：渐变 + 进度（参考图） */
.tier-card { position: relative; overflow: hidden; margin: 0 24rpx; padding: 34rpx 36rpx 30rpx; border-radius: 32rpx; box-shadow: 0 16rpx 40rpx rgba(0, 0, 0, .16); }
.tier-glow { position: absolute; border-radius: 50%; background: rgba(255, 255, 255, .18); }
.tier-glow-a { width: 300rpx; height: 300rpx; right: -70rpx; top: -140rpx; }
.tier-glow-b { width: 200rpx; height: 200rpx; right: 120rpx; bottom: -120rpx; }
.tier-head { position: relative; display: flex; align-items: flex-start; justify-content: space-between; }
.tier-left { display: grid; gap: 8rpx; }
.tier-name { color: #fff; font-size: 38rpx; font-weight: 800; letter-spacing: -.02em; }
.tier-cycle { color: rgba(255, 255, 255, .82); font-size: 22rpx; }
.tier-link { background: rgba(255, 255, 255, .94); color: #1d1d1f; font-size: 22rpx; font-weight: 600; padding: 10rpx 22rpx; border-radius: 980px; }
.tier-progress { position: relative; display: flex; align-items: center; gap: 16rpx; margin-top: 30rpx; }
.tier-track { flex: 1; height: 14rpx; border-radius: 980px; background: rgba(255, 255, 255, .35); overflow: hidden; }
.tier-fill { height: 100%; border-radius: 980px; background: #fff; }
.tier-target { color: #fff; font-size: 24rpx; font-weight: 700; font-variant-numeric: tabular-nums; }
.tier-hint { position: relative; display: block; margin-top: 18rpx; color: rgba(255, 255, 255, .9); font-size: 24rpx; }

/* 权益行 */
.benefits { padding: 34rpx 24rpx 8rpx; }
.benefits-title { display: block; text-align: center; color: #3a3a3c; font-size: 26rpx; margin-bottom: 28rpx; }
.benefits-row { display: grid; grid-template-columns: repeat(4, 1fr); }
.benefit { display: grid; justify-items: center; gap: 14rpx; }
.benefit-icon { width: 76rpx; height: 76rpx; }
.benefit-emoji { width: 76rpx; height: 76rpx; display: grid; place-items: center; font-size: 38rpx; }
.benefit-label { font-size: 23rpx; color: #1d1d1f; }

/* 我的账户 */
.account-card { background: #fff; border-radius: 28rpx; margin: 24rpx; padding: 30rpx 32rpx 12rpx; box-shadow: 0 8rpx 24rpx rgba(0, 0, 0, .05); }
.account-head { display: flex; align-items: center; justify-content: space-between; }
.account-title { font-size: 32rpx; font-weight: 800; letter-spacing: -.02em; }
.account-more { font-size: 24rpx; font-weight: 600; }
.account-stats { display: flex; margin-top: 22rpx; border-top: 1rpx solid rgba(60, 60, 67, .06); }
.stat { flex: 1; display: grid; justify-items: center; gap: 6rpx; padding: 24rpx 0; }
.stat-num { font-size: 40rpx; font-weight: 800; font-variant-numeric: tabular-nums; }
.stat-unit { font-size: 22rpx; font-weight: 600; margin-left: 2rpx; }
.stat-label { font-size: 22rpx; color: #86868b; }

/* 我的服务：5 列线性图标宫格（参考图） */
.group { background: #fff; border-radius: 28rpx; margin: 0 24rpx 24rpx; overflow: hidden; box-shadow: 0 8rpx 24rpx rgba(0, 0, 0, .05); }
.services-card { padding: 30rpx 14rpx 14rpx; }
.services-title { font-size: 32rpx; font-weight: 800; letter-spacing: -.02em; padding: 0 18rpx 26rpx; }
.services-grid { display: grid; grid-template-columns: repeat(5, 1fr); gap: 30rpx 0; }
.service { display: grid; justify-items: center; gap: 12rpx; }
.service-icon-img { width: 58rpx; height: 58rpx; }
.service-icon { width: 58rpx; height: 58rpx; display: grid; place-items: center; border-radius: 18rpx; font-size: 30rpx; }
.service-label { font-size: 21rpx; color: #3a3a3c; }

/* iOS 设置风格 cell */
.cell { display: flex; align-items: center; gap: 24rpx; padding: 26rpx 30rpx; }
.cell:active { background: #f5f5f7; }
.cell-icon { width: 64rpx; height: 64rpx; border-radius: 16rpx; }
.chevron { color: #c7c7cc; font-size: 40rpx; line-height: 1; font-weight: 400; }
.platform-info { flex: 1; }
.p-name { font-size: 30rpx; font-weight: 600; letter-spacing: -.01em; color: #1d1d1f; }
.p-code { color: #86868b; font-size: 24rpx; margin-top: 6rpx; }

.login, .exit { margin: 48rpx 24rpx; background: #0071e3; color: #fff; height: 96rpx; display: flex; align-items: center; justify-content: center; font-size: 30rpx; box-shadow: 0 8rpx 24rpx rgba(0, 113, 227, .26); }
.exit { background: #fff; color: #ff3b30; box-shadow: 0 2rpx 12rpx rgba(0, 0, 0, .05); }
</style>
