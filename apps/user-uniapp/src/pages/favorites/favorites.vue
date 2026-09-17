<template>
  <view class="page">
    <view v-if="!items.length" class="empty"><text class="emoji">⭐</text><text>还没有收藏的商品</text><text class="empty-sub">看到喜欢的商品点个收藏吧</text></view>
    <view v-else class="grid">
      <view v-for="item in items" :key="item.id" class="product" @tap="go(item)">
        <image :src="item.mainImage || fallback" mode="aspectFill" />
        <view class="pinfo">
          <view class="pname">{{ item.name }}</view>
          <view class="price-row">
            <view class="price" :style="{ color: theme.primary }"><text class="yen">¥</text>{{ minPrice(item) }}</view>
            <text class="remove" @tap.stop="remove(item)">取消</text>
          </view>
        </view>
      </view>
    </view>
  </view>
</template>

<script setup>
import { ref } from 'vue'
import { onShow } from '@dcloudio/uni-app'
import { get, post } from '@/common/request'
import { getTheme, requireLogin } from '@/common/store'

const fallback = '/static/placeholder.png'
const theme = ref(getTheme()); const items = ref([])

const minPrice = item => (item.skus || []).length ? Math.min(...item.skus.map(sku => Number(sku.price))).toFixed(2) : '--'
const go = item => item.skus?.length ? uni.navigateTo({ url: `/pages/product/detail?id=${item.id}` }) : uni.showToast({ title: '商品缺少SKU', icon: 'none' })
const remove = async item => {
  await post('/users/ToggleFavorite', { userId: 0, productId: item.id })
  uni.showToast({ title: '已取消收藏' })
  load()
}
// 收藏只存商品 ID：逐个取详情（上限 20 个，避免首屏请求过多）。
const load = async () => {
  if (!requireLogin()) return
  const favorites = await get('/users/Favorites') || []
  const details = await Promise.all(favorites.slice(0, 20).map(item =>
    get('/products/GetProductDetail', { id: item.productId }).catch(() => null)))
  items.value = details.filter(Boolean).map(data => ({ ...data.product, skus: data.skus || [] }))
}
onShow(() => { theme.value = getTheme(); load() })
</script>

<style scoped>
.page { min-height: 100vh; padding: 24rpx; box-sizing: border-box; background: #f5f5f7; }
.empty { display: grid; justify-items: center; gap: 16rpx; color: #a1a1a6; padding-top: 200rpx; font-size: 28rpx; }
.empty .emoji { font-size: 104rpx; line-height: 1; }
.empty .empty-sub { font-size: 24rpx; color: #c7c7cc; }
.grid { display: grid; grid-template-columns: repeat(2, 1fr); gap: 22rpx; }
.product { background: #fff; border-radius: 26rpx; overflow: hidden; border: 1rpx solid rgba(60, 60, 67, .06); box-shadow: 0 8rpx 24rpx rgba(0, 0, 0, .05); }
.product image { width: 100%; height: 330rpx; background: #f7f7f9; }
.pinfo { padding: 20rpx 22rpx 24rpx; }
.pname { height: 72rpx; overflow: hidden; font-weight: 600; font-size: 27rpx; line-height: 36rpx; display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical; }
.price-row { display: flex; align-items: center; justify-content: space-between; margin-top: 14rpx; }
.price { font-weight: 800; font-size: 34rpx; font-variant-numeric: tabular-nums; }
.price .yen { font-size: 23rpx; font-weight: 700; margin-right: 2rpx; }
.remove { color: #a1a1a6; font-size: 23rpx; padding: 8rpx 0 8rpx 16rpx; }
</style>
