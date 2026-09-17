<template>
  <view class="page" :style="pageStyle">
    <view v-if="!platformCode" class="empty">
      <text class="emoji">🏬</text>
      <text>请先选择平台</text>
      <button class="primary" @tap="goSelector">选择平台</button>
    </view>

    <template v-else>
      <!-- 铺满式头图（参考图样式）：图片轮播或主题渐变；顶部平台切换、底部悬浮搜索 -->
      <view class="hero-wrap">
        <swiper v-if="bannerItems.length" class="hero" circular autoplay :interval="4200" :indicator-dots="bannerItems.length > 1" indicator-color="rgba(255,255,255,.5)" indicator-active-color="#fff">
          <swiper-item v-for="banner in bannerItems" :key="banner.image + banner.title">
            <image class="hero-image" :src="banner.image || fallback" mode="aspectFill" @tap="goLink(banner)" />
          </swiper-item>
        </swiper>
        <view v-else class="hero hero-fallback" :style="{ background: heroGradient }" @tap="goCategory('')">
          <view class="hero-glow hero-glow-a" />
          <view class="hero-glow hero-glow-b" />
          <view class="hero-title">{{ design.home.appName || platform?.platformName }}</view>
          <view class="hero-sub">{{ design.home.slogan || '本平台专属精选商城' }}</view>
        </view>

        <view class="hero-topbar" @tap="goSelector">
          <text class="hero-location">📍 {{ platform?.platformName || '选择平台' }}</text>
          <text class="hero-caret">∨</text>
        </view>

        <view class="hero-search">
          <text class="hero-search-icon">🔍</text>
          <input v-model="keyword" placeholder="搜索" confirm-type="search" @confirm="search" />
          <text class="hero-search-action" :style="{ color: themeCss.primary }" @tap="search">搜索</text>
        </view>
      </view>

      <!-- 金刚区：白卡上浮压在头图上（参考图布局），图标由后台配置 -->
      <view v-if="quickItems.length" class="quick-card">
        <view v-for="item in quickItems" :key="item.title" class="quick-item" @tap="goLink(item)">
          <image v-if="isImageIcon(item.icon)" class="quick-icon-img" :src="item.icon" mode="aspectFit" />
          <text v-else class="quick-icon" :style="{ background: themeCss.primary + '12' }">{{ item.icon || '🎁' }}</text>
          <text class="quick-label">{{ item.title }}</text>
        </view>
      </view>

      <!-- 会员问候 + 账户概览（参考图首页"Hi 残阳"卡片） -->
      <view v-if="logged" class="greeting-card">
        <view class="greeting-top">
          <text class="greeting-hi">Hi，{{ user.userName || '会员' }}，{{ greeting }}</text>
          <view class="greeting-chip" :style="{ color: themeCss.primary, background: themeCss.primary + '14' }">{{ memberTier.name }}</view>
        </view>
        <view class="greeting-stats">
          <view class="stat" @tap="goMine"><text class="stat-num">{{ stats.coupons }}<text class="stat-unit">张</text></text><text class="stat-label">优惠券</text></view>
          <view class="stat" @tap="goMine"><text class="stat-num">{{ stats.favorites }}</text><text class="stat-label">我的收藏</text></view>
          <view class="stat" @tap="goMine"><text class="stat-num">{{ stats.orders }}</text><text class="stat-label">订单</text></view>
          <view class="stat stat-more" @tap="goMine"><text class="stat-num">全部</text><text class="stat-label">查看 ›</text></view>
        </view>
      </view>
      <view v-else class="greeting-card guest" @tap="goLogin">
        <text class="guest-text">Hi，登录后查看优惠券与订单 ›</text>
      </view>

      <view v-if="design.home.notice" class="notice">
        <text class="notice-tag" :style="{ background: themeCss.primary + '14', color: themeCss.primary }">公告</text>
        <text class="notice-text">{{ design.home.notice }}</text>
      </view>

      <!-- 优惠专区：进行中活动与可领券 -->
      <view v-if="promotions.length" class="block promo-block">
        <view class="block-title">
          <view class="block-title-text">
            <text class="title">优惠专区</text>
            <text class="subtitle">正在进行</text>
          </view>
          <text class="more" :style="{ color: themeCss.primary }" @tap="goCouponCenter">查看全部 ›</text>
        </view>
        <scroll-view scroll-x class="promo-scroll">
          <view v-for="item in promotions" :key="item.key" class="promo-card" :style="{ background: themeCss.primary + '0F' }" @tap="goPromotion(item)">
            <text class="promo-card-tag" :style="{ color: themeCss.primary, background: '#fff' }">{{ item.tag }}</text>
            <text class="promo-card-name">{{ item.name }}</text>
            <text class="promo-card-benefit" :style="{ color: themeCss.primary }">{{ item.benefit }}</text>
            <text class="promo-card-time">{{ item.timeText }}</text>
          </view>
        </scroll-view>
      </view>

      <view v-for="(module, index) in visibleModules" :key="`${module.type}-${index}`" class="block">
        <view v-if="module.title" class="block-title">
          <view class="block-title-text">
            <text class="title">{{ module.title }}</text>
            <text class="subtitle">{{ moduleSubtitle(module) }}</text>
          </view>
          <text v-if="module.type === 'products'" class="more" :style="{ color: themeCss.primary }" @tap="goCategory(module.categoryId)">更多 ›</text>
        </view>

        <view v-if="module.type === 'quickNav'" class="quick-grid">
          <view v-for="item in (module.items || []).slice(0, 8)" :key="item.title" class="quick-item" @tap="goLink(item)">
            <image v-if="isImageIcon(item.icon)" class="quick-icon-img" :src="item.icon" mode="aspectFit" />
            <text v-else class="quick-icon" :style="{ background: themeCss.primary + '12' }">{{ item.icon || '🎁' }}</text>
            <text class="quick-label">{{ item.title }}</text>
          </view>
        </view>

        <scroll-view v-if="module.type === 'categories'" scroll-x class="category-scroll">
          <view v-for="category in visibleCategories(module)" :key="category.id" class="category" :style="{ background: themeCss.primary + '12', color: themeCss.primary }" @tap="goCategory(category.id)">{{ category.name }}</view>
        </scroll-view>

        <template v-if="module.type === 'products'">
          <view v-if="sectionLoading(module)" class="grid">
            <view v-for="n in 4" :key="n" class="product skeleton">
              <view class="skeleton-image" /><view class="skeleton-line" /><view class="skeleton-line short" />
            </view>
          </view>
          <view v-else-if="!sectionProducts(module).length" class="empty small">本模块暂无商品</view>
          <scroll-view v-else-if="module.layout === 'list'" scroll-x class="list-scroll">
            <view v-for="item in sectionProducts(module)" :key="`${module.key}-${item.id}`" class="product row" @tap="goProduct(item)">
              <image :src="item.mainImage || fallback" mode="aspectFill" />
              <view class="pinfo"><view class="pname">{{ item.name }}</view><view class="price"><text class="yen">¥</text>{{ minPrice(item) }}</view></view>
            </view>
          </scroll-view>
          <!-- 三列紧凑推荐（参考图"商场特惠/为您推荐"） -->
          <view v-else-if="module.layout === 'grid3'" class="grid3">
            <view v-for="item in sectionProducts(module)" :key="`${module.key}-${item.id}`" class="product compact" @tap="goProduct(item)">
              <image :src="item.mainImage || fallback" mode="aspectFill" />
              <view class="pinfo">
                <view class="pname">{{ item.name }}</view>
                <view class="price"><text class="yen">¥</text>{{ minPrice(item) }}</view>
              </view>
            </view>
          </view>
          <view v-else class="grid">
            <view v-for="item in sectionProducts(module)" :key="`${module.key}-${item.id}`" class="product" @tap="goProduct(item)">
              <image :src="item.mainImage || fallback" mode="aspectFill" />
              <view class="pinfo">
                <view class="pname">{{ item.name }}</view>
                <view class="price-row"><view class="price"><text class="yen">¥</text>{{ minPrice(item) }}</view><text class="buy-dot" :style="{ color: themeCss.primary, background: themeCss.primary + '14' }">＋</text></view>
              </view>
            </view>
          </view>
        </template>
      </view>
    </template>
  </view>
</template>

<script setup>
import { onShow } from '@dcloudio/uni-app'
import { computed, ref } from 'vue'
import { get } from '@/common/request'
import { loadPlatformDesign } from '@/common/design'
import { isImageIcon } from '@/common/icon'
import { getPlatform, getPlatformCode, getTheme, getUser, isLogin } from '@/common/store'
import { memberTierOf } from '@/common/member'

const fallback = '/static/placeholder.png'
const design = ref({ home: {}, theme: {}, modules: [], profile: {} })
const platform = ref(null); const platformCode = ref(''); const keyword = ref('')
const theme = ref(getTheme()); const categories = ref([]); const productsByModule = ref({}); const loadingModules = ref({})
const logged = ref(false); const user = ref({}); const stats = ref({ coupons: 0, favorites: 0, orders: 0 })
const marketActivities = ref([]); const marketCoupons = ref([])

const pageStyle = computed(() => ({ background: design.value.theme?.background || theme.value.background, minHeight: '100vh' }))
const themeCss = computed(() => ({ primary: design.value.theme?.primary || theme.value.primary }))
const heroGradient = computed(() => {
  const primary = themeCss.value.primary
  return `linear-gradient(135deg, ${primary} 0%, ${primary}D9 52%, ${primary}A6 100%)`
})
const greeting = computed(() => {
  const hour = new Date().getHours()
  return hour < 6 ? '凌晨好' : hour < 12 ? '早上好' : hour < 18 ? '下午好' : '晚上好'
})
const memberTier = computed(() => memberTierOf(stats.value.spent))
const modules = computed(() => design.value.home?.modules || [])
// 金刚区：优先使用独立的快捷入口模块（取第一个），参考图中它压在头图上。
const quickItems = computed(() => modules.value.find(module => module.type === 'quickNav')?.items || [])
const bannerItems = computed(() => {
  const top = (design.value.home?.banners || []).filter(item => item.image)
  if (top.length) return top
  const module = modules.value.find(item => item.type === 'banners')
  return (module?.items || []).filter(item => item.image)
})
// 过滤没有内容的模块；金刚区已单独渲染在头图下方，不再重复。
const visibleModules = computed(() => modules.value.filter(module => {
  if (module.type === 'quickNav' || module.type === 'banners') return false
  if (module.type === 'categories') return categories.value.length > 0
  return true
}))
const promotions = computed(() => [
  ...marketActivities.value.map(item => ({
    key: `a-${item.id}`, kind: 'activity', id: item.id, tag: '活动',
    name: item.name,
    benefit: Number(item.activityType) === 1 ? `满${Number(item.threshold)}减${Number(item.discountValue)}`
      : Number(item.activityType) === 2 ? `满${Number(item.threshold)}打${(Number(item.discountValue) * 10).toFixed(1)}折`
        : `满${Number(item.threshold)}赠券`,
    timeText: formatRange(item.startAt, item.endAt)
  })),
  ...marketCoupons.value.map(item => ({
    key: `c-${item.id}`, kind: 'coupon', id: item.id, tag: '可领券',
    name: item.name, benefit: `剩余${item.remainingStock}张`,
    timeText: item.endAt ? `至 ${String(item.endAt).slice(0, 10)}` : '长期有效'
  }))
])

const moduleSubtitle = module => ({ categories: '探索分类', products: '精选好物' }[module.type] || '')
const formatRange = (start, end) => `${String(start || '').slice(0, 10)} ~ ${end ? String(end).slice(0, 10) : '长期'}`
const goSelector = () => uni.navigateTo({ url: '/pages/platform/selector' })
const goMine = () => uni.switchTab({ url: '/pages/profile/profile' })
const goLogin = () => uni.navigateTo({ url: '/pages/auth/login' })
const goCouponCenter = () => uni.navigateTo({ url: '/pages/coupon/center' })
const minPrice = item => (item.skus || []).length ? Math.min(...item.skus.map(sku => Number(sku.price))).toFixed(2) : '--'
const sectionProducts = module => productsByModule.value[module.key || module.title] || []
const sectionLoading = module => loadingModules.value[module.key || module.title]
const visibleCategories = module => categories.value.slice(0, Number(module.limit || 8))

const flattenCategories = (items, result = []) => (items || []).flatMap(item => [item, ...flattenCategories(item.children || [])])
const goLink = item => {
  const type = item.linkType || item.action
  if (type === 'category') return goCategory(item.linkValue || item.value)
  if (type === 'cart') return uni.switchTab({ url: '/pages/cart/cart' })
  if (type === 'orders') return uni.navigateTo({ url: '/pages/orders/orders' })
  if (type === 'address') return uni.navigateTo({ url: '/pages/address/address' })
  if (type === 'coupon-center') return goCouponCenter()
  if (type === 'coupons') return uni.navigateTo({ url: '/pages/coupon/mine' })
  if (type === 'favorites') return uni.navigateTo({ url: '/pages/favorites/favorites' })
  if (type === 'profile') return goMine()
  uni.navigateTo({ url: `/pages/category/category?id=${item.linkValue || item.value || ''}` })
}
const goCategory = categoryId => uni.navigateTo({ url: `/pages/category/category?id=${categoryId || ''}` })
const goProduct = item => {
  if (!item.skus?.length) return uni.showToast({ title: '商品缺少SKU', icon: 'none' })
  uni.navigateTo({ url: `/pages/product/detail?id=${item.id}` })
}
const goPromotion = item => item.kind === 'coupon' ? goCouponCenter() : goCategory('')
const search = () => {
  const text = String(keyword.value || '').trim()
  if (!text) return uni.showToast({ title: '请输入搜索关键词', icon: 'none' })
  uni.navigateTo({ url: `/pages/category/category?keyword=${encodeURIComponent(text)}` })
}

const loadModules = async () => {
  for (const module of modules.value) {
    if (module.type !== 'products') continue
    const key = module.key || module.title
    loadingModules.value[key] = true
    try {
      const data = await get('/products/List', {
        categoryId: module.categoryId || 0, merchantId: module.merchantId || 0,
        status: 1, page: 1, pageSize: module.limit || 10
      })
      productsByModule.value[key] = data.items || []
    } finally { loadingModules.value[key] = false }
  }
}

// 会员统计：券/收藏/订单 + 累计消费（订单实付累加，用于会员等级展示；仅前端计算）。
const loadMemberStats = async () => {
  logged.value = isLogin()
  user.value = getUser()
  if (!logged.value) { stats.value = { coupons: 0, favorites: 0, orders: 0, spent: 0 }; return }
  const [coupons, favorites, orders] = await Promise.all([
    get('/marketing/MyCoupons').catch(() => ({ items: [] })),
    get('/users/Favorites').catch(() => []),
    get('/orders/List', { customerId: 0, page: 1, pageSize: 100 }).catch(() => ({ items: [], total: 0 }))
  ])
  const paid = (orders.items || []).filter(item => Number(item.orderStatus) >= 20 && Number(item.orderStatus) !== 60)
  stats.value = {
    coupons: (coupons.items || []).filter(item => Number(item.status) === 1).length,
    favorites: (favorites || []).length,
    orders: Number(orders.total || 0),
    spent: paid.reduce((sum, item) => sum + Number(item.paymentPrice || 0), 0)
  }
}

const loadPromotions = async () => {
  const data = await get('/marketing/ActiveActivities', { platformId: platform.value?.id || 0 }).catch(() => null)
  marketActivities.value = data?.activities || []
  marketCoupons.value = data?.coupons || []
}

onShow(async () => {
  platformCode.value = getPlatformCode()
  platform.value = getPlatform()
  if (!platformCode.value) return
  const loaded = await loadPlatformDesign()
  if (loaded) {
    design.value = loaded
    theme.value = getTheme()
    uni.setNavigationBarTitle({ title: loaded.home?.appName || platform.value?.platformName || '商城' })
  }
  const tree = await get('/products/GetCategoryTree') || []
  categories.value = flattenCategories(tree)
  await Promise.all([loadModules(), loadMemberStats(), loadPromotions()])
})
</script>

<style scoped>
.page { padding-bottom: 170rpx; background: #f5f5f7; }
.empty { display: grid; justify-items: center; gap: 18rpx; color: #a1a1a6; padding: 160rpx 0; }
.empty .emoji { font-size: 104rpx; line-height: 1; }
.empty.small { padding: 60rpx 0; font-size: 26rpx; }
.empty button { margin-top: 10rpx; width: 260rpx; color: #fff; background: #0071e3; }

/* 铺满式头图 + 悬浮元素（参考图首页） */
.hero-wrap { position: relative; }
.hero { width: 750rpx; height: 540rpx; }
.hero-image { width: 750rpx; height: 540rpx; }
.hero-fallback { position: relative; overflow: hidden; height: 540rpx; display: flex; flex-direction: column; justify-content: center; padding: 0 44rpx; box-sizing: border-box; }
.hero-glow { position: absolute; border-radius: 50%; background: rgba(255, 255, 255, .16); }
.hero-glow-a { width: 360rpx; height: 360rpx; right: -80rpx; top: -120rpx; }
.hero-glow-b { width: 220rpx; height: 220rpx; right: 140rpx; bottom: -110rpx; }
.hero-title { position: relative; color: #fff; font-size: 48rpx; font-weight: 800; letter-spacing: -.02em; }
.hero-sub { position: relative; color: rgba(255, 255, 255, .84); font-size: 25rpx; margin-top: 12rpx; }
.hero-topbar { position: absolute; left: 30rpx; top: calc(24rpx + env(safe-area-inset-top)); display: flex; align-items: center; gap: 8rpx; background: rgba(0, 0, 0, .28); backdrop-filter: blur(10px); border-radius: 980px; padding: 10rpx 22rpx; }
.hero-location { color: #fff; font-size: 25rpx; font-weight: 600; max-width: 420rpx; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.hero-caret { color: rgba(255, 255, 255, .85); font-size: 22rpx; }
.hero-search { position: absolute; left: 30rpx; right: 30rpx; bottom: -40rpx; z-index: 6; display: flex; align-items: center; gap: 12rpx; background: rgba(255, 255, 255, .96); backdrop-filter: blur(20px) saturate(180%); height: 84rpx; border-radius: 980rpx; padding: 0 30rpx; box-shadow: 0 12rpx 32rpx rgba(0, 0, 0, .14); }
.hero-search-icon { font-size: 26rpx; opacity: .45; }
.hero-search input { flex: 1; height: 100%; font-size: 27rpx; }
.hero-search-action { font-weight: 600; font-size: 27rpx; }

/* 金刚区白卡上浮 */
/* 金刚区：给悬浮搜索条留出位置（搜索条 bottom:-40rpx 叠在头图上） */
.quick-card { display: grid; grid-template-columns: repeat(4, 1fr); gap: 24rpx 12rpx; background: #fff; margin: 62rpx 24rpx 0; padding: 34rpx 24rpx 30rpx; border-radius: 32rpx; box-shadow: 0 12rpx 32rpx rgba(0, 0, 0, .08); }
.quick-grid { display: grid; grid-template-columns: repeat(4, 1fr); gap: 28rpx 18rpx; }
.quick-item { display: grid; justify-items: center; gap: 14rpx; }
.quick-icon-img { width: 76rpx; height: 76rpx; }
.quick-icon { width: 76rpx; height: 76rpx; display: grid; place-items: center; border-radius: 24rpx; font-size: 38rpx; }
.quick-label { font-size: 23rpx; color: #6e6e73; line-height: 1; }

/* 会员问候卡（参考图 Hi 卡片） */
.greeting-card { background: #fff; margin: 26rpx 24rpx 0; border-radius: 32rpx; padding: 30rpx 32rpx 8rpx; box-shadow: 0 8rpx 28rpx rgba(0, 0, 0, .05); }
.greeting-card.guest { padding: 34rpx 32rpx; }
.guest-text { font-size: 28rpx; color: #6e6e73; }
.greeting-top { display: flex; align-items: center; justify-content: space-between; }
.greeting-hi { font-size: 34rpx; font-weight: 800; letter-spacing: -.02em; }
.greeting-chip { font-size: 21rpx; font-weight: 600; padding: 6rpx 18rpx; border-radius: 980px; }
.greeting-stats { display: flex; margin-top: 24rpx; border-top: 1rpx solid rgba(60, 60, 67, .06); }
.stat { flex: 1; display: grid; justify-items: center; gap: 6rpx; padding: 24rpx 0; }
.stat-num { font-size: 38rpx; font-weight: 800; font-variant-numeric: tabular-nums; }
.stat-unit { font-size: 22rpx; font-weight: 600; margin-left: 2rpx; }
.stat-label { font-size: 22rpx; color: #86868b; }
.stat-more .stat-num { font-size: 26rpx; font-weight: 600; }

.notice { display: flex; gap: 14rpx; align-items: center; margin: 24rpx 24rpx 0; background: #fff; border-radius: 22rpx; padding: 22rpx 26rpx; box-shadow: 0 6rpx 20rpx rgba(0, 0, 0, .04); }
.notice-tag { padding: 4rpx 14rpx; border-radius: 980px; font-size: 20rpx; font-weight: 600; flex-shrink: 0; }
.notice-text { color: #6e6e73; font-size: 24rpx; overflow: hidden; white-space: nowrap; text-overflow: ellipsis; }

.block { background: #fff; margin: 24rpx; border-radius: 32rpx; padding: 32rpx; box-shadow: 0 8rpx 28rpx rgba(0, 0, 0, .05); }
.block-title { display: flex; align-items: flex-end; justify-content: space-between; margin-bottom: 26rpx; }
.block-title-text { display: grid; gap: 6rpx; }
.block-title .title { font-size: 36rpx; font-weight: 800; letter-spacing: -.02em; }
.block-title .subtitle { font-size: 22rpx; color: #a1a1a6; }
.block-title .more { font-size: 25rpx; font-weight: 600; }

/* 优惠专区 */
.promo-block { padding-bottom: 26rpx; }
.promo-scroll { white-space: nowrap; }
.promo-scroll::-webkit-scrollbar { display: none; }
.promo-card { display: inline-flex; flex-direction: column; gap: 10rpx; width: 320rpx; margin-right: 18rpx; padding: 24rpx; border-radius: 24rpx; box-sizing: border-box; vertical-align: top; }
.promo-card-tag { align-self: flex-start; font-size: 20rpx; font-weight: 700; padding: 4rpx 14rpx; border-radius: 980px; }
.promo-card-name { font-size: 28rpx; font-weight: 700; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.promo-card-benefit { font-size: 32rpx; font-weight: 800; }
.promo-card-time { font-size: 21rpx; color: #a1a1a6; }

.category-scroll { white-space: nowrap; }
.category { display: inline-block; padding: 18rpx 32rpx; border-radius: 980px; margin-right: 16rpx; font-size: 26rpx; font-weight: 600; }
.category-scroll::-webkit-scrollbar, .list-scroll::-webkit-scrollbar { display: none; }

/* 商品卡：白卡 + 1:1 图 */
.grid { display: grid; grid-template-columns: repeat(2, 1fr); gap: 22rpx; }
.grid3 { display: grid; grid-template-columns: repeat(3, 1fr); gap: 16rpx; }
.product { background: #fff; border-radius: 26rpx; overflow: hidden; border: 1rpx solid rgba(60, 60, 67, .06); box-shadow: 0 8rpx 24rpx rgba(0, 0, 0, .05); }
.product image { width: 100%; height: 330rpx; background: #f7f7f9; }
.product.compact image { height: 230rpx; }
.pinfo { padding: 20rpx 22rpx 24rpx; }
.pname { height: 72rpx; overflow: hidden; font-weight: 600; letter-spacing: -.01em; font-size: 27rpx; line-height: 36rpx; display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical; }
.product.compact .pname { height: 104rpx; font-size: 24rpx; line-height: 34rpx; -webkit-line-clamp: 3; }
.price-row { display: flex; align-items: center; justify-content: space-between; margin-top: 14rpx; }
.price { color: #1d1d1f; font-weight: 800; font-size: 34rpx; font-variant-numeric: tabular-nums; }
.product.compact .price { font-size: 30rpx; margin-top: 8rpx; }
.price .yen { font-size: 23rpx; font-weight: 700; margin-right: 2rpx; }
.buy-dot { width: 46rpx; height: 46rpx; border-radius: 50%; display: grid; place-items: center; font-size: 28rpx; font-weight: 700; }
.row { border: 0; background: #f5f5f7; box-shadow: none; }
.row image { width: 190rpx; height: 190rpx; border-radius: 18rpx; background: transparent; }
.list-scroll { white-space: nowrap; }
.list-scroll .product { display: inline-flex; width: 430rpx; margin-right: 18rpx; align-items: center; }
.list-scroll .pinfo { flex: 1; }

/* 骨架屏 */
.skeleton { border: 0; box-shadow: none; background: #fff; }
.skeleton-image { height: 330rpx; border-radius: 20rpx; background: #f2f2f4; animation: pulse 1.2s ease-in-out infinite; }
.skeleton-line { height: 24rpx; margin: 20rpx 20rpx 0; border-radius: 8rpx; background: #f2f2f4; animation: pulse 1.2s ease-in-out infinite; }
.skeleton-line.short { width: 40%; }
@keyframes pulse { 0%, 100% { opacity: 1; } 50% { opacity: .55; } }
</style>
