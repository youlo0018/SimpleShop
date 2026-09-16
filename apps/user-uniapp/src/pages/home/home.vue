<template>
  <view class="page" :style="pageStyle">
    <view v-if="!platformCode" class="empty">
      <text>请先选择平台</text>
      <button class="primary" @tap="goSelector">选择平台</button>
    </view>

    <template v-else>
      <view class="header">
        <view class="brand-row"><view><view class="app-name">{{ design.home.appName || platform?.platformName }}</view><view class="slogan">{{ design.home.slogan || platform?.platformCode }}</view></view><text class="switch" @tap="goSelector">切换</text></view>
        <view class="search"><input v-model="keyword" placeholder="搜索本平台商品" confirm-type="search" @confirm="search" /><text @tap="search">搜索</text></view>
      </view>

      <view v-if="design.home.notice" class="notice"><text class="notice-tag">公告</text><text>{{ design.home.notice }}</text></view>

      <swiper v-if="bannerItems.length" class="banners" circular autoplay :interval="4200">
        <swiper-item v-for="banner in bannerItems" :key="banner.image + banner.title"><image :src="banner.image || fallback" mode="aspectFill" @tap="goLink(banner)" /></swiper-item>
      </swiper>

      <view v-for="(module, index) in modules" :key="`${module.type}-${index}`" class="block">
        <view v-if="module.title" class="block-title"><text>{{ module.title }}</text></view>

        <view v-if="module.type === 'quickNav'" class="quick">
          <view v-for="item in module.items || []" :key="item.title" @tap="goLink(item)"><text class="icon">{{ item.icon || '🎁' }}</text><text class="icon-label">{{ item.title }}</text></view>
        </view>

        <scroll-view v-if="module.type === 'categories'" scroll-x class="category-scroll">
          <view v-for="category in visibleCategories(module)" :key="category.id" class="category" :style="{ background: themeCss.primary + '14', color: themeCss.primary }" @tap="goCategory(category.id)">{{ category.name }}</view>
        </scroll-view>

        <template v-if="module.type === 'products'">
          <view v-if="sectionLoading(module)" class="empty">加载中...</view>
          <view v-else-if="!sectionProducts(module).length" class="empty">本模块暂无商品</view>
          <scroll-view v-else-if="module.layout === 'list'" scroll-x class="list-scroll">
            <view v-for="item in sectionProducts(module)" :key="`${module.key}-${item.id}`" class="product row" @tap="goProduct(item)"><image :src="item.mainImage || fallback" mode="aspectFill" /><view class="pinfo"><view class="pname">{{ item.name }}</view><view class="price"><text class="yen">¥</text>{{ minPrice(item) }}</view></view></view>
          </scroll-view>
          <view v-else class="grid">
            <view v-for="item in sectionProducts(module)" :key="`${module.key}-${item.id}`" class="product" @tap="goProduct(item)"><image :src="item.mainImage || fallback" mode="aspectFill" /><view class="pinfo"><view class="pname">{{ item.name }}</view><view class="price"><text class="yen">¥</text>{{ minPrice(item) }}</view></view></view>
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
import { getPlatform, getPlatformCode, getTheme } from '@/common/store'

const fallback = 'https://dummyimage.com/600x600/edf2f7/94a3b8&text=Shop'
const design = ref({ home: {}, theme: {}, modules: [] })
const platform = ref(null); const platformCode = ref(''); const keyword = ref('')
const theme = ref(getTheme()); const categories = ref([]); const productsByModule = ref({}); const loadingModules = ref({})

const pageStyle = computed(() => ({ background: design.value.theme?.background || theme.value.background, minHeight: '100vh' }))
const themeCss = computed(() => ({ primary: design.value.theme?.primary || theme.value.primary }))
const modules = computed(() => design.value.home?.modules || [])
const bannerItems = computed(() => (design.value.home?.banners || []).filter(item => item.image))

const goSelector = () => uni.navigateTo({ url: '/pages/platform/selector' })
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
  uni.navigateTo({ url: `/pages/category/category?id=${item.linkValue || item.value || ''}` })
}
const goCategory = categoryId => uni.navigateTo({ url: `/pages/category/category?id=${categoryId}` })
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
  await loadModules()
})
</script>

<style scoped>
.empty { text-align: center; color: #86868b; padding: 100rpx 0; }
.empty button { margin-top: 24rpx; width: 240rpx; color: #fff; background: #0071e3; }
/* Apple 风头部：白底大标题 + iOS 灰搜索框 */
.header { padding: 38rpx 30rpx 30rpx; background: rgba(255, 255, 255, .86); backdrop-filter: blur(20px); }
.brand-row { display: flex; align-items: flex-start; justify-content: space-between; }
.app-name { font-size: 46rpx; font-weight: 800; letter-spacing: -.02em; color: #1d1d1f; } .slogan { color: #86868b; margin-top: 8rpx; font-size: 24rpx; }
.switch { background: #e9e9eb; color: #1d1d1f; padding: 10rpx 24rpx; border-radius: 980px; font-size: 24rpx; font-weight: 500; }
.search { display: flex; align-items: center; background: #e9e9eb; color: #8e8e93; height: 72rpx; border-radius: 980px; padding: 0 28rpx; margin-top: 26rpx; }
.search input { flex: 1; height: 100%; } .search text { color: #0071e3; font-weight: 600; }
.notice { display: flex; gap: 12rpx; align-items: center; margin: 22rpx 24rpx 0; background: #fff; border-radius: 20rpx; padding: 20rpx 24rpx; color: #6e6e73; font-size: 24rpx; }
.notice-tag { background: rgba(0, 113, 227, .1); color: #0071e3; padding: 4rpx 12rpx; border-radius: 980px; font-size: 20rpx; font-weight: 600; }
.banners { height: 300rpx; margin: 22rpx 24rpx 0; border-radius: 24rpx; overflow: hidden; box-shadow: 0 2rpx 12rpx rgba(0, 0, 0, .05); }
.banners image { width: 100%; height: 100%; }
.block { background: #fff; margin: 22rpx 24rpx; border-radius: 28rpx; padding: 28rpx; box-shadow: 0 2rpx 12rpx rgba(0, 0, 0, .04); }
.block-title { font-size: 34rpx; font-weight: 700; letter-spacing: -.01em; margin-bottom: 22rpx; }
.quick { display: grid; grid-template-columns: repeat(4,1fr); gap: 22rpx; text-align: center; }
.quick view { display: grid; justify-items: center; gap: 12rpx; font-size: 24rpx; color: #6e6e73; }
/* 金刚区图标底座：iOS App 图标质感 */
.quick .icon { width: 92rpx; height: 92rpx; display: grid; place-items: center; background: #f5f5f7; border-radius: 28rpx; font-size: 46rpx; }
.icon-label { line-height: 1; }
.category-scroll { white-space: nowrap; } .category { display: inline-block; padding: 16rpx 28rpx; border-radius: 980px; margin-right: 16rpx; font-size: 24rpx; font-weight: 600; }
.grid { display: grid; grid-template-columns: repeat(2,1fr); gap: 20rpx; }
.product { background: #f5f5f7; border-radius: 20rpx; overflow: hidden; }
.product image { width: 100%; height: 300rpx; } .row image { width: 190rpx; height: 190rpx; border-radius: 16rpx; }
.pinfo { padding: 18rpx 20rpx 22rpx; } .pname { height: 74rpx; overflow: hidden; font-weight: 600; letter-spacing: -.01em; display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical; }
.price { color: #1d1d1f; font-weight: 700; margin-top: 10rpx; font-size: 32rpx; font-variant-numeric: tabular-nums; }
.price .yen { font-size: 22rpx; font-weight: 600; margin-right: 2rpx; }
.list-scroll { white-space: nowrap; } .list-scroll .product { display: inline-flex; width: 420rpx; margin-right: 18rpx; align-items: center; }
/* 隐藏 H5 横向滚动条（小程序端本就没有） */
.category-scroll::-webkit-scrollbar, .list-scroll::-webkit-scrollbar { display: none; }
</style>
