<template>
  <view class="page">
    <view class="card" @tap="selectAddress"><view v-if="address"><b>{{ address.receiverName }} {{ address.receiverPhone }}</b><view class="muted">{{ fullAddress(address) }}</view></view><view v-else class="center">请选择收货地址 +</view></view>
    <view class="card"><view v-for="item in items" :key="item.skuId" class="item"><image :src="item.mainImage || fallback" mode="aspectFill" /><view class="goods"><view>{{ item.productName }}</view><view class="muted">¥{{ item.price }} × {{ item.quantity }}</view></view><b>¥{{ (item.price * item.quantity).toFixed(2) }}</b></view></view>
    <view class="bar safe-bottom"><view>合计：<text class="amount">¥{{ amount }}</text></view><button class="submit" :disabled="submitting" @tap="submit">{{ submitting ? '支付中' : '提交并支付' }}</button></view>
  </view>
</template>

<script setup>
import { onLoad, onShow } from '@dcloudio/uni-app'
import { computed, ref } from 'vue'
import { post } from '@/common/request'
import { getUser, getUserId, requireLogin } from '@/common/store'

const fallback = 'https://dummyimage.com/300x300/edf2f7/94a3b8&text=SKU'
const items = ref([]); const address = ref(null); const submitting = ref(false)
const amount = computed(() => items.value.reduce((sum, item) => sum + Number(item.price) * Number(item.quantity), 0).toFixed(2))
const fullAddress = item => `${item.province || ''}${item.city || ''}${item.district || ''}${item.detail}`
const selectAddress = () => uni.navigateTo({ url: '/pages/address/address?from=checkout' })

const submit = async () => {
  if (!requireLogin()) return
  if (!address.value) return uni.showToast({ title: '请选择收货地址', icon: 'none' })
  if (!items.value.length) return uni.showToast({ title: '结算商品为空', icon: 'none' })
  submitting.value = true
  try {
    const user = getUser(); const first = items.value[0]
    const order = await post('/orders/Create', {
      idempotencyKey: `${Date.now()}-${Math.random().toString(36).slice(2)}`,
      platformId: first.platformId, customerId: getUserId(), customerNo: `U${getUserId()}`,
      customerName: user.userName || '商城用户', receiverName: address.value.receiverName,
      receiverPhone: address.value.receiverPhone, receiverAddress: fullAddress(address.value),
      items: items.value.map(item => ({ skuId: item.skuId, platformId: item.platformId, merchantId: item.merchantId, productName: item.productName, price: Number(item.price), quantity: Number(item.quantity) })),
      stockItems: items.value.map(item => ({ skuId: item.skuId, quantity: Number(item.quantity) }))
    })
    if (order.success === false) throw new Error(order.message || '下单失败')
    await post('/payments/Create', { bizNo: order.orderNo, platformId: first.platformId, merchantId: first.merchantId, userId: getUserId(), amount: Number(amount.value), items: items.value.map(item => ({ skuId: item.skuId, quantity: Number(item.quantity) })) })
    await post('/payments/Confirm', { bizNo: order.orderNo, items: items.value.map(item => ({ skuId: item.skuId, quantity: Number(item.quantity) })) })
    uni.showToast({ title: '支付成功' })
    setTimeout(() => uni.redirectTo({ url: '/pages/orders/orders' }), 700)
  } finally { submitting.value = false }
}
onLoad(options => { items.value = uni.getStorageSync('checkout') || []; requireLogin() })
onShow(() => { address.value = uni.getStorageSync('selectedAddress') || null })
</script>

<style scoped>.card { background: #fff; border-radius: 20rpx; margin: 20rpx; padding: 26rpx; } .muted { color: #667085; font-size: 24rpx; margin-top: 10rpx; } .center { color: #ff4d6d; text-align: center; }
.item { display: flex; gap: 18rpx; margin-bottom: 18rpx; align-items: center; } .item image { width: 110rpx; height: 110rpx; border-radius: 12rpx; } .goods { flex: 1; }
.bar { position: fixed; left: 0; right: 0; bottom: 0; display: flex; justify-content: space-between; align-items: center; background: #fff; padding: 20rpx; }
.amount { color: #ff4d6d; font-weight: 800; font-size: 34rpx; } .submit { width: 250rpx; background: #ff4d6d; color: #fff; }</style>
