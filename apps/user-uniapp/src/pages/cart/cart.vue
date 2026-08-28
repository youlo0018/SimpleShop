<template>
  <view class="page" :style="{ background: theme.background }">
    <view v-if="!items.length" class="empty"><text>购物车空空如也</text><button :style="{ background: theme.primary }" @tap="goHome">去逛逛</button></view>
    <view v-else class="body">
      <view v-for="item in items" :key="item.skuId" class="card"><image :src="item.mainImage || fallback" mode="aspectFill" /><view class="info"><view class="name">{{ item.productName }}</view><view class="price" :style="{ color: theme.primary }">¥{{ item.price }}</view><view class="qty"><view class="stepper"><text @tap="change(item, -1)">-</text><text>{{ item.quantity }}</text><text @tap="change(item, 1)">+</text></view><text class="remove" @tap="remove(item)">删除</text></view></view></view>
      <view class="bar safe-bottom"><view>合计：<text class="amount" :style="{ color: theme.primary }">¥{{ total }}</text></view><button :style="{ background: theme.primary }" @tap="checkout">去结算({{ count }})</button></view>
    </view>
  </view>
</template>

<script setup>
import { computed, ref } from 'vue'
import { onShow } from '@dcloudio/uni-app'
import { get, post } from '@/common/request'
import { getTheme, requireLogin } from '@/common/store'

const fallback = 'https://dummyimage.com/300x300/edf2f7/94a3b8&text=SKU'
const theme = ref(getTheme()); const items = ref([])
const total = computed(() => items.value.filter(item => item.checked).reduce((sum, item) => sum + item.price * item.quantity, 0).toFixed(2))
const count = computed(() => items.value.filter(item => item.checked).reduce((sum, item) => sum + item.quantity, 0))

const load = async () => {
  if (!requireLogin()) return
  const data = await get('/carts/Get')
  items.value = (data.items || []).map(item => ({ ...item, checked: true, quantity: Number(item.quantity), price: Number(item.price) }))
}
const change = async (item, delta) => {
  const quantity = item.quantity + delta
  if (quantity < 1) return remove(item)
  await post('/carts/Add', { ...item, quantity, checked: undefined })
  load()
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
.page { min-height: 100vh; padding-bottom: 150rpx; } .empty { text-align: center; color: #667085; padding-top: 180rpx; }
.empty button { margin: 28rpx auto; width: 240rpx; color: #fff; }
.card { display: flex; gap: 18rpx; background: #fff; margin: 20rpx; border-radius: 20rpx; padding: 20rpx; }
.card image { width: 160rpx; height: 160rpx; border-radius: 12rpx; } .info { flex: 1; }
.name { font-weight: 600; min-height: 70rpx; } .price { font-weight: 800; margin: 8rpx 0; }
.qty { display: flex; justify-content: space-between; color: #667085; } .stepper { display: flex; gap: 22rpx; } .remove { color: #98a2b3; }
.bar { position: fixed; left: 0; right: 0; bottom: 50px; display: flex; justify-content: space-between; align-items: center; background: #fff; padding: 20rpx; }
.amount { font-weight: 800; font-size: 34rpx; } .bar button { width: 220rpx; color: #fff; }
</style>
