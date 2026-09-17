<template>
  <view class="page" :style="{ background: theme.background }">
    <!-- 顶部搜索：搜索优先的浏览结构 -->
    <view class="topbar">
      <view class="search">
        <text class="search-icon">🔍</text>
        <input v-model="keyword" placeholder="搜索商品" confirm-type="search" @confirm="reload" />
        <text class="search-action" :style="{ color: theme.primary }" @tap="reload">搜索</text>
      </view>
    </view>

    <view class="body">
      <scroll-view scroll-y class="left">
        <view v-for="item in categories" :key="item.id" :class="['menu', String(current) === String(item.id) && 'on']" :style="String(current) === String(item.id) ? { color: theme.primary, background: theme.primary + '12' } : {}" @tap="select(item)">{{ item.name }}</view>
      </scroll-view>
      <scroll-view scroll-y class="right">
        <view class="result-head">
          <text class="result-title">{{ keyword ? `“${keyword}”` : (currentName || '全部商品') }}</text>
          <text class="result-count">{{ products.length }} 件</text>
        </view>
        <view class="sort-bar">
          <text :class="['sort-item', sort === '' && 'on']" :style="sort === '' ? { color: theme.primary } : {}" @tap="setSort('')">综合</text>
          <text :class="['sort-item', sort !== '' && 'on']" :style="sort !== '' ? { color: theme.primary } : {}" @tap="togglePrice">价格 {{ sort === 'asc' ? '↑' : sort === 'desc' ? '↓' : '↕' }}</text>
        </view>
        <view v-for="item in sortedProducts" :key="item.id" class="card" @tap="go(item)">
          <image :src="item.mainImage || fallback" mode="aspectFill" />
          <view class="info">
            <view class="name">{{ item.name }}</view>
            <view class="price-row">
              <view class="price" :style="{ color: theme.primary }"><text class="yen">¥</text>{{ price(item) }}</view>
              <text class="buy-dot" :style="{ color: theme.primary, background: theme.primary + '14' }">＋</text>
            </view>
          </view>
        </view>
        <view v-if="!products.length && !loading" class="empty"><text class="emoji">🧺</text><text>该分类暂无商品</text></view>
      </scroll-view>
    </view>
  </view>
</template>

<script setup>
import { computed, ref } from 'vue'
import { onLoad, onShow } from '@dcloudio/uni-app'
import { get } from '@/common/request'
import { getTheme } from '@/common/store'

const fallback = '/static/placeholder.png'
const theme = ref(getTheme()); const categories = ref([]); const current = ref(0); const products = ref([]); const loading = ref(false)
const keyword = ref('')

const currentName = computed(() => current.value ? (categories.value.find(item => String(item.id) === String(current.value))?.name || '') : '')
// 排序：价格升/降为客户端排序（平台暂无销量数据，故不提供销量排序）。
const sort = ref('')
const sortedProducts = computed(() => {
  if (!sort.value) return products.value
  const list = [...products.value]
  list.sort((a, b) => sort.value === 'asc' ? Number(price(a)) - Number(price(b)) : Number(price(b)) - Number(price(a)))
  return list
})
const setSort = value => { sort.value = value }
const togglePrice = () => { sort.value = sort.value === 'asc' ? 'desc' : 'asc' }
const price = item => (item.skus || []).length ? Math.min(...item.skus.map(sku => Number(sku.price))).toFixed(2) : '--'
const loadProducts = async () => {
  loading.value = true
  try {
    const data = await get('/products/List', { categoryId: current.value, keyword: keyword.value, status: 1, page: 1, pageSize: 50 })
    products.value = data.items || []
  } finally { loading.value = false }
}
const reload = () => { if (keyword.value) current.value = 0; loadProducts() }
const select = item => { keyword.value = ''; current.value = item.id; loadProducts() }
const go = item => item.skus?.length ? uni.navigateTo({ url: `/pages/product/detail?id=${item.id}` }) : uni.showToast({ title: '商品缺少SKU', icon: 'none' })
onLoad(options => { current.value = Number(options.id || 0); keyword.value = options.keyword || '' })
onShow(async () => {
  theme.value = getTheme()
  if (!categories.value.length) {
    const tree = await get('/products/GetCategoryTree') || []
    // 顶部固定"全部"，避免默认落在无商品的分类上出现空屏。
    categories.value = [{ id: 0, name: '全部' }, ...tree.flatMap(item => [item, ...(item.children || [])]).filter(item => item.isActive !== false)]
  }
  await loadProducts()
})
</script>

<style scoped>
.page { display: flex; flex-direction: column; height: 100vh; }
.topbar { padding: 18rpx 24rpx 14rpx; background: #fff; }
.search { display: flex; align-items: center; gap: 12rpx; background: #f5f5f7; height: 74rpx; border-radius: 980px; padding: 0 26rpx; }
.search-icon { font-size: 25rpx; opacity: .45; }
.search input { flex: 1; height: 100%; font-size: 27rpx; }
.search-action { font-weight: 600; font-size: 27rpx; }
.body { flex: 1; display: flex; min-height: 0; }
.left { width: 196rpx; background: #fff; padding: 8rpx 0; box-sizing: border-box; }
.menu { margin: 6rpx 14rpx; padding: 26rpx 12rpx; text-align: center; font-size: 26rpx; color: #6e6e73; border-radius: 18rpx; transition: all .15s ease; }
.menu.on { font-weight: 700; }
.right { flex: 1; height: 100%; padding: 20rpx; box-sizing: border-box; }
.result-head { display: flex; align-items: baseline; justify-content: space-between; padding: 4rpx 6rpx 18rpx; }
.result-title { font-size: 30rpx; font-weight: 700; letter-spacing: -.01em; }
.result-count { font-size: 22rpx; color: #a1a1a6; }
.sort-bar { display: flex; gap: 32rpx; padding: 0 6rpx 18rpx; }
.sort-item { font-size: 25rpx; color: #6e6e73; }
.sort-item.on { font-weight: 700; }
.card { display: flex; gap: 20rpx; background: #fff; border-radius: 26rpx; overflow: hidden; margin-bottom: 20rpx; border: 1rpx solid rgba(60, 60, 67, .06); box-shadow: 0 8rpx 24rpx rgba(0, 0, 0, .05); }
.card image { width: 190rpx; height: 190rpx; background: #f7f7f9; }
.card .info { flex: 1; padding: 20rpx 20rpx 20rpx 0; display: flex; flex-direction: column; justify-content: space-between; }
.name { font-weight: 600; letter-spacing: -.01em; font-size: 28rpx; line-height: 38rpx; display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical; overflow: hidden; }
.price-row { display: flex; align-items: center; justify-content: space-between; }
.price { font-weight: 800; font-variant-numeric: tabular-nums; font-size: 34rpx; }
.price .yen { font-size: 22rpx; font-weight: 700; margin-right: 2rpx; }
.buy-dot { width: 46rpx; height: 46rpx; border-radius: 50%; display: grid; place-items: center; font-size: 28rpx; font-weight: 700; }
.empty { display: grid; justify-items: center; gap: 16rpx; color: #a1a1a6; padding-top: 140rpx; font-size: 26rpx; }
.empty .emoji { font-size: 92rpx; line-height: 1; }
</style>
