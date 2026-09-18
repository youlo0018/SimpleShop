<template>
  <view class="page">
    <!-- 顶部：商场特惠 + 搜索（参考图商城页） -->
    <view class="topbar">
      <view class="market-title"><text class="bell">🔔</text><text>商场特惠</text></view>
      <view class="search">
        <text class="search-icon">🔍</text>
        <input v-model="keyword" placeholder="搜索商品" confirm-type="search" @confirm="reload" />
        <text class="search-action" @tap="reload">搜索</text>
      </view>
    </view>

    <!-- 分类 chips -->
    <scroll-view scroll-x class="chips">
      <text :class="['chip', current === 0 && 'on']" @tap="select({ id: 0 })">推荐</text>
      <text v-for="category in categories" :key="category.id" :class="['chip', String(current) === String(category.id) && 'on']" @tap="select(category)">{{ category.name }}</text>
    </scroll-view>

    <view v-if="keyword" class="result-head"><text class="result-title">“{{ keyword }}” 的搜索结果</text><text class="result-count">{{ products.length }} 件</text></view>

    <!-- 商场团购（参考图横滑卡片） -->
    <view v-if="!keyword && groupProducts.length" class="section">
      <view class="section-head"><text class="section-title">商场团购</text><text class="section-more">查看更多 ›</text></view>
      <scroll-view scroll-x class="group-scroll">
        <view v-for="item in groupProducts" :key="item.id" class="group-card" @tap="go(item)">
          <image :src="item.mainImage || fallback" mode="aspectFill" />
          <view class="group-title"><text class="group-tag">自营</text><text class="group-name">{{ item.name }}</text></view>
          <view class="group-price"><text class="yen">¥</text>{{ deal(item).finalPrice }}</view>
        </view>
      </scroll-view>
    </view>

    <!-- 为您推荐（参考图三列） -->
    <view class="section">
      <view class="section-head"><text class="section-title">{{ keyword ? '商品' : '为您推荐' }}</text><text class="section-more">查看更多 ›</text></view>
      <view v-if="!sortedProducts.length && !loading" class="empty"><text class="emoji">🧺</text><text>暂无相关商品</text></view>
      <view v-else class="grid3">
        <view v-for="item in sortedProducts" :key="item.id" class="product" @tap="go(item)">
          <image :src="item.mainImage || fallback" mode="aspectFill" />
          <view class="pname">{{ item.name }}</view>
          <view class="price"><text class="yen">¥</text>{{ deal(item).finalPrice }}</view>
        </view>
      </view>
    </view>

    <!-- 商场活动（参考图：大图活动卡 + 时间） -->
    <view v-if="!keyword && activities.length" class="section">
      <view class="section-head"><text class="section-title">商场活动</text><text class="section-more">查看更多 ›</text></view>
      <view v-for="item in activities" :key="item.key" class="activity-card" @tap="goActivity">
        <view class="activity-image" :style="{ background: theme.primary + '16' }"><text class="activity-icon">🎁</text></view>
        <view class="activity-name">{{ item.name }}</view>
        <view class="activity-time">活动时间：{{ item.timeText }}</view>
      </view>
    </view>
  </view>
</template>

<script setup>
import { computed, ref } from 'vue'
import { onLoad, onShow } from '@dcloudio/uni-app'
import { get } from '@/common/request'
import { getPlatform, getTheme } from '@/common/store'
import { enrichFinalPrices, priceInfo } from '@/common/final-price'

const fallback = '/static/placeholder.png'
const theme = ref(getTheme()); const categories = ref([]); const current = ref(0); const products = ref([]); const loading = ref(false)
const keyword = ref(''); const activities = ref([])

const deal = item => priceInfo(item)
const groupProducts = computed(() => products.value.slice(0, 6))
const sortedProducts = computed(() => products.value)

const loadProducts = async () => {
  loading.value = true
  try {
    const data = await get('/products/List', { categoryId: current.value, keyword: keyword.value, status: 1, page: 1, pageSize: 50 })
    products.value = data.items || []
    await enrichFinalPrices(products.value, getPlatform()?.id || 0)
  } finally { loading.value = false }
}
const loadActivities = async () => {
  const data = await get('/marketing/ActiveActivities', { platformId: getPlatform()?.id || 0 }).catch(() => null)
  activities.value = (data?.activities || []).slice(0, 4).map(item => ({
    key: `a-${item.id}`, id: item.id, name: item.name,
    timeText: `${String(item.startAt || '').slice(0, 10)} ${String(item.startAt || '').slice(11, 16)} - ${item.endAt ? `${String(item.endAt).slice(0, 10)} ${String(item.endAt).slice(11, 16)}` : '长期'}`
  }))
}
const reload = () => { if (keyword.value) current.value = 0; loadProducts() }
const select = item => { keyword.value = ''; current.value = item.id; loadProducts() }
const go = item => item.skus?.length ? uni.navigateTo({ url: `/pages/product/detail?id=${item.id}` }) : uni.showToast({ title: '商品缺少SKU', icon: 'none' })
const goActivity = () => uni.navigateTo({ url: '/pages/coupon/center' })
onLoad(options => { current.value = Number(options.id || 0); keyword.value = options.keyword || '' })
onShow(async () => {
  theme.value = getTheme()
  if (!categories.value.length) {
    const tree = await get('/products/GetCategoryTree') || []
    // 顶部固定"全部"，避免默认落在无商品的分类上出现空屏。
    categories.value = tree.flatMap(item => [item, ...(item.children || [])]).filter(item => item.isActive !== false)
  }
  await Promise.all([loadProducts(), loadActivities()])
})
</script>

<style scoped>
.page { min-height: 100vh; padding-bottom: 60rpx; background: #f5f5f5; }
.topbar { background: #fff; padding: 20rpx 24rpx 16rpx; }
.market-title { display: flex; align-items: center; gap: 10rpx; font-size: 34rpx; font-weight: 800; letter-spacing: -.01em; color: #1a1a1a; }
.bell { font-size: 30rpx; }
.search { display: flex; align-items: center; gap: 12rpx; background: #f2f3f5; height: 72rpx; border-radius: 980px; padding: 0 26rpx; margin-top: 18rpx; }
.search-icon { font-size: 25rpx; opacity: .5; }
.search input { flex: 1; height: 100%; font-size: 27rpx; }
.search-action { font-weight: 600; font-size: 27rpx; color: #1a1a1a; }

.chips { white-space: nowrap; background: #fff; padding: 4rpx 20rpx 18rpx; }
.chip { display: inline-block; padding: 10rpx 8rpx 14rpx; margin-right: 34rpx; font-size: 28rpx; color: #4b5563; position: relative; }
.chip.on { color: #1a1a1a; font-weight: 700; }
.chip.on::after { content: ''; position: absolute; left: 50%; transform: translateX(-50%); bottom: 4rpx; width: 44rpx; height: 5rpx; border-radius: 5rpx; background: #e93b3d; }

.result-head { display: flex; align-items: baseline; justify-content: space-between; padding: 24rpx 30rpx 0; }
.result-title { font-size: 30rpx; font-weight: 700; }
.result-count { font-size: 23rpx; color: #9aa0a8; }

.section { margin-top: 26rpx; }
.section-head { display: flex; align-items: center; justify-content: space-between; padding: 0 30rpx 18rpx; }
.section-title { font-size: 34rpx; font-weight: 800; letter-spacing: -.01em; color: #1a1a1a; }
.section-more { font-size: 25rpx; color: #9aa0a8; }

/* 团购横滑 */
.group-scroll { white-space: nowrap; padding: 0 24rpx; }
.group-card { display: inline-block; width: 260rpx; margin-right: 16rpx; background: #fff; border-radius: 18rpx; overflow: hidden; vertical-align: top; box-shadow: 0 4rpx 14rpx rgba(0, 0, 0, .04); }
.group-card image { width: 260rpx; height: 260rpx; background: #f7f7f9; }
.group-title { display: flex; align-items: flex-start; gap: 8rpx; margin: 14rpx 16rpx 0; }
.group-tag { flex-shrink: 0; margin-top: 4rpx; background: #2f6bff; color: #fff; font-size: 19rpx; font-weight: 600; padding: 2rpx 10rpx; border-radius: 6rpx; }
.group-name { flex: 1; font-size: 25rpx; line-height: 34rpx; height: 68rpx; overflow: hidden; display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical; color: #1a1a1a; }
.group-price { margin: 10rpx 16rpx 18rpx; color: #e93b3d; font-size: 32rpx; font-weight: 800; }

/* 推荐三列 */
.grid3 { display: grid; grid-template-columns: repeat(3, 1fr); gap: 14rpx; padding: 0 24rpx; }
.product { background: #fff; border-radius: 16rpx; overflow: hidden; box-shadow: 0 4rpx 14rpx rgba(0, 0, 0, .04); }
.product image { width: 100%; height: 220rpx; background: #f7f7f9; }
.pname { padding: 12rpx 14rpx 0; font-size: 24rpx; line-height: 33rpx; height: 100rpx; overflow: hidden; display: -webkit-box; -webkit-line-clamp: 3; -webkit-box-orient: vertical; color: #1a1a1a; }
.price { padding: 10rpx 14rpx 16rpx; color: #e93b3d; font-size: 32rpx; font-weight: 800; font-variant-numeric: tabular-nums; }
.yen { font-size: 22rpx; font-weight: 700; margin-right: 2rpx; }

/* 商场活动 */
.activity-card { background: #fff; border-radius: 20rpx; margin: 0 24rpx 20rpx; overflow: hidden; box-shadow: 0 4rpx 14rpx rgba(0, 0, 0, .04); }
.activity-image { height: 260rpx; display: grid; place-items: center; }
.activity-icon { font-size: 72rpx; }
.activity-name { padding: 20rpx 24rpx 0; font-size: 30rpx; font-weight: 700; color: #1a1a1a; }
.activity-time { padding: 10rpx 24rpx 24rpx; font-size: 23rpx; color: #8a8f99; }

.empty { display: grid; justify-items: center; gap: 16rpx; color: #9aa0a8; padding: 120rpx 0; font-size: 26rpx; }
.empty .emoji { font-size: 84rpx; }
</style>
