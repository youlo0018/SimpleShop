<template>
  <view class="page" :style="pageStyle">
    <view v-if="!platformCode" class="empty">
      <text>请先选择平台</text>
      <button class="primary" @tap="goSelector">选择平台</button>
    </view>

    <template v-else>
      <view class="header" :style="{ background: themeCss.primary }">
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
          <view v-for="item in module.items || []" :key="item.title" @tap="goLink(item)"><text class="icon">{{ item.icon || '🎁' }}</text><text>{{ item.title }}</text></view>
        </view>

        <scroll-view v-if="module.type === 'categories'" scroll-x class="category-scroll">
          <view v-for="category in visibleCategories(module)" :key="category.id" class="category" :style="{ background: themeCss.primary + '14', color: themeCss.primary }" @tap="goCategory(category.id)">{{ category.name }}</view>
        </scroll-view>

        <template v-if="module.type === 'products'">
          <view v-if="sectionLoading(module)" class="empty">加载中...</view>
          <view v-else-if="!sectionProducts(module).length" class="empty">本模块暂无商品</view>
          <scroll-view v-else-if="module.layout === 'list'" scroll-x class="list-scroll">
            <view v-for="item in sectionProducts(module)" :key="`${module.key}-${item.id}`" class="product row" @tap="goProduct(item)"><image :src="item.mainImage || fallback" mode="aspectFill" /><view class="pinfo"><view class="pname">{{ item.name }}</view><view class="price">¥{{ minPrice(item) }}</view></view></view>
          </scroll-view>
          <view v-else class="grid">
            <view v-for="item in sectionProducts(module)" :key="`${module.key}-${item.id}`" class="product" @tap="goProduct(item)"><image :src="item.mainImage || fallback" mode="aspectFill" /><view class="pinfo"><view class="pname">{{ item.name }}</view><view class="price">¥{{ minPrice(item) }}</view></view></view>
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
const search = () => uni.navigateTo({ url: `/pages/category/category?keyword=${encodeURIComponent(keyword.value)}` })

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
.empty { text-align: center; color: #667085; padding: 100rpx 0; }
.empty button { margin-top: 24rpx; width: 240rpx; color: #fff; background: #ff4d6d; }
.header { padding: 38rpx 30rpx 34rpx; color: #fff; border-radius: 0 0 32rpx 32rpx; }
.brand-row { display: flex; align-items: flex-start; justify-content: space-between; }
.app-name { font-size: 42rpx; font-weight: 800; } .slogan { opacity: .85; margin-top: 8rpx; font-size: 24rpx; }
.switch { background: rgba(255,255,255,.2); padding: 8rpx 18rpx; border-radius: 28rpx; font-size: 24rpx; }
.search { display: flex; align-items: center; background: #fff; color: #8a94a6; height: 76rpx; border-radius: 38rpx; padding: 0 26rpx; margin-top: 30rpx; }
.search input { flex: 1; height: 100%; } .search text { color: #ff4d6d; font-weight: 700; }
.notice { display: flex; gap: 12rpx; align-items: center; margin: 22rpx 24rpx 0; background: #fff; border-radius: 18rpx; padding: 18rpx 20rpx; color: #50607a; font-size: 24rpx; }
.notice-tag { background: #fff1f3; color: #ff4d6d; padding: 4rpx 10rpx; border-radius: 8rpx; font-size: 20rpx; }
.banners { height: 300rpx; margin: 22rpx 24rpx 0; border-radius: 24rpx; overflow: hidden; }
.banners image { width: 100%; height: 100%; }
.block { background: #fff; margin: 22rpx 24rpx; border-radius: 24rpx; padding: 24rpx; }
.block-title { font-size: 32rpx; font-weight: 700; margin-bottom: 20rpx; }
.quick { display: grid; grid-template-columns: repeat(4,1fr); gap: 22rpx; text-align: center; }
.quick view { display: grid; gap: 8rpx; font-size: 24rpx; color: #50607a; } .icon { font-size: 42rpx; }
.category-scroll { white-space: nowrap; } .category { display: inline-block; padding: 16rpx 24rpx; border-radius: 26rpx; margin-right: 16rpx; font-size: 24rpx; font-weight: 600; }
.grid { display: grid; grid-template-columns: repeat(2,1fr); gap: 20rpx; }
.product { background: #fff; border-radius: 18rpx; overflow: hidden; box-shadow: 0 6rpx 18rpx rgba(15,23,42,.05); }
.product image { width: 100%; height: 300rpx; } .row image { width: 190rpx; height: 190rpx; }
.pinfo { padding: 18rpx; } .pname { height: 74rpx; overflow: hidden; font-weight: 600; display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical; }
.price { color: #ff4d6d; font-weight: 800; margin-top: 10rpx; }
.list-scroll { white-space: nowrap; } .list-scroll .product { display: inline-flex; width: 420rpx; margin-right: 18rpx; align-items: center; }
</style>
