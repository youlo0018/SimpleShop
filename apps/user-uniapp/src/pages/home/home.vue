<template>
  <view class="page" :style="pageStyle">
    <!-- 铺满头图：轮播图/主题渐变 + 悬浮定位/评分/搜索（参考凯德星首页） -->
    <view class="hero-wrap">
      <swiper v-if="bannerItems.length" class="hero" circular autoplay :interval="4200" @change="onBannerChange">
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

      <!-- 顶部悬浮：定位 + 评分/消息 -->
      <view class="hero-top">
        <view class="hero-loc"><text class="loc-icon">📍</text><text class="loc-text">{{ platform?.platformName || '商城' }}</text><text class="loc-caret">∨</text></view>
        <view class="hero-right">
          <view class="hero-score"><text class="score-star">☆</text><text>2.0</text></view>
          <view class="hero-msg">💬</view>
        </view>
      </view>

      <view v-if="bannerItems.length > 1" class="hero-indicator">{{ bannerIndex + 1 }} / {{ bannerItems.length }}</view>

      <!-- 悬浮搜索 -->
      <view class="hero-search">
        <text class="hero-search-icon">🔍</text>
        <input v-model="keyword" placeholder="搜索" confirm-type="search" @confirm="search" />
        <text class="hero-search-action" @tap="search">搜索</text>
      </view>
    </view>

    <!-- 金刚区（参考图：白卡四宫格线性图标） -->
    <view v-if="quickItems.length" class="quick-card">
      <view v-for="(item, index) in quickItems" :key="`${item.title}-${index}`" class="quick-item" @tap="goLink(item)">
        <image v-if="isImageIcon(item.icon)" class="quick-icon-img" :src="item.icon" mode="aspectFit" />
        <text v-else class="quick-icon">{{ item.icon || '•' }}</text>
        <text class="quick-label">{{ item.title }}</text>
      </view>
    </view>

    <!-- 会员问候卡（参考图：Hi + 等级 chip + 三栏账户） -->
    <view v-if="logged" class="greeting-card">
      <view class="greeting-hi">Hi {{ user.userName || '会员' }}，{{ greeting }}</view>
      <view class="greeting-chip" :style="{ background: tierGradient }">{{ memberTier.name }}</view>
      <view class="greeting-stats">
        <view class="stat" @tap="goMine"><text class="stat-num">{{ stats.coupons }}<text class="stat-unit">张</text></text><text class="stat-label">优惠券</text></view>
        <view class="stat" @tap="goMine"><text class="stat-num">{{ stats.favorites }}</text><text class="stat-label">我的收藏</text></view>
        <view class="stat" @tap="goMine"><text class="stat-num">{{ stats.orders }}</text><text class="stat-label">订单</text></view>
      </view>
    </view>
    <view v-else class="greeting-card guest" @tap="goLogin">
      <text class="guest-text">Hi，登录后查看优惠券与订单 ›</text>
    </view>

    <!-- 优惠专区（进行中活动/可领券，参考商城页） -->
    <view v-if="promotions.length" class="block">
      <view class="block-title">
        <text class="title">优惠专区</text>
        <text class="more" @tap="goCategory('')">查看更多 ›</text>
      </view>
      <scroll-view scroll-x class="promo-scroll">
        <view v-for="item in promotions" :key="item.key" class="promo-card" :style="{ background: themeCss.primary + '10' }" @tap="goPromotion(item)">
          <view class="promo-tag" :style="{ background: themeCss.primary }">{{ item.tag }}</view>
          <view class="promo-name">{{ item.name }}</view>
          <view class="promo-benefit" :style="{ color: themeCss.primary }">{{ item.benefit }}</view>
          <view class="promo-time">{{ item.timeText }}</view>
        </view>
      </scroll-view>
    </view>

    <!-- 装修模块（后台可视化配置） -->
    <view v-for="module in visibleModules" :key="module.key || module.title" class="block">
      <view class="block-title">
        <text class="title">{{ module.title || moduleSubtitle(module) }}</text>
        <text v-if="module.type === 'products'" class="more" @tap="goCategory(module.categoryId)">查看更多 ›</text>
      </view>

      <scroll-view v-if="module.type === 'categories'" scroll-x class="category-scroll">
        <view v-for="category in visibleCategories(module)" :key="category.id" class="category" @tap="goCategory(category.id)">{{ category.name }}</view>
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
            <view class="pinfo"><view class="pname">{{ item.name }}</view><view class="price"><text class="yen">¥</text>{{ deal(item).finalPrice }}</view></view>
          </view>
        </scroll-view>
        <view v-else-if="module.layout === 'grid3'" class="grid3">
          <view v-for="item in sectionProducts(module)" :key="`${module.key}-${item.id}`" class="product compact" @tap="goProduct(item)">
            <image :src="item.mainImage || fallback" mode="aspectFill" />
            <view class="pinfo">
              <view class="pname">{{ item.name }}</view>
              <view class="price"><text class="yen">¥</text>{{ deal(item).finalPrice }}</view>
            </view>
          </view>
        </view>
        <view v-else class="grid">
          <view v-for="item in sectionProducts(module)" :key="`${module.key}-${item.id}`" class="product" @tap="goProduct(item)">
            <image :src="item.mainImage || fallback" mode="aspectFill" />
            <view class="pinfo">
              <view class="pname">{{ item.name }}</view>
              <view class="price-row"><view class="price"><text class="yen">¥</text>{{ deal(item).finalPrice }}</view><text class="buy-dot" :style="{ color: themeCss.primary, background: themeCss.primary + '14' }">＋</text></view>
            </view>
          </view>
        </view>
      </template>
    </view>
  </view>
</template>

<script setup>
import { onShow } from '@dcloudio/uni-app'
import { computed, ref } from 'vue'
import { get } from '@/common/request'
import { loadPlatformDesign } from '@/common/design'
import { isImageIcon } from '@/common/icon'
import { getPlatform, getPlatformCode, getTheme, getUser, isLogin } from '@/common/store'
import { ensurePlatform } from '@/common/platform-config'
import { memberTierOf } from '@/common/member'
import { enrichFinalPrices, priceInfo } from '@/common/final-price'

const fallback = '/static/placeholder.png'
const design = ref({ home: {}, theme: {}, modules: [], profile: {} })
const platform = ref(null); const platformCode = ref(''); const keyword = ref('')
const theme = ref(getTheme()); const categories = ref([]); const productsByModule = ref({}); const loadingModules = ref({})
const logged = ref(false); const user = ref({}); const stats = ref({ coupons: 0, favorites: 0, orders: 0 })
const marketActivities = ref([]); const marketCoupons = ref([]); const bannerIndex = ref(0)

const pageStyle = computed(() => ({ background: design.value.theme?.background || theme.value.background, minHeight: '100vh' }))
const themeCss = computed(() => ({ primary: design.value.theme?.primary || theme.value.primary }))
const heroGradient = computed(() => {
  const primary = themeCss.value.primary
  return `linear-gradient(135deg, ${primary} 0%, ${primary}D9 52%, ${primary}A6 100%)`
})
const tierGradient = computed(() => {
  const primary = themeCss.value.primary || '#00c1a2'
  return `linear-gradient(120deg, #8fd3e8 0%, ${primary} 55%, #8f7ae5 100%)`
})
const greeting = computed(() => {
  const hour = new Date().getHours()
  return hour < 6 ? '凌晨好' : hour < 12 ? '早上好' : hour < 18 ? '下午好' : '晚上好'
})
const memberTier = computed(() => memberTierOf(stats.value.spent))
const modules = computed(() => design.value.home?.modules || [])
const quickItems = computed(() => modules.value.find(module => module.type === 'quickNav')?.items || [])
const bannerItems = computed(() => {
  const top = (design.value.home?.banners || []).filter(item => item.image)
  if (top.length) return top
  const module = modules.value.find(item => item.type === 'banners')
  return (module?.items || []).filter(item => item.image)
})
const promotions = computed(() => [
  ...marketActivities.value.map(item => ({
    key: `a-${item.id}`, kind: 'activity', id: item.id, tag: '活动',
    name: item.name,
    benefit: Number(item.activityType) === 1 ? `满${Number(item.threshold)}减${Number(item.discountValue)}`
      : Number(item.activityType) === 2 ? `满${Number(item.threshold)}打${(Number(item.discountValue) * 10).toFixed(1)}折`
        : `满${Number(item.threshold)}赠券`,
    timeText: `${String(item.startAt || '').slice(0, 10)} ~ ${item.endAt ? String(item.endAt).slice(0, 10) : '长期'}`
  })),
  ...marketCoupons.value.map(item => ({
    key: `c-${item.id}`, kind: 'coupon', id: item.id, tag: '可领券',
    name: item.name, benefit: `剩余${item.remainingStock}张`,
    timeText: item.endAt ? `至 ${String(item.endAt).slice(0, 10)}` : '长期有效'
  }))
])
const visibleModules = computed(() => modules.value.filter(module => {
  if (module.type === 'quickNav' || module.type === 'banners') return false
  if (module.type === 'categories') return categories.value.length > 0
  return true
}))

const deal = item => priceInfo(item)
const moduleSubtitle = module => ({ categories: '探索分类', products: '精选好物' }[module.type] || '')
const onBannerChange = event => { bannerIndex.value = event.detail?.current || 0 }
const goMine = () => uni.switchTab({ url: '/pages/profile/profile' })
const goLogin = () => uni.navigateTo({ url: '/pages/auth/login' })
const goCouponCenter = () => uni.navigateTo({ url: '/pages/coupon/center' })
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
const goPromotion = item => item.kind === 'coupon' ? goCouponCenter() : goCategory('')
const loadPromotions = async () => {
  const data = await get('/marketing/ActiveActivities', { platformId: platform.value?.id || 0 }).catch(() => null)
  marketActivities.value = data?.activities || []
  marketCoupons.value = data?.coupons || []
}
const goProduct = item => {
  if (!item.skus?.length) return uni.showToast({ title: '商品缺少SKU', icon: 'none' })
  uni.navigateTo({ url: `/pages/product/detail?id=${item.id}` })
}
const search = () => {
  const text = String(keyword.value || '').trim()
  if (!text) return uni.showToast({ title: '请输入搜索关键词', icon: 'none' })
  uni.navigateTo({ url: `/pages/category/category?keyword=${encodeURIComponent(text)}` })
}

const loadModules = async () => {
  const productModules = modules.value.filter(module => module.type === 'products')
  await Promise.all(productModules.map(async module => {
    const key = module.key || module.title
    loadingModules.value[key] = true
    try {
      const data = await get('/products/List', {
        categoryId: module.categoryId || 0, merchantId: module.merchantId || 0,
        status: 1, page: 1, pageSize: module.limit || 10
      })
      productsByModule.value[key] = data.items || []
    } finally { loadingModules.value[key] = false }
  }))
  const allProducts = productModules.flatMap(module => productsByModule.value[module.key || module.title] || [])
  await enrichFinalPrices(allProducts, platform.value?.id || 0)
}

// 会员统计：券/收藏/订单 + 累计消费（订单实付累加，用于会员等级展示；仅前端计算）。
const loadMemberStats = async () => {
  logged.value = isLogin()
  user.value = getUser()
  if (!logged.value) { stats.value = { coupons: 0, favorites: 0, orders: 0, spent: 0 }; return }
  const [coupons, favorites, orders] = await Promise.all([
    get('/marketing/MyCoupons').catch(() => ({ items: [] })),
    get('/customers/Favorites').catch(() => []),
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

onShow(async () => {
  // 平台由发布配置锁定：启动时确保平台信息已就绪（失败不阻塞，下次进入重试）。
  platform.value = await ensurePlatform().catch(() => null) || getPlatform()
  platformCode.value = getPlatformCode()
  if (!platform.value) return
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
.page { padding-bottom: 170rpx; background: #f5f5f5; }
.empty { display: grid; justify-items: center; gap: 18rpx; color: #9aa0a8; padding: 160rpx 0; }
.empty .emoji { font-size: 104rpx; }
.empty.small { padding: 60rpx 0; font-size: 26rpx; }

/* 头图 + 悬浮元素（参考图首页） */
.hero-wrap { position: relative; }
.hero { width: 750rpx; height: 560rpx; }
.hero-image { width: 750rpx; height: 560rpx; }
.hero-fallback { position: relative; overflow: hidden; height: 560rpx; display: flex; flex-direction: column; justify-content: center; padding: 0 44rpx; box-sizing: border-box; }
.hero-glow { position: absolute; border-radius: 50%; background: rgba(255, 255, 255, .16); }
.hero-glow-a { width: 360rpx; height: 360rpx; right: -80rpx; top: -120rpx; }
.hero-glow-b { width: 220rpx; height: 220rpx; right: 140rpx; bottom: -110rpx; }
.hero-title { position: relative; color: #fff; font-size: 48rpx; font-weight: 800; letter-spacing: -.02em; }
.hero-sub { position: relative; color: rgba(255, 255, 255, .84); font-size: 25rpx; margin-top: 12rpx; }
.hero-top { position: absolute; left: 26rpx; right: 26rpx; top: 24rpx; display: flex; align-items: center; justify-content: space-between; }
.hero-loc { display: flex; align-items: center; gap: 6rpx; background: rgba(0, 0, 0, .22); backdrop-filter: blur(10px); border-radius: 980px; padding: 10rpx 22rpx; }
.loc-icon { font-size: 24rpx; }
.loc-text { color: #fff; font-size: 26rpx; font-weight: 600; max-width: 380rpx; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.loc-caret { color: rgba(255, 255, 255, .85); font-size: 22rpx; }
.hero-right { display: flex; align-items: center; gap: 14rpx; }
.hero-score { display: flex; align-items: center; gap: 6rpx; background: rgba(255, 255, 255, .95); color: #1a1a1a; font-size: 24rpx; font-weight: 700; padding: 10rpx 20rpx; border-radius: 980px; }
.score-star { color: #f5a623; }
.hero-msg { width: 64rpx; height: 64rpx; border-radius: 50%; background: rgba(255, 255, 255, .95); display: grid; place-items: center; font-size: 30rpx; }
.hero-indicator { position: absolute; left: 26rpx; bottom: 106rpx; background: rgba(0, 0, 0, .38); color: #fff; font-size: 22rpx; padding: 4rpx 18rpx; border-radius: 980px; }
.hero-search { position: absolute; left: 26rpx; right: 26rpx; bottom: -36rpx; z-index: 6; display: flex; align-items: center; gap: 12rpx; background: rgba(255, 255, 255, .97); height: 84rpx; border-radius: 980px; padding: 0 30rpx; box-shadow: 0 12rpx 32rpx rgba(0, 0, 0, .12); }
.hero-search-icon { font-size: 26rpx; opacity: .45; }
.hero-search input { flex: 1; height: 100%; font-size: 27rpx; }
.hero-search-action { font-weight: 600; font-size: 27rpx; color: #1a1a1a; }

/* 金刚区白卡 */
.quick-card { display: grid; grid-template-columns: repeat(4, 1fr); gap: 28rpx 12rpx; background: #fff; margin: 58rpx 24rpx 0; padding: 34rpx 24rpx 30rpx; border-radius: 28rpx; box-shadow: 0 10rpx 30rpx rgba(0, 0, 0, .06); }
.quick-item { display: grid; justify-items: center; gap: 14rpx; }
.quick-icon-img { width: 72rpx; height: 72rpx; }
.quick-icon { width: 72rpx; height: 72rpx; display: grid; place-items: center; font-size: 38rpx; }
.quick-label { font-size: 23rpx; color: #1a1a1a; }

/* 会员问候卡 */
.greeting-card { background: #fff; margin: 24rpx; border-radius: 28rpx; padding: 34rpx 32rpx 12rpx; box-shadow: 0 8rpx 26rpx rgba(0, 0, 0, .05); }
.greeting-card.guest { padding: 40rpx 32rpx; }
.guest-text { font-size: 28rpx; color: #6b7280; }
.greeting-hi { font-size: 38rpx; font-weight: 800; letter-spacing: -.02em; color: #1a1a1a; }
.greeting-chip { display: inline-block; margin-top: 18rpx; color: #fff; font-size: 22rpx; font-weight: 600; padding: 8rpx 22rpx; border-radius: 10rpx; }
.greeting-stats { display: flex; margin-top: 30rpx; border-top: 1rpx solid rgba(60, 60, 67, .06); }
.stat { flex: 1; display: grid; justify-items: center; gap: 8rpx; padding: 28rpx 0; }
.stat-num { font-size: 44rpx; font-weight: 800; font-variant-numeric: tabular-nums; color: #1a1a1a; }
.stat-unit { font-size: 24rpx; font-weight: 600; margin-left: 4rpx; }
.stat-label { font-size: 24rpx; color: #8a8f99; }

.block { background: #fff; margin: 24rpx; border-radius: 28rpx; padding: 32rpx; box-shadow: 0 8rpx 26rpx rgba(0, 0, 0, .05); }
.block-title { display: flex; align-items: flex-end; justify-content: space-between; margin-bottom: 26rpx; }
.block-title .title { font-size: 36rpx; font-weight: 800; letter-spacing: -.02em; color: #1a1a1a; }
.block-title .more { font-size: 25rpx; color: #9aa0a8; }

/* 优惠专区 */
.promo-scroll { white-space: nowrap; }
.promo-scroll::-webkit-scrollbar { display: none; }
.promo-card { display: inline-flex; flex-direction: column; gap: 10rpx; width: 320rpx; margin-right: 18rpx; padding: 24rpx; border-radius: 22rpx; box-sizing: border-box; vertical-align: top; }
.promo-tag { align-self: flex-start; color: #fff; font-size: 20rpx; font-weight: 700; padding: 4rpx 14rpx; border-radius: 980px; }
.promo-name { font-size: 28rpx; font-weight: 700; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; color: #1a1a1a; }
.promo-benefit { font-size: 32rpx; font-weight: 800; }
.promo-time { font-size: 21rpx; color: #9aa0a8; }

.category-scroll { white-space: nowrap; }
.category { display: inline-block; padding: 18rpx 32rpx; border-radius: 980px; margin-right: 16rpx; font-size: 26rpx; font-weight: 600; background: #f2f3f5; color: #4b5563; }
.category-scroll::-webkit-scrollbar, .list-scroll::-webkit-scrollbar { display: none; }

/* 商品卡 */
.grid { display: grid; grid-template-columns: repeat(2, 1fr); gap: 20rpx; }
.grid3 { display: grid; grid-template-columns: repeat(3, 1fr); gap: 14rpx; }
.product { background: #fff; border-radius: 20rpx; overflow: hidden; box-shadow: 0 6rpx 20rpx rgba(0, 0, 0, .04); }
.product image { width: 100%; height: 330rpx; background: #f7f7f9; }
.product.compact image { height: 220rpx; }
.pinfo { padding: 18rpx 20rpx 22rpx; }
.pname { height: 72rpx; overflow: hidden; font-weight: 500; font-size: 27rpx; line-height: 36rpx; color: #1a1a1a; display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical; }
.product.compact .pname { height: 100rpx; font-size: 24rpx; line-height: 33rpx; -webkit-line-clamp: 3; }
.price-row { display: flex; align-items: center; justify-content: space-between; margin-top: 14rpx; }
.price { color: #e93b3d; font-weight: 800; font-size: 36rpx; font-variant-numeric: tabular-nums; }
.product.compact .price { font-size: 30rpx; margin-top: 8rpx; }
.price .yen { font-size: 23rpx; font-weight: 700; margin-right: 2rpx; }
.buy-dot { width: 46rpx; height: 46rpx; border-radius: 50%; display: grid; place-items: center; font-size: 28rpx; font-weight: 700; }
.row { border: 0; background: #f5f5f5; box-shadow: none; }
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
