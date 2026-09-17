<template>
  <view class="page">
    <!-- 店铺头（参考图）：头像 + 名称 + 认证信息 + 右侧图标 -->
    <view class="shop-header" :style="{ background: headerGradient }">
      <view class="hero-glow hero-glow-a" />
      <view class="hero-glow hero-glow-b" />
      <view class="shop-logo">{{ (shop.merchantName || '店').slice(0, 1) }}</view>
      <view class="shop-info">
        <view class="shop-name">{{ shop.merchantName || '店铺' }}</view>
        <view class="shop-meta">★★★<text class="meta-rest">★★</text> 平台认证 · 在售 {{ total }} 件</view>
      </view>
      <view class="shop-grid-icon" @tap="toggleLayout">{{ layout === 'grid' ? '☰' : '▦' }}</view>
    </view>

    <!-- 双 Tab（参考图：商品 / 活动） -->
    <view class="tabs">
      <view :class="['tab', activeTab === 'products' && 'on']" @tap="activeTab = 'products'">
        <text :style="activeTab === 'products' ? { color: '#1d1d1f' } : {}">商品</text>
        <view v-if="activeTab === 'products'" class="tab-bar" :style="{ background: theme.primary }" />
      </view>
      <view :class="['tab', activeTab === 'activities' && 'on']" @tap="activeTab = 'activities'">
        <text :style="activeTab === 'activities' ? { color: '#1d1d1f' } : {}">活动</text>
        <view v-if="activeTab === 'activities'" class="tab-bar" :style="{ background: theme.primary }" />
      </view>
    </view>

    <!-- 商品 Tab：分类 chips + 优惠专区 + 排序 + 商品网格 -->
    <template v-if="activeTab === 'products'">
      <scroll-view v-if="chips.length" scroll-x class="chips">
        <view v-for="chip in chips" :key="chip.id" :class="['chip', String(chip.id) === String(currentCategory) && 'on']" :style="String(chip.id) === String(currentCategory) ? { color: theme.primary } : {}" @tap="currentCategory = chip.id">
          <text>{{ chip.name }}</text>
          <view v-if="String(chip.id) === String(currentCategory)" class="chip-bar" :style="{ background: theme.primary }" />
        </view>
      </scroll-view>

      <view v-if="promotions.length" class="promo-section">
        <view class="section-head"><text class="section-title">优惠专区</text><text class="section-more" :style="{ color: theme.primary }" @tap="activeTab = 'activities'">查看更多 ›</text></view>
        <view class="promo-card" :style="{ background: theme.primary + '0F' }" @tap="goPromotion(promotions[0])">
          <text class="promo-tag" :style="{ color: theme.primary, background: '#fff' }">{{ promotions[0].tag }}</text>
          <text class="promo-name">{{ promotions[0].name }}</text>
          <text class="promo-benefit" :style="{ color: theme.primary }">{{ promotions[0].benefit }}</text>
          <text class="promo-time">{{ promotions[0].timeText }}</text>
          <view class="promo-dots"><view v-for="n in Math.min(promotions.length, 3)" :key="n" :class="['promo-dot', n === 1 && 'on']" /></view>
          <text class="promo-status">进行中</text>
        </view>
      </view>

      <view class="sort-bar">
        <text :class="['sort-item', sort === '' && 'on']" :style="sort === '' ? { color: theme.primary } : {}" @tap="sort = ''">综合</text>
        <text :class="['sort-item', sort !== '' && 'on']" :style="sort !== '' ? { color: theme.primary } : {}" @tap="togglePrice">价格 {{ sort === 'asc' ? '↑' : sort === 'desc' ? '↓' : '↕' }}</text>
      </view>

      <view v-if="!filteredProducts.length" class="empty small"><text class="emoji">🧺</text><text>暂无相关商品</text></view>
      <view v-else-if="layout === 'grid'" class="grid">
        <view v-for="item in filteredProducts" :key="item.id" class="product" @tap="go(item)">
          <image :src="item.mainImage || fallback" mode="aspectFill" />
          <view class="pinfo">
            <view class="pname">{{ item.name }}</view>
            <view class="price" :style="{ color: theme.primary }"><text class="yen">¥</text>{{ minPrice(item) }}</view>
          </view>
        </view>
      </view>
      <view v-else class="list">
        <view v-for="item in filteredProducts" :key="item.id" class="list-item" @tap="go(item)">
          <image :src="item.mainImage || fallback" mode="aspectFill" />
          <view class="list-info">
            <view class="pname">{{ item.name }}</view>
            <view class="price" :style="{ color: theme.primary }"><text class="yen">¥</text>{{ minPrice(item) }}</view>
          </view>
        </view>
      </view>
    </template>

    <!-- 活动 Tab -->
    <template v-else>
      <view v-if="!promotions.length" class="empty small"><text class="emoji">🎁</text><text>店铺暂无进行中活动</text></view>
      <view v-else class="activity-list">
        <view v-for="item in promotions" :key="item.key" class="activity-card" @tap="goPromotion(item)">
          <view class="activity-left" :style="{ background: theme.primary + '12' }"><text class="activity-icon">🎁</text></view>
          <view class="activity-info">
            <view class="activity-name">{{ item.name }}</view>
            <view class="activity-benefit" :style="{ color: theme.primary }">{{ item.benefit }}</view>
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

const fallback = '/static/placeholder.png'
const theme = ref(getTheme()); const merchantId = ref('')
const shop = ref({}); const products = ref([]); const total = ref(0)
const marketActivities = ref([]); const categoryTree = ref([])
const activeTab = ref('products'); const layout = ref('grid'); const sort = ref(''); const currentCategory = ref(0)
const cartCount = ref(0)

const headerGradient = computed(() => {
  const primary = theme.value.primary || '#0071e3'
  return `linear-gradient(135deg, ${primary} 0%, ${primary}D9 55%, ${primary}A6 100%)`
})
const minPrice = item => (item.skus || []).length ? Math.min(...item.skus.map(sku => Number(sku.price))).toFixed(2) : '--'
// 商品分类 chips：推荐 + 本店商品实际出现的分类（来自分类树名称映射）。
const chips = computed(() => {
  const names = new Map()
  const walk = items => (items || []).forEach(item => {
    names.set(String(item.id), item.name)
    walk(item.children)
  })
  walk(categoryTree.value)
  const ids = [...new Set(products.value.map(item => String(item.categoryId || 0)).filter(id => id !== '0'))]
  return [{ id: 0, name: '推荐' }, ...ids.filter(id => names.has(id)).map(id => ({ id, name: names.get(id) }))]
})
const filteredProducts = computed(() => {
  let list = products.value
  if (currentCategory.value) list = list.filter(item => String(item.categoryId) === String(currentCategory.value))
  if (sort.value) {
    list = [...list].sort((a, b) => sort.value === 'asc' ? Number(minPrice(a)) - Number(minPrice(b)) : Number(minPrice(b)) - Number(minPrice(a)))
  }
  return list
})
const promotions = computed(() => marketActivities.value.map(item => ({
  key: `a-${item.id}`, kind: 'activity', id: item.id, tag: '活动', name: item.name,
  benefit: Number(item.activityType) === 1 ? `满${Number(item.threshold)}减${Number(item.discountValue)}`
    : Number(item.activityType) === 2 ? `满${Number(item.threshold)}打${(Number(item.discountValue) * 10).toFixed(1)}折`
      : `满${Number(item.threshold)}赠券`,
  timeText: `${String(item.startAt || '').slice(0, 10)} ~ ${item.endAt ? String(item.endAt).slice(0, 10) : '长期'}`
})))

const toggleLayout = () => { layout.value = layout.value === 'grid' ? 'list' : 'grid' }
const togglePrice = () => { sort.value = sort.value === 'asc' ? 'desc' : 'asc' }
const go = item => item.skus?.length ? uni.navigateTo({ url: `/pages/product/detail?id=${item.id}` }) : uni.showToast({ title: '商品缺少SKU', icon: 'none' })
const goPromotion = () => uni.navigateTo({ url: '/pages/category/category' })
const goCart = () => uni.switchTab({ url: '/pages/cart/cart' })
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
}
onLoad(options => { theme.value = getTheme(); merchantId.value = options.id || ''; load(); loadCartCount() })
</script>

<style scoped>
.page { min-height: 100vh; padding-bottom: 160rpx; background: #f5f5f7; }
.shop-header { position: relative; overflow: hidden; display: flex; align-items: center; gap: 26rpx; margin: 24rpx; padding: 40rpx; border-radius: 36rpx; box-shadow: 0 16rpx 38rpx rgba(0, 0, 0, .14); }
.hero-glow { position: absolute; border-radius: 50%; background: rgba(255, 255, 255, .16); }
.hero-glow-a { width: 300rpx; height: 300rpx; right: -70rpx; top: -130rpx; }
.hero-glow-b { width: 180rpx; height: 180rpx; right: 140rpx; bottom: -100rpx; }
.shop-logo { position: relative; width: 112rpx; height: 112rpx; display: grid; place-items: center; flex-shrink: 0; border-radius: 50%; background: rgba(255, 255, 255, .22); border: 2rpx solid rgba(255, 255, 255, .5); color: #fff; font-size: 44rpx; font-weight: 700; }
.shop-info { position: relative; flex: 1; min-width: 0; }
.shop-name { font-size: 40rpx; font-weight: 800; letter-spacing: -.02em; color: #fff; }
.shop-meta { margin-top: 10rpx; font-size: 23rpx; color: rgba(255, 255, 255, .85); }
.meta-rest { color: rgba(255, 255, 255, .5); }
.shop-grid-icon { position: relative; color: #fff; font-size: 36rpx; opacity: .9; }

/* 双 Tab */
.tabs { display: flex; justify-content: center; gap: 120rpx; background: #fff; margin: 0 24rpx; border-radius: 28rpx 28rpx 0 0; padding-top: 26rpx; }
.tab { display: grid; justify-items: center; gap: 14rpx; padding-bottom: 20rpx; font-size: 30rpx; color: #86868b; }
.tab.on { font-weight: 700; }
.tab-bar { width: 48rpx; height: 6rpx; border-radius: 980px; }

/* 分类 chips */
.chips { white-space: nowrap; background: #fff; margin: 0 24rpx; padding: 6rpx 24rpx 20rpx; }
.chips::-webkit-scrollbar { display: none; }
.chip { display: inline-grid; justify-items: center; gap: 10rpx; margin-right: 44rpx; font-size: 27rpx; color: #6e6e73; }
.chip.on { font-weight: 700; }
.chip-bar { width: 40rpx; height: 6rpx; border-radius: 980px; }

/* 优惠专区 */
.promo-section { background: #fff; margin: 20rpx 24rpx 0; border-radius: 28rpx; padding: 28rpx 30rpx; box-shadow: 0 8rpx 24rpx rgba(0, 0, 0, .05); }
.section-head { display: flex; align-items: center; justify-content: space-between; margin-bottom: 22rpx; }
.section-title { font-size: 32rpx; font-weight: 800; letter-spacing: -.02em; }
.section-more { font-size: 24rpx; font-weight: 600; }
.promo-card { position: relative; border-radius: 24rpx; padding: 26rpx 28rpx; }
.promo-tag { position: absolute; left: 0; top: 0; font-size: 20rpx; font-weight: 700; padding: 6rpx 18rpx; border-radius: 24rpx 0 24rpx 0; }
.promo-name { display: block; font-size: 30rpx; font-weight: 700; margin: 18rpx 0 10rpx; }
.promo-benefit { display: block; font-size: 34rpx; font-weight: 800; }
.promo-time { display: block; margin-top: 10rpx; font-size: 22rpx; color: #a1a1a6; }
.promo-dots { display: flex; gap: 8rpx; justify-content: center; margin-top: 18rpx; }
.promo-dot { width: 10rpx; height: 10rpx; border-radius: 50%; background: rgba(0, 0, 0, .12); }
.promo-dot.on { background: rgba(0, 0, 0, .4); }
.promo-status { position: absolute; right: 28rpx; bottom: 26rpx; color: #ff3b30; font-size: 26rpx; font-weight: 700; }

/* 排序 + 商品 */
.sort-bar { display: flex; justify-content: center; gap: 140rpx; padding: 26rpx 0 8rpx; }
.sort-item { font-size: 28rpx; color: #6e6e73; }
.sort-item.on { font-weight: 700; }
.empty.small { display: grid; justify-items: center; gap: 14rpx; padding: 120rpx 0; color: #a1a1a6; font-size: 26rpx; }
.empty .emoji { font-size: 92rpx; }
.grid { display: grid; grid-template-columns: repeat(2, 1fr); gap: 20rpx; padding: 18rpx 24rpx 0; }
.product { background: #fff; border-radius: 24rpx; overflow: hidden; box-shadow: 0 6rpx 20rpx rgba(0, 0, 0, .05); }
.product image { width: 100%; height: 330rpx; background: #f7f7f9; }
.pinfo { padding: 18rpx 20rpx 22rpx; }
.pname { height: 72rpx; overflow: hidden; font-weight: 600; font-size: 26rpx; line-height: 36rpx; display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical; }
.price { margin-top: 12rpx; font-weight: 800; font-size: 34rpx; font-variant-numeric: tabular-nums; }
.price .yen { font-size: 22rpx; font-weight: 700; margin-right: 2rpx; }
.list { padding: 18rpx 24rpx 0; }
.list-item { display: flex; gap: 20rpx; background: #fff; border-radius: 24rpx; padding: 22rpx; margin-bottom: 18rpx; box-shadow: 0 6rpx 20rpx rgba(0, 0, 0, .05); }
.list-item image { width: 180rpx; height: 180rpx; border-radius: 18rpx; background: #f7f7f9; }
.list-info { flex: 1; display: flex; flex-direction: column; justify-content: space-between; }
.list-info .pname { height: auto; max-height: 76rpx; }

/* 活动 Tab */
.activity-list { padding: 22rpx 24rpx 0; }
.activity-card { display: flex; align-items: center; gap: 22rpx; background: #fff; border-radius: 26rpx; padding: 26rpx; margin-bottom: 20rpx; box-shadow: 0 6rpx 20rpx rgba(0, 0, 0, .05); }
.activity-left { width: 96rpx; height: 96rpx; display: grid; place-items: center; border-radius: 24rpx; }
.activity-icon { font-size: 44rpx; }
.activity-info { flex: 1; min-width: 0; }
.activity-name { font-size: 29rpx; font-weight: 700; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.activity-benefit { margin-top: 8rpx; font-size: 30rpx; font-weight: 800; }
.activity-time { margin-top: 8rpx; font-size: 21rpx; color: #a1a1a6; }
.activity-status { color: #ff3b30; font-size: 24rpx; font-weight: 700; }

/* 悬浮按钮 */
.fabs { position: fixed; right: 28rpx; bottom: 220rpx; display: grid; gap: 18rpx; z-index: 20; }
.fab { position: relative; width: 92rpx; height: 92rpx; display: grid; place-items: center; border-radius: 50%; background: rgba(255, 255, 255, .95); box-shadow: 0 10rpx 26rpx rgba(0, 0, 0, .16); }
.fab-icon { font-size: 38rpx; }
.fab-badge { position: absolute; top: -6rpx; right: -6rpx; min-width: 32rpx; height: 32rpx; padding: 0 8rpx; border-radius: 980px; background: #ff3b30; color: #fff; font-size: 19rpx; font-weight: 700; display: grid; place-items: center; }
</style>
