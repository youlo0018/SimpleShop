<template>
  <view class="page">
    <!-- 顶部搜索（参考图：店铺页顶部搜索框） -->
    <view class="topbar">
      <view class="search" @tap="goSearch">
        <text class="search-icon">🔍</text>
        <text class="search-text">搜索</text>
      </view>
    </view>

    <!-- 店铺头：logo + 名称 + 评分 + 全部商品（参考图） -->
    <view class="shop-head">
      <view class="shop-logo">
        <image v-if="shop.logo" class="shop-logo-img" :src="shop.logo" mode="aspectFill" />
        <text v-else class="shop-logo-text">{{ (shop.merchantName || 'L').slice(0, 1).toUpperCase() }}</text>
      </view>
      <view class="shop-info">
        <view class="shop-name">{{ shop.merchantName || '店铺' }}</view>
        <view class="shop-meta">
          <text class="stars">★★★★★</text>
          <text class="score">4.9</text>
          <text class="divider">|</text>
          <text class="total">全部商品 {{ total }}</text>
        </view>
      </view>
      <view class="shop-cate" @tap="layout = layout === 'grid' ? 'list' : 'grid'"><text>☰</text></view>
    </view>

    <!-- 商品 / 活动 Tab（参考图居中下划线） -->
    <view class="tabs">
      <view :class="['tab', activeTab === 'products' && 'on']" @tap="activeTab = 'products'"><text>商品</text></view>
      <view :class="['tab', activeTab === 'activities' && 'on']" @tap="activeTab = 'activities'"><text>活动</text></view>
    </view>

    <template v-if="activeTab === 'products'">
      <!-- 分类 chips（推荐 + 本店商品分类，参考图） -->
      <scroll-view scroll-x class="chips">
        <text v-for="chip in chips" :key="chip.id" :class="['chip', currentCategory === chip.id && 'on']" @tap="selectCategory(chip.id)">{{ chip.name }}</text>
      </scroll-view>

      <!-- 优惠专区（参考图：大图活动卡 + 时间 + 进行中 + 进度条） -->
      <view v-if="promotions.length" class="section">
        <view class="section-head">
          <text class="section-title">优惠专区</text>
          <text class="section-more" @tap="activeTab = 'activities'">查看更多 ›</text>
        </view>
        <view class="promo-card" @tap="goPromotion(promotions[0])">
          <image class="promo-image" :src="promoImage" mode="aspectFill" />
          <view class="promo-body">
            <view class="promo-name">{{ promotions[0].name }}</view>
            <view class="promo-time">{{ promotions[0].timeText }}</view>
            <view class="promo-foot">
              <view class="promo-bar"><view class="promo-fill" :style="{ width: promotions[0].progress + '%' }" /></view>
              <text class="promo-status">进行中</text>
            </view>
          </view>
        </view>
      </view>

      <!-- 排序：销量 / 价格（参考图） -->
      <view class="sort-bar">
        <text :class="['sort', sort === 'sales' && 'on']" @tap="sort = 'sales'">销量</text>
        <text :class="['sort', sort.startsWith('price') && 'on']" @tap="togglePrice">价格<text class="arrows">{{ sort === 'priceAsc' ? '↑' : sort === 'priceDesc' ? '↓' : '⇅' }}</text></text>
      </view>

      <!-- 商品两列网格（参考图） -->
      <view v-if="!filteredProducts.length" class="empty"><text class="emoji">🧺</text><text>暂无相关商品</text></view>
      <view v-else class="grid">
        <view v-for="item in filteredProducts" :key="item.id" class="product" @tap="go(item)">
          <image class="product-image" :src="item.mainImage || fallback" mode="aspectFill" />
          <view class="product-name">{{ item.name }}</view>
          <view class="product-price">
            <text class="yen">¥</text><text class="amount">{{ deal(item).finalPrice }}</text>
            <text v-if="deal(item).hasDiscount" class="deal-tag">券后</text>
          </view>
        </view>
      </view>
    </template>

    <!-- 活动 Tab -->
    <template v-else>
      <view v-if="!promotions.length" class="empty"><text class="emoji">🎁</text><text>店铺暂无进行中活动</text></view>
      <view v-else class="activity-list">
        <view v-for="item in promotions" :key="item.key" class="activity-card" @tap="goPromotion(item)">
          <view class="activity-image" :style="{ background: theme.primary + '14' }"><text class="activity-icon">🎁</text></view>
          <view class="activity-info">
            <view class="activity-name">{{ item.name }}</view>
            <view class="activity-benefit" :style="{ color: '#e93b3d' }">{{ item.benefit }}</view>
            <view class="activity-time">{{ item.timeText }}</view>
          </view>
          <text class="activity-status">进行中</text>
        </view>
      </view>
    </template>

    <!-- 悬浮：购物车 + 分享（参考图右下角圆形按钮） -->
    <view class="fabs">
      <view class="fab" @tap="goCart"><text class="fab-icon">🛒</text><text v-if="cartCount > 0" class="fab-badge">{{ cartCount > 99 ? '99+' : cartCount }}</text></view>
      <view class="fab" @tap="shareHint"><text class="fab-icon">↗</text></view>
    </view>
  </view>
</template>

<script setup>
import { computed, ref } from 'vue'
import { onLoad } from '@dcloudio/uni-app'
import { get } from '@/common/request'
import { getPlatform, getTheme, isLogin } from '@/common/store'
import { enrichFinalPrices, priceInfo } from '@/common/final-price'

const fallback = '/static/placeholder.png'
const theme = ref(getTheme()); const merchantId = ref('')
const shop = ref({}); const products = ref([]); const total = ref(0)
const marketActivities = ref([]); const categoryTree = ref([])
const activeTab = ref('products'); const layout = ref('grid'); const sort = ref('sales'); const currentCategory = ref(0)
const cartCount = ref(0)

const deal = item => priceInfo(item)
const promoImage = computed(() => {
  const first = products.value.find(item => item.mainImage)
  return first?.mainImage || fallback
})
const chips = computed(() => {
  const names = new Map()
  const walk = items => (items || []).forEach(item => { names.set(String(item.id), item.name); walk(item.children) })
  walk(categoryTree.value)
  const ids = [...new Set(products.value.map(item => String(item.categoryId || 0)).filter(id => id !== '0'))]
  return [{ id: 0, name: '推荐' }, ...ids.filter(id => names.has(id)).map(id => ({ id: Number(id), name: names.get(id) }))]
})
const filteredProducts = computed(() => {
  let list = products.value
  if (currentCategory.value) list = list.filter(item => String(item.categoryId) === String(currentCategory.value))
  if (sort.value === 'priceAsc') list = [...list].sort((a, b) => Number(deal(a).finalPrice) - Number(deal(b).finalPrice))
  if (sort.value === 'priceDesc') list = [...list].sort((a, b) => Number(deal(b).finalPrice) - Number(deal(a).finalPrice))
  return list
})
const promotions = computed(() => marketActivities.value.map(item => {
  const start = new Date(item.startAt).getTime(); const end = item.endAt ? new Date(item.endAt).getTime() : 0
  const progress = end > start ? Math.min(100, Math.max(4, Math.round((Date.now() - start) / (end - start) * 100))) : 100
  return {
    key: `a-${item.id}`, kind: 'activity', id: item.id, tag: '活动', name: item.name,
    benefit: Number(item.activityType) === 1 ? `满${Number(item.threshold)}减${Number(item.discountValue)}`
      : Number(item.activityType) === 2 ? `满${Number(item.threshold)}打${(Number(item.discountValue) * 10).toFixed(1)}折`
        : `满${Number(item.threshold)}赠券`,
    timeText: `${String(item.startAt || '').slice(0, 10)} ${String(item.startAt || '').slice(11, 16)} - ${item.endAt ? `${String(item.endAt).slice(0, 10)} ${String(item.endAt).slice(11, 16)}` : '长期'}`,
    progress
  }
}))

const togglePrice = () => { sort.value = sort.value === 'priceAsc' ? 'priceDesc' : 'priceAsc' }
const selectCategory = id => { currentCategory.value = id }
const go = item => item.skus?.length ? uni.navigateTo({ url: `/pages/product/detail?id=${item.id}` }) : uni.showToast({ title: '商品缺少SKU', icon: 'none' })
const goPromotion = () => uni.navigateTo({ url: '/pages/category/category' })
const goCart = () => uni.switchTab({ url: '/pages/cart/cart' })
const goSearch = () => uni.navigateTo({ url: '/pages/category/category' })
const shareHint = () => uni.showToast({ title: '点右上角「···」分享店铺', icon: 'none' })
const loadCartCount = async () => {
  if (!isLogin()) return
  const data = await get('/carts/Get').catch(() => null)
  cartCount.value = (data?.items || []).reduce((sum, item) => sum + Number(item.quantity || 0), 0)
}

// 店铺页数据：公开店铺信息 + 本店在售商品 + 本店活动 + 分类树（chips 用）。
const load = async () => {
  const platformId = getPlatform()?.id || 0
  const [shopData, productData, activities, tree] = await Promise.all([
    get('/merchants/Shop', { id: merchantId.value }),
    get('/products/List', { merchantId: merchantId.value, status: 1, page: 1, pageSize: 100 }),
    get('/marketing/ActiveActivities', { platformId, merchantId: merchantId.value }).catch(() => null),
    get('/products/GetCategoryTree').catch(() => [])
  ])
  shop.value = shopData || {}
  products.value = productData.items || []
  total.value = Number(productData.total || products.value.length)
  marketActivities.value = activities?.activities || []
  categoryTree.value = tree || []
  uni.setNavigationBarTitle({ title: shop.value.merchantName || '店铺' })
  await enrichFinalPrices(products.value, platformId)
}
onLoad(options => { theme.value = getTheme(); merchantId.value = options.id || ''; load(); loadCartCount() })
</script>

<style scoped>
.page { min-height: 100vh; padding-bottom: 180rpx; background: #f5f5f5; }
.topbar { background: #fff; padding: 18rpx 24rpx; }
.search { display: flex; align-items: center; gap: 12rpx; background: #f2f3f5; height: 74rpx; border-radius: 980px; padding: 0 28rpx; }
.search-icon { font-size: 26rpx; opacity: .5; }
.search-text { color: #9aa0a8; font-size: 27rpx; }

/* 店铺头 */
.shop-head { display: flex; align-items: center; gap: 22rpx; background: #fff; padding: 26rpx 30rpx 30rpx; }
.shop-logo { width: 108rpx; height: 108rpx; border-radius: 50%; overflow: hidden; background: #e8f1fb; display: grid; place-items: center; flex-shrink: 0; }
.shop-logo-img { width: 100%; height: 100%; }
.shop-logo-text { font-size: 44rpx; font-weight: 800; color: #2f7fd8; }
.shop-info { flex: 1; min-width: 0; }
.shop-name { font-size: 38rpx; font-weight: 800; letter-spacing: -.01em; color: #1a1a1a; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.shop-meta { display: flex; align-items: center; gap: 10rpx; margin-top: 12rpx; }
.stars { color: #ff5b3c; font-size: 24rpx; letter-spacing: 2rpx; }
.score { color: #1a1a1a; font-size: 24rpx; font-weight: 600; }
.divider { color: #d9d9d9; font-size: 22rpx; }
.total { color: #8a8f99; font-size: 24rpx; }
.shop-cate { width: 66rpx; height: 66rpx; border: 1rpx solid #e5e5e5; border-radius: 14rpx; display: grid; place-items: center; font-size: 30rpx; color: #1a1a1a; }

/* Tab */
.tabs { display: flex; background: #fff; }
.tab { flex: 1; display: grid; place-items: center; padding: 20rpx 0 18rpx; font-size: 30rpx; color: #6b7280; position: relative; }
.tab.on { color: #1a1a1a; font-weight: 700; }
.tab.on::after { content: ''; position: absolute; bottom: 0; width: 56rpx; height: 6rpx; border-radius: 6rpx; background: #e93b3d; }

/* 分类 chips */
.chips { white-space: nowrap; background: #fff; padding: 6rpx 20rpx 18rpx; }
.chip { display: inline-block; padding: 10rpx 8rpx 14rpx; margin-right: 34rpx; font-size: 28rpx; color: #4b5563; position: relative; }
.chip.on { color: #1a1a1a; font-weight: 700; }
.chip.on::after { content: ''; position: absolute; left: 50%; transform: translateX(-50%); bottom: 4rpx; width: 44rpx; height: 5rpx; border-radius: 5rpx; background: #e93b3d; }

/* 优惠专区 */
.section { margin-top: 20rpx; }
.section-head { display: flex; align-items: center; justify-content: space-between; padding: 0 30rpx 18rpx; }
.section-title { font-size: 34rpx; font-weight: 800; letter-spacing: -.01em; }
.section-more { font-size: 25rpx; color: #9aa0a8; }
.promo-card { background: #fff; border-radius: 20rpx; margin: 0 24rpx; overflow: hidden; box-shadow: 0 6rpx 18rpx rgba(0, 0, 0, .04); }
.promo-image { width: 100%; height: 460rpx; background: #f7f7f9; }
.promo-body { padding: 22rpx 26rpx 26rpx; }
.promo-name { font-size: 30rpx; font-weight: 600; color: #1a1a1a; }
.promo-time { font-size: 24rpx; color: #8a8f99; margin-top: 10rpx; }
.promo-foot { display: flex; align-items: center; justify-content: space-between; margin-top: 18rpx; }
.promo-bar { flex: 1; height: 10rpx; border-radius: 980px; background: #e9eaee; overflow: hidden; margin-right: 22rpx; }
.promo-fill { height: 100%; border-radius: 980px; background: #24c39a; }
.promo-status { color: #e93b3d; font-size: 26rpx; font-weight: 700; }

/* 排序 */
.sort-bar { display: flex; gap: 60rpx; padding: 30rpx 30rpx 16rpx; }
.sort { font-size: 28rpx; color: #4b5563; display: flex; align-items: center; gap: 6rpx; }
.sort.on { color: #e93b3d; font-weight: 700; }
.arrows { font-size: 22rpx; }

/* 商品网格 */
.grid { display: grid; grid-template-columns: 1fr 1fr; gap: 16rpx; padding: 0 24rpx; }
.product { background: #fff; border-radius: 16rpx; overflow: hidden; box-shadow: 0 4rpx 14rpx rgba(0, 0, 0, .04); }
.product-image { width: 100%; height: 340rpx; background: #f7f7f9; }
.product-name { padding: 16rpx 18rpx 0; font-size: 26rpx; color: #1a1a1a; line-height: 36rpx; display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical; overflow: hidden; }
.product-price { display: flex; align-items: baseline; gap: 8rpx; padding: 12rpx 18rpx 20rpx; }
.yen { color: #e93b3d; font-size: 24rpx; font-weight: 700; }
.amount { color: #e93b3d; font-size: 36rpx; font-weight: 800; font-variant-numeric: tabular-nums; }
.deal-tag { font-size: 19rpx; color: #e93b3d; border: 1rpx solid #e93b3d; border-radius: 6rpx; padding: 0 6rpx; }

/* 活动列表 */
.activity-list { padding: 20rpx 24rpx; display: grid; gap: 20rpx; }
.activity-card { display: flex; align-items: center; gap: 22rpx; background: #fff; border-radius: 20rpx; padding: 24rpx; box-shadow: 0 4rpx 14rpx rgba(0, 0, 0, .04); }
.activity-image { width: 96rpx; height: 96rpx; border-radius: 18rpx; display: grid; place-items: center; }
.activity-icon { font-size: 42rpx; }
.activity-info { flex: 1; min-width: 0; }
.activity-name { font-size: 29rpx; font-weight: 600; color: #1a1a1a; }
.activity-benefit { font-size: 27rpx; font-weight: 700; margin-top: 8rpx; }
.activity-time { font-size: 22rpx; color: #9aa0a8; margin-top: 6rpx; }
.activity-status { color: #24c39a; font-size: 24rpx; font-weight: 600; }

/* 悬浮按钮 */
.fabs { position: fixed; right: 26rpx; bottom: 190rpx; display: grid; gap: 18rpx; z-index: 20; }
.fab { position: relative; width: 96rpx; height: 96rpx; border-radius: 50%; background: #fff; display: grid; place-items: center; box-shadow: 0 10rpx 26rpx rgba(0, 0, 0, .14); }
.fab-icon { font-size: 38rpx; }
.fab-badge { position: absolute; top: -8rpx; right: -6rpx; min-width: 32rpx; height: 32rpx; padding: 0 8rpx; border-radius: 980px; background: #e93b3d; color: #fff; font-size: 20rpx; font-weight: 700; display: grid; place-items: center; }
.empty { display: grid; justify-items: center; gap: 16rpx; color: #9aa0a8; padding: 120rpx 0; font-size: 26rpx; }
.empty .emoji { font-size: 84rpx; }
</style>
