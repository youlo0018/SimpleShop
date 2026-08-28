<template>
  <view class="page" :style="{ background: theme.background }">
    <scroll-view scroll-y class="left"><view v-for="item in categories" :key="item.id" :class="['menu', current === item.id && 'on']" :style="current === item.id && { color: theme.primary, borderColor: theme.primary }" @tap="select(item)">{{ item.name }}</view></scroll-view>
    <scroll-view scroll-y class="right">
      <view v-for="item in products" :key="item.id" class="card" @tap="go(item)"><image :src="item.mainImage || fallback" mode="aspectFill" /><view><view class="name">{{ item.name }}</view><view class="price" :style="{ color: theme.primary }">¥{{ price(item) }}</view></view></view>
      <view v-if="!products.length && !loading" class="empty">该分类暂无商品</view>
    </scroll-view>
  </view>
</template>

<script setup>
import { onLoad, onShow } from '@dcloudio/uni-app'
import { ref } from 'vue'
import { get } from '@/common/request'
import { getTheme } from '@/common/store'

const fallback = 'https://dummyimage.com/600x600/edf2f7/94a3b8&text=Shop'
const theme = ref(getTheme()); const categories = ref([]); const current = ref(0); const products = ref([]); const loading = ref(false)
const keyword = ref('')

const price = item => (item.skus || []).length ? Math.min(...item.skus.map(sku => Number(sku.price))).toFixed(2) : '--'
const loadProducts = async () => {
  loading.value = true
  try {
    const data = await get('/products/List', { categoryId: current.value, keyword: keyword.value, status: 1, page: 1, pageSize: 50 })
    products.value = data.items || []
  } finally { loading.value = false }
}
const select = item => { current.value = item.id; loadProducts() }
const go = item => item.skus?.length ? uni.navigateTo({ url: `/pages/product/detail?id=${item.id}` }) : uni.showToast({ title: '商品缺少SKU', icon: 'none' })
onLoad(options => { current.value = Number(options.id || 0); keyword.value = options.keyword || '' })
onShow(async () => {
  theme.value = getTheme()
  if (!categories.value.length) {
    const tree = await get('/products/GetCategoryTree') || []
    categories.value = tree.flatMap(item => [item, ...(item.children || [])]).filter(item => item.isActive !== false)
    if (!current.value && !keyword.value) current.value = categories.value[0]?.id || 0
  }
  await loadProducts()
})
</script>

<style scoped>
.page { display: flex; height: 100vh; } .left { width: 198rpx; background: #fff; }
.menu { padding: 32rpx 18rpx; text-align: center; font-size: 26rpx; border-left: 6rpx solid transparent; }
.menu.on { background: #f8fafc; font-weight: 700; }
.right { flex: 1; height: 100%; padding: 22rpx; box-sizing: border-box; }
.card { display: flex; gap: 18rpx; background: #fff; border-radius: 18rpx; overflow: hidden; margin-bottom: 18rpx; }
.card image { width: 170rpx; height: 170rpx; } .name { font-weight: 600; padding: 18rpx; min-height: 72rpx; } .price { padding: 0 18rpx; font-weight: 800; }
.empty { text-align: center; color: #667085; padding-top: 80rpx; }
</style>
