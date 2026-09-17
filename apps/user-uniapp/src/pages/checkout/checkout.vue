<template>
  <view class="page">
    <view class="card" @tap="selectAddress"><view v-if="address"><b>{{ address.receiverName }} {{ address.receiverPhone }}</b><view class="muted">{{ fullAddress(address) }}</view></view><view v-else class="center">请选择收货地址 +</view></view>
    <view class="card"><view v-for="item in items" :key="item.skuId" class="item"><image :src="item.mainImage || fallback" mode="aspectFill" /><view class="goods"><view>{{ item.productName }}</view><view class="muted">¥{{ item.price }} × {{ item.quantity }}<text v-if="itemDiscount(item.skuId) > 0" class="item-discount"> 已优惠 ¥{{ itemDiscount(item.skuId).toFixed(2) }}</text></view></view><b>¥{{ (item.price * item.quantity - itemDiscount(item.skuId)).toFixed(2) }}</b></view></view>

    <view v-if="settle && ((settle.coupons || []).length || (settle.activities || []).length)" class="card">
      <view class="section-title">优惠</view>
      <view v-for="coupon in settle.coupons || []" :key="coupon.userCouponId" class="promo-row" @tap="toggleCoupon(coupon.userCouponId)">
        <text class="tick" :class="{ on: selectedCouponIds.includes(coupon.userCouponId) }" :style="selectedCouponIds.includes(coupon.userCouponId) ? { background: theme.primary, borderColor: theme.primary } : {}">{{ selectedCouponIds.includes(coupon.userCouponId) ? '✓' : '' }}</text>
        <text class="promo-name">券：{{ coupon.name }}</text>
        <text class="promo-value">-¥{{ Number(coupon.estimatedDiscount).toFixed(2) }}</text>
      </view>
      <view v-for="activity in settle.activities || []" :key="activity.activityId" class="promo-row static">
        <text class="tick activity">活动</text>
        <text class="promo-name">{{ activity.name }}</text>
        <text class="promo-value">{{ Number(activity.estimatedDiscount) > 0 ? '-¥' + Number(activity.estimatedDiscount).toFixed(2) : '赠券' }}</text>
      </view>
    </view>

    <view class="card summary-card">
      <view class="summary-row"><text>商品金额</text><text>¥{{ amount }}</text></view>
      <view class="summary-row" :class="{ highlight: totalDiscount > 0 }">
        <text>优惠合计</text>
        <text class="discount-value">-¥{{ totalDiscount.toFixed(2) }}</text>
      </view>
      <view class="summary-row pay-row"><text>实付金额</text><text class="pay-amount">¥{{ payable }}</text></view>
      <view v-if="totalDiscount <= 0" class="muted summary-tip">暂无满足条件的活动或优惠券</view>
    </view>

    <view class="bar safe-bottom">
      <view class="bar-total">
        <text v-if="totalDiscount > 0" class="origin">¥{{ amount }}</text>
        合计：<text class="amount"><text class="yen">¥</text>{{ payable }}</text>
        <text v-if="totalDiscount > 0" class="discount-tip">已优惠 ¥{{ totalDiscount.toFixed(2) }}</text>
      </view>
      <button class="submit" :style="{ background: theme.primary }" :disabled="submitting" @tap="submit">{{ submitting ? '支付中' : '提交并支付' }}</button>
    </view>
  </view>
</template>

<script setup>
import { onLoad, onShow } from '@dcloudio/uni-app'
import { computed, ref } from 'vue'
import { post } from '@/common/request'
import { getUser, getUserId, getTheme, requireLogin } from '@/common/store'
import { isPhone, toast, validateQuantity } from '@/common/validators'

const fallback = '/static/placeholder.png'
const theme = ref(getTheme()); const items = ref([]); const address = ref(null); const submitting = ref(false)
const settle = ref(null); const selectedCouponIds = ref([])
const amount = computed(() => items.value.reduce((sum, item) => sum + Number(item.price) * Number(item.quantity), 0).toFixed(2))
const totalDiscount = computed(() => Number(settle.value?.totalDiscount || 0))
const payable = computed(() => (Number(amount.value) - totalDiscount.value).toFixed(2))
const itemDiscount = skuId => {
  const hit = (settle.value?.items || []).find(item => String(item.skuId) === String(skuId))
  return Number(hit?.discountAmount || 0)
}
const fullAddress = item => `${item.province || ''}${item.city || ''}${item.district || ''}${item.detail}`
const selectAddress = () => uni.navigateTo({ url: '/pages/address/address?from=checkout' })

// 提交页由用户勾选用券：auto=true 表示按全部可用券结算，之后按勾选集合重算。
const refreshPreview = async auto => {
  if (!items.value.length) return
  const data = await post('/marketing/SettlePreview', {
    platformId: items.value[0].platformId,
    selectedUserCouponIds: auto ? null : selectedCouponIds.value,
    items: items.value.map(item => ({
      skuId: item.skuId, platformId: item.platformId, merchantId: item.merchantId,
      productName: item.productName, price: Number(item.price), quantity: Number(item.quantity)
    }))
  })
  settle.value = data
  if (auto) selectedCouponIds.value = (data.coupons || []).map(coupon => coupon.userCouponId)
}
const toggleCoupon = async couponId => {
  selectedCouponIds.value = selectedCouponIds.value.includes(couponId)
    ? selectedCouponIds.value.filter(id => id !== couponId)
    : [...selectedCouponIds.value, couponId]
  await refreshPreview(false)
}

const submit = async () => {
  if (!requireLogin()) return
  if (!address.value) return uni.showToast({ title: '请选择收货地址', icon: 'none' })
  if (!items.value.length) return uni.showToast({ title: '结算商品为空', icon: 'none' })
  // 提交前与后端 CreateOrderValidator 对齐：手机号、价格与数量必须合法，避免把必然 400 的请求发出去。
  if (!isPhone(address.value.receiverPhone)) return toast('收货手机号格式不正确')
  for (const item of items.value) {
    if (!(Number(item.price) > 0)) return toast('商品价格异常，请重新加购')
    if (!validateQuantity(item.quantity)) return
  }
  submitting.value = true
  try {
    const user = getUser(); const first = items.value[0]
    const order = await post('/orders/Create', {
      idempotencyKey: `${Date.now()}-${Math.random().toString(36).slice(2)}`,
      platformId: first.platformId, customerId: getUserId(), customerNo: `U${getUserId()}`,
      customerName: user.userName || '商城用户', receiverName: address.value.receiverName,
      receiverPhone: address.value.receiverPhone, receiverAddress: fullAddress(address.value),
      selectedUserCouponIds: selectedCouponIds.value,
      items: items.value.map(item => ({ skuId: item.skuId, platformId: item.platformId, merchantId: item.merchantId, productName: item.productName, price: Number(item.price), quantity: Number(item.quantity) })),
      stockItems: items.value.map(item => ({ skuId: item.skuId, quantity: Number(item.quantity) }))
    })
    if (order.success === false) throw new Error(order.message || '下单失败')
    // 支付金额以订单服务端结算后的实付为准（含优惠），不能再传前端原价合计。
    await post('/payments/Create', { bizNo: order.orderNo, platformId: first.platformId, merchantId: first.merchantId, userId: getUserId(), amount: Number(order.paymentPrice), items: items.value.map(item => ({ skuId: item.skuId, quantity: Number(item.quantity) })) })
    await post('/payments/Confirm', { bizNo: order.orderNo, items: items.value.map(item => ({ skuId: item.skuId, quantity: Number(item.quantity) })) })
    uni.showToast({ title: '支付成功' })
    setTimeout(() => uni.redirectTo({ url: '/pages/orders/orders' }), 700)
  } finally { submitting.value = false }
}
onLoad(async options => {
  // 购物车项带 image 字段、立即购买带 mainImage，这里统一成显示字段。
  items.value = (uni.getStorageSync('checkout') || []).map(item => ({ ...item, mainImage: item.mainImage || item.image || '' }))
  requireLogin()
  await refreshPreview(true)
})
onShow(() => { theme.value = getTheme(); address.value = uni.getStorageSync('selectedAddress') || null })
</script>

<style scoped>.page { min-height: 100vh; box-sizing: border-box; padding-bottom: 220rpx; }
.card { background: #fff; border-radius: 28rpx; margin: 22rpx 24rpx; padding: 30rpx; box-shadow: 0 6rpx 24rpx rgba(0, 0, 0, .05); } .muted { color: #86868b; font-size: 24rpx; margin-top: 10rpx; } .center { color: #0071e3; text-align: center; font-weight: 500; }
.item { display: flex; gap: 18rpx; margin-bottom: 18rpx; align-items: center; } .item image { width: 110rpx; height: 110rpx; border-radius: 16rpx; background: #f5f5f7; } .goods { flex: 1; }
.bar { position: fixed; left: 0; right: 0; bottom: 0; display: flex; justify-content: space-between; align-items: center; background: rgba(255, 255, 255, .92); backdrop-filter: blur(20px); border-top: 1rpx solid rgba(0, 0, 0, .06); padding: 18rpx 26rpx; }
.bar-total { color: #86868b; font-size: 26rpx; }
.amount { color: #1d1d1f; font-weight: 800; font-size: 38rpx; font-variant-numeric: tabular-nums; }
.origin { text-decoration: line-through; color: #a1a1a6; margin-right: 10rpx; font-size: 24rpx; }
.discount-tip { color: #ff3b30; font-size: 22rpx; margin-left: 10rpx; }
.section-title { font-weight: 700; margin-bottom: 14rpx; }
.promo-row { display: flex; align-items: center; gap: 14rpx; padding: 12rpx 0; border-top: 1rpx solid #f2f2f7; }
.promo-row.static { opacity: .9; }
.tick { width: 36rpx; height: 36rpx; border-radius: 50%; border: 2rpx solid #c7c7cc; color: #fff; font-size: 22rpx; display: flex; align-items: center; justify-content: center; }
.tick.on { background: #0071e3; border-color: #0071e3; }
.tick.activity { width: auto; height: 36rpx; border-radius: 8rpx; border: 0; background: #fff7e6; color: #ff9500; padding: 0 10rpx; }
.promo-name { flex: 1; font-size: 26rpx; }
.promo-value { color: #ff3b30; font-size: 30rpx; font-weight: 800; font-variant-numeric: tabular-nums; }
.summary-card { padding: 26rpx 30rpx; }
.summary-row { display: flex; justify-content: space-between; padding: 10rpx 0; font-size: 27rpx; color: #3a3a3c; }
.summary-row .discount-value { color: #ff3b30; font-weight: 700; }
.summary-row.highlight { color: #1d1d1f; }
.pay-row { border-top: 1rpx solid #f2f2f7; margin-top: 8rpx; padding-top: 18rpx; font-weight: 600; }
.pay-amount { color: #ff3b30; font-size: 38rpx; font-weight: 800; font-variant-numeric: tabular-nums; }
.summary-tip { margin-top: 6rpx; }
.item-discount { color: #ff3b30; margin-left: 8rpx; } .amount .yen { font-size: 24rpx; font-weight: 600; margin-right: 2rpx; } .submit { width: 260rpx; color: #fff; box-shadow: 0 8rpx 22rpx rgba(0, 113, 227, .28); }</style>
