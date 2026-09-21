<template>
  <view class="page" :style="{ background: theme.background }">
    <view v-if="!items.length" class="empty"><text class="emoji">🛒</text><text>购物车还是空的</text><text class="empty-sub">去挑几件心仪的好物吧</text><button :style="{ background: theme.primary }" @tap="goHome">去逛逛</button></view>
    <view v-else class="body">
      <view v-for="item in items" :key="item.skuId" class="card"><image :src="item.image || fallback" mode="aspectFill" /><view class="info"><view class="name">{{ item.productName }}</view><view class="price" :style="{ color: theme.primary }">¥{{ item.price }}</view><view class="qty"><view class="stepper"><text class="step" :class="{ disabled: Number(item.quantity) <= 1 || changing === item.skuId }" @tap="change(item, -1)">-</text><text>{{ item.quantity }}</text><text class="step" :class="{ disabled: changing === item.skuId }" @tap="change(item, 1)">+</text></view><text class="remove" @tap="remove(item)">删除</text></view></view></view>
      <view v-if="settle && Number(settle.totalDiscount) > 0" class="promo">
        <view class="promo-line">
          <text class="promo-label">已优惠</text>
          <text class="promo-amount">-¥{{ Number(settle.totalDiscount).toFixed(2) }}</text>
        </view>
        <view class="promo-tags">
          <text v-if="bestActivity" class="promo-item">活动：{{ bestActivity.name }}</text>
          <text v-for="item in (settle.coupons || []).slice(0, 2)" :key="'c' + item.userCouponId" class="promo-item">券：{{ item.name }}</text>
        </view>
      </view>
      <view class="bar safe-bottom">
        <view class="bar-total">
          <text v-if="settle && Number(settle.totalDiscount) > 0" class="origin">¥{{ total }}</text>
          合计：<text class="amount" :style="{ color: theme.primary }"><text class="yen">¥</text>{{ payable }}</text>
        </view>
        <button :style="{ background: theme.primary }" @tap="checkout">去结算({{ count }})</button>
      </view>
    </view>
  </view>
</template>

<script setup>
import { computed, ref } from 'vue'
import { onShow } from '@dcloudio/uni-app'
import { get, post } from '@/common/request'
import { getTheme, isLogin, requireLogin } from '@/common/store'
import { validateQuantity } from '@/common/validators'

const fallback = '/static/placeholder.png'
const theme = ref(getTheme()); const items = ref([]); const settle = ref(null); const changing = ref('')
// 与结算页一致：活动只展示最优惠的一个（赠券兜底）。
const bestActivity = computed(() => {
  const list = [...(settle.value?.activities || [])]
  if (!list.length) return null
  return list.sort((a, b) => Number(b.estimatedDiscount) - Number(a.estimatedDiscount)
    || Number(a.threshold) - Number(b.threshold))[0]
})
const total = computed(() => items.value.filter(item => item.checked).reduce((sum, item) => sum + item.price * item.quantity, 0).toFixed(2))
const payable = computed(() => {
  const amount = Number(total.value) - Number(settle.value?.totalDiscount || 0)
  return amount.toFixed(2)
})
const count = computed(() => items.value.filter(item => item.checked).reduce((sum, item) => sum + item.quantity, 0))

// 购物车展示后端自动结算的最优优惠：金额随优惠浮动，实际以提交页用户勾选为准。
const preview = async () => {
  const selected = items.value.filter(item => item.checked)
  if (!selected.length || !isLogin()) { settle.value = null; return }
  try {
    settle.value = await post('/marketing/SettlePreview', {
      platformId: selected[0].platformId,
      items: selected.map(item => ({ skuId: item.skuId, platformId: item.platformId, merchantId: item.merchantId, productName: item.productName, price: Number(item.price), quantity: Number(item.quantity) }))
    })
  } catch { settle.value = null }
}

const load = async () => {
  if (!requireLogin()) return
  const data = await get('/carts/Get')
  items.value = (data.items || []).map(item => ({ ...item, checked: true, quantity: Number(item.quantity), price: Number(item.price), image: item.image || '' }))
  preview()
}
// 后端 /carts/Add 是"累加数量"语义：这里必须传本次增量（±1），否则会出现倍数增长。
const change = async (item, delta) => {
  if (changing.value) return
  const target = Number(item.quantity) + delta
  if (target < 1) return remove(item)
  if (!validateQuantity(target)) return
  changing.value = item.skuId
  try {
    await post('/carts/Add', { ...item, image: item.image, quantity: delta, checked: undefined })
    await load()
  } finally { changing.value = '' }
}
const remove = async item => { await post('/carts/Remove', { skuId: item.skuId }); load() }
const checkout = () => {
  const selected = items.value.filter(item => item.checked)
  if (!selected.length) return uni.showToast({ title: '请选择商品', icon: 'none' })
  uni.setStorageSync('checkout', selected); uni.navigateTo({ url: '/pages/checkout/checkout?type=cart' })
}
const goHome = () => uni.switchTab({ url: '/pages/home/home' })
onShow(() => { theme.value = getTheme(); load() })
</script>

<style scoped>
.page { min-height: 100vh; padding-bottom: 220rpx; }
.empty { display: grid; justify-items: center; gap: 16rpx; color: #a1a1a6; padding-top: 200rpx; font-size: 28rpx; }
.empty .emoji { font-size: 110rpx; line-height: 1; }
.empty .empty-sub { font-size: 24rpx; color: #c7c7cc; }
.empty button { margin: 28rpx auto 0; width: 260rpx; color: #fff; }
.card { display: flex; gap: 22rpx; background: #fff; margin: 22rpx 24rpx; border-radius: 28rpx; padding: 26rpx; box-shadow: 0 6rpx 24rpx rgba(0, 0, 0, .05); }
.card image { width: 168rpx; height: 168rpx; border-radius: 20rpx; background: #f7f7f9; } .info { flex: 1; display: flex; flex-direction: column; }
.name { font-weight: 600; letter-spacing: -.01em; min-height: 72rpx; font-size: 29rpx; }
.price { font-weight: 700; margin: 10rpx 0; font-size: 34rpx; font-variant-numeric: tabular-nums; }
.price .yen, .amount .yen { font-size: 22rpx; font-weight: 600; margin-right: 2rpx; }
.qty { display: flex; justify-content: space-between; align-items: center; color: #86868b; margin-top: auto; }
.stepper { display: flex; align-items: center; background: #f5f5f7; border-radius: 980px; padding: 2rpx; } .stepper text { width: 64rpx; text-align: center; padding: 10rpx 0; font-weight: 600; } .stepper text:nth-child(2) { min-width: 52rpx; font-weight: 600; font-size: 28rpx; }
.remove { color: #a1a1a6; font-size: 24rpx; padding: 10rpx 0 10rpx 20rpx; }
.bar { position: fixed; left: 0; right: 0; bottom: 50px; display: flex; justify-content: space-between; align-items: center; background: rgba(255, 255, 255, .94); backdrop-filter: blur(20px) saturate(180%); border-top: 1rpx solid rgba(0, 0, 0, .05); padding: 20rpx 28rpx; }
.bar-total { color: #86868b; font-size: 26rpx; } .origin { text-decoration: line-through; color: #a1a1a6; margin-right: 10rpx; }
.promo { padding: 16rpx 26rpx; background: rgba(255, 59, 48, .07); border-top: 1rpx solid rgba(255, 59, 48, .12); }
.promo-line { display: flex; align-items: baseline; justify-content: space-between; }
.promo-label { color: #ff3b30; font-size: 26rpx; font-weight: 700; }
.promo-amount { color: #ff3b30; font-size: 34rpx; font-weight: 800; font-variant-numeric: tabular-nums; }
.promo-tags { display: flex; flex-wrap: wrap; gap: 10rpx; margin-top: 8rpx; }
.promo-item { color: #6e6e73; font-size: 22rpx; background: #fff; border-radius: 980px; padding: 4rpx 14rpx; }
.step { transition: opacity .15s ease; }
.step.disabled { opacity: .35; }
.amount { font-weight: 800; font-size: 38rpx; font-variant-numeric: tabular-nums; } .bar button { width: 240rpx; color: #fff; }
</style>
