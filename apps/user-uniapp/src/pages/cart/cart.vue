<template>
  <view class="page" :style="{ background: theme.background }">
    <view v-if="!items.length" class="empty"><text>购物车空空如也</text><button :style="{ background: theme.primary }" @tap="goHome">去逛逛</button></view>
    <view v-else class="body">
      <view v-for="item in items" :key="item.skuId" class="card"><image :src="item.mainImage || fallback" mode="aspectFill" /><view class="info"><view class="name">{{ item.productName }}</view><view class="price" :style="{ color: theme.primary }">¥{{ item.price }}</view><view class="qty"><view class="stepper"><text @tap="change(item, -1)">-</text><text>{{ item.quantity }}</text><text @tap="change(item, 1)">+</text></view><text class="remove" @tap="remove(item)">删除</text></view></view></view>
      <view class="bar safe-bottom"><view class="bar-total">合计：<text class="amount" :style="{ color: theme.primary }"><text class="yen">¥</text>{{ total }}</text></view><button :style="{ background: theme.primary }" @tap="checkout">去结算({{ count }})</button></view>
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
.page { min-height: 100vh; padding-bottom: 150rpx; } .empty { text-align: center; color: #86868b; padding-top: 180rpx; }
.empty button { margin: 28rpx auto; width: 240rpx; color: #fff; }
.card { display: flex; gap: 20rpx; background: #fff; margin: 20rpx 24rpx; border-radius: 24rpx; padding: 22rpx; box-shadow: 0 2rpx 12rpx rgba(0, 0, 0, .04); }
.card image { width: 160rpx; height: 160rpx; border-radius: 18rpx; background: #f5f5f7; } .info { flex: 1; }
.name { font-weight: 600; letter-spacing: -.01em; min-height: 70rpx; }
.price { font-weight: 700; margin: 8rpx 0; font-size: 32rpx; font-variant-numeric: tabular-nums; }
.price .yen, .amount .yen { font-size: 22rpx; font-weight: 600; margin-right: 2rpx; }
.qty { display: flex; justify-content: space-between; align-items: center; color: #86868b; }
.stepper { display: flex; align-items: center; background: #f5f5f7; border-radius: 980px; } .stepper text { width: 60rpx; text-align: center; padding: 8rpx 0; font-weight: 600; } .stepper text:nth-child(2) { min-width: 48rpx; font-weight: 500; }
.remove { color: #86868b; font-size: 24rpx; }
.bar { position: fixed; left: 0; right: 0; bottom: 50px; display: flex; justify-content: space-between; align-items: center; background: rgba(255, 255, 255, .92); backdrop-filter: blur(20px); border-top: 1rpx solid rgba(0, 0, 0, .06); padding: 18rpx 26rpx; }
.bar-total { color: #86868b; font-size: 26rpx; }
.amount { font-weight: 800; font-size: 38rpx; font-variant-numeric: tabular-nums; } .bar button { width: 240rpx; color: #fff; }
</style>
