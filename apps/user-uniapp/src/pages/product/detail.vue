<template>
  <view class="page">
    <!-- 商品图集：主图 + SKU 图去重，支持滑动与指示点 -->
    <swiper v-if="images.length" class="gallery" :indicator-dots="images.length > 1" indicator-color="rgba(0,0,0,.18)" indicator-active-color="#1d1d1f" circular>
      <swiper-item v-for="(image, index) in images" :key="index"><image class="gallery-image" :src="image" mode="aspectFill" @tap="previewImage(index)" /></swiper-item>
    </swiper>
    <image v-else class="gallery-image" :src="fallback" mode="aspectFill" />

    <!-- #ifdef MP-WEIXIN -->
    <view class="share-fab" @tap="shareHint"><text>↗</text></view>
    <!-- #endif -->

    <view class="card main-card">
      <view class="price-row">
        <view class="price" :style="{ color: theme.primary }"><text class="yen">¥</text>{{ selected?.price || '--' }}</view>
        <text v-if="originalPrice > 0" class="origin">¥{{ originalPrice.toFixed(2) }}</text>
        <text class="stock">库存 {{ selected?.stock || 0 }}</text>
      </view>
      <view v-if="promoText" class="promo-pill"><text class="promo-pill-icon">🎫</text><text>{{ promoText }}</text></view>
      <view class="name">{{ product.name }}</view>
      <view class="services">
        <view class="service"><text class="service-icon">🛡</text><text>正品保证</text></view>
        <view class="service"><text class="service-icon">🚚</text><text>极速发货</text></view>
        <view class="service"><text class="service-icon">↩️</text><text>7天无理由</text></view>
      </view>
    </view>

    <!-- 配送信息：平台发货，下单后可在订单详情跟踪 -->
    <view class="card row-card">
      <text class="row-label">配送</text>
      <text class="row-value">全国配送 · 平台发货</text>
    </view>

    <view class="card">
      <view class="section">选择规格</view>
      <view class="skus">
        <view v-for="sku in product.skus || []" :key="sku.id" :class="['sku', selected?.id === sku.id && 'on']" :style="selected?.id === sku.id ? { color: theme.primary, borderColor: theme.primary, background: theme.primary + '12' } : {}" @tap="selected = sku">
          <text class="spec">{{ sku.specName || '规格' }}：{{ sku.specValue || sku.skuCode }}</text>
          <text class="spec-price">¥{{ sku.price }} / 库存{{ sku.stock }}</text>
        </view>
      </view>
      <view class="qty">
        <text class="qty-label">购买数量</text>
        <view class="stepper">
          <text class="step" @tap="stepDown">-</text>
          <input v-model="quantity" type="number" @blur="normalizeQuantity" />
          <text class="step" @tap="stepUp">+</text>
        </view>
      </view>
    </view>

    <!-- 商品评价：评价体系尚未接入，如实展示占位 -->
    <view class="card">
      <view class="section-row">
        <text class="section">商品评价（0）</text>
        <text class="muted">暂无评价</text>
      </view>
    </view>

    <view class="card">
      <view class="section">商品详情</view>
      <view class="desc" v-html="product.description || '本平台精选商品'"></view>
    </view>

    <!-- 悬浮回首页（参考图右下角圆形按钮） -->
    <view class="home-fab" @tap="goHome"><text class="home-fab-icon">⌂</text><text class="home-fab-text">首页</text></view>

    <!-- 吸底（参考图）：店铺 / 购物车（角标）/ 加入购物车（橙黄渐变）/ 立即购买（红渐变） -->
    <view class="actions safe-bottom">
      <view class="entry" @tap="goShop">
        <text class="entry-icon">🏬</text><text class="entry-text">店铺</text>
      </view>
      <view class="entry" @tap="goCart">
        <view class="entry-badge-wrap">
          <text class="entry-icon">🛒</text>
          <text v-if="cartCount > 0" class="entry-badge">{{ cartCount > 99 ? '99+' : cartCount }}</text>
        </view>
        <text class="entry-text">购物车</text>
      </view>
      <view class="buy-row">
        <button class="cart-btn" @tap="addToCart">加入购物车</button>
        <button class="buy-btn" @tap="buyNow">立即购买</button>
      </view>
    </view>
  </view>
</template>

<script setup>
import { computed, ref } from 'vue'
import { onLoad, onShareAppMessage } from '@dcloudio/uni-app'
import { get, post } from '@/common/request'
import { getPlatform, getTheme, isLogin, requireLogin } from '@/common/store'
import { toast, validateQuantity } from '@/common/validators'

const fallback = '/static/placeholder.png'
const theme = ref(getTheme()); const product = ref({}); const selected = ref(null); const quantity = ref(1)
const cartCount = ref(0)

// 图集 = 主图 + SKU 图去重（最多 8 张），加载失败时回落占位图。
const images = computed(() => {
  const list = [product.value.mainImage, ...(product.value.skus || []).map(sku => sku.image)]
    .filter(Boolean)
  return [...new Set(list)].slice(0, 8)
})
const originalPrice = computed(() => {
  const value = Number(selected.value?.originalPrice || 0)
  const price = Number(selected.value?.price || 0)
  return value > price ? value : 0
})
// 购买数量上限 = min(库存, 99)：99 与下单/支付/库存链路的后端上限一致。
const maxQuantity = computed(() => Math.min(99, Number(selected.value?.stock || 0)))
const stepDown = () => { if (Number(quantity.value) > 1) quantity.value = Number(quantity.value) - 1 }
const stepUp = () => {
  const next = Number(quantity.value || 0) + 1
  if (!validateQuantity(next)) return
  if (next > maxQuantity.value) return uni.showToast({ title: `不能超过库存 ${maxQuantity.value}`, icon: 'none' })
  quantity.value = next
}
const normalizeQuantity = () => {
  const value = Math.floor(Number(quantity.value) || 1)
  quantity.value = Math.max(1, Math.min(maxQuantity.value || 1, value))
}
const checkQuantity = () => {
  if (!selected.value) return toast('请选择规格')
  if (Number(selected.value.stock) <= 0) return toast('该规格库存不足')
  if (!validateQuantity(quantity.value)) return false
  if (Number(quantity.value) > maxQuantity.value) return toast(`数量不能超过库存 ${maxQuantity.value}`)
  return true
}

// 活动标签：取全平台范围内、当前进行中的活动，展示"满X减Y"提示（实际优惠在结算页计算）。
const promoText = ref('')
const loadPromo = async () => {
  const platformId = product.value.platformId || getPlatform()?.id || 0
  const data = await get('/marketing/ActiveActivities', { platformId }).catch(() => null)
  const activity = (data?.activities || []).find(item => Number(item.scopeType) === 1)
  if (!activity) return
  promoText.value = Number(activity.activityType) === 1 ? `全场满${Number(activity.threshold)}减${Number(activity.discountValue)}`
    : Number(activity.activityType) === 2 ? `全场满${Number(activity.threshold)}打${(Number(activity.discountValue) * 10).toFixed(1)}折`
      : `全场满${Number(activity.threshold)}赠券`
}
const goHome = () => uni.switchTab({ url: '/pages/home/home' })
const previewImage = index => uni.previewImage({ current: index, urls: images.value })
const shareHint = () => uni.showToast({ title: '点右上角「···」分享给好友', icon: 'none' })
const goCart = () => uni.switchTab({ url: '/pages/cart/cart' })
const goShop = () => {
  const merchantId = selected.value?.merchantId || product.value.merchantId
  if (!merchantId) return uni.showToast({ title: '店铺信息缺失', icon: 'none' })
  uni.navigateTo({ url: `/pages/shop/shop?id=${merchantId}` })
}
const addToCart = async () => {
  if (!requireLogin()) return
  if (!checkQuantity()) return
  await post('/carts/Add', {
    userId: 0, productId: product.value.id, skuId: selected.value.id, merchantId: selected.value.merchantId,
    platformId: selected.value.platformId, productName: product.value.name,
    image: selected.value.image || product.value.mainImage || '',
    price: Number(selected.value.price), quantity: Number(quantity.value)
  })
  uni.showToast({ title: '已加入购物车' })
  loadCartCount()
}
const buyNow = () => {
  if (!requireLogin()) return
  if (!checkQuantity()) return
  uni.setStorageSync('checkout', [{
    skuId: selected.value.id, productId: product.value.id, merchantId: selected.value.merchantId,
    platformId: selected.value.platformId, productName: product.value.name, mainImage: selected.value.image || product.value.mainImage,
    price: Number(selected.value.price), quantity: Number(quantity.value), checked: true
  }])
  uni.navigateTo({ url: '/pages/checkout/checkout?type=buy' })
}
// 购物车角标：登录后统计数量合计，未登录为 0。
const loadCartCount = async () => {
  if (!isLogin()) { cartCount.value = 0; return }
  const data = await get('/carts/Get').catch(() => null)
  cartCount.value = (data?.items || []).reduce((sum, item) => sum + Number(item.quantity || 0), 0)
}
onShareAppMessage(() => ({ title: product.value.name || '精选好物', path: `/pages/product/detail?id=${product.value.id}` }))
onLoad(async options => {
  theme.value = getTheme()
  const data = await get('/products/GetProductDetail', { id: options.id })
  product.value = { ...data.product, skus: (data.skus || []).filter(sku => sku.isActive !== false) }
  selected.value = product.value.skus[0] || null
  uni.setNavigationBarTitle({ title: product.value.name || '商品详情' })
  loadCartCount()
  loadPromo()
})
</script>

<style scoped>
.page { padding-bottom: 260rpx; }
.gallery { width: 750rpx; height: 750rpx; background: #fff; }
.gallery-image { width: 750rpx; height: 750rpx; background: #fff; }
.share-fab { position: fixed; right: 28rpx; top: 28rpx; width: 72rpx; height: 72rpx; display: grid; place-items: center; border-radius: 50%; background: rgba(255, 255, 255, .92); box-shadow: 0 6rpx 20rpx rgba(0, 0, 0, .12); font-size: 34rpx; color: #1d1d1f; z-index: 20; }
.card { background: #fff; border-radius: 32rpx; margin: 22rpx 24rpx; padding: 32rpx; box-shadow: 0 8rpx 28rpx rgba(0, 0, 0, .05); }
.main-card { margin-top: 24rpx; }
.row-card { display: flex; align-items: center; gap: 24rpx; padding: 26rpx 32rpx; }
.row-label { color: #86868b; font-size: 26rpx; }
.row-value { color: #1d1d1f; font-size: 26rpx; font-weight: 500; }

.price-row { display: flex; align-items: baseline; gap: 14rpx; }
.price { font-size: 56rpx; font-weight: 800; font-variant-numeric: tabular-nums; line-height: 1.05; }
.price .yen { font-size: 30rpx; font-weight: 700; margin-right: 4rpx; }
.origin { color: #c7c7cc; text-decoration: line-through; font-size: 26rpx; }
.stock { margin-left: auto; font-size: 22rpx; font-weight: 500; color: #86868b; background: #f5f5f7; padding: 6rpx 18rpx; border-radius: 980px; }
.name { font-size: 36rpx; font-weight: 700; letter-spacing: -.01em; margin: 20rpx 0 22rpx; line-height: 48rpx; }
.promo-pill { display: inline-flex; align-items: center; gap: 8rpx; background: #fff3e6; color: #ff6a00; font-size: 24rpx; font-weight: 700; padding: 8rpx 20rpx; border-radius: 12rpx; margin-bottom: 20rpx; }
.promo-pill-icon { font-size: 24rpx; }

/* 服务保障：低成本高信任 */
.services { display: flex; gap: 12rpx; padding-top: 22rpx; border-top: 1rpx solid rgba(60, 60, 67, .06); }
.service { flex: 1; display: flex; align-items: center; justify-content: center; gap: 8rpx; color: #6e6e73; font-size: 22rpx; }
.service-icon { font-size: 24rpx; }

.section { font-weight: 700; letter-spacing: -.01em; font-size: 30rpx; }
.section-row { display: flex; align-items: center; justify-content: space-between; }
.muted { color: #a1a1a6; font-size: 24rpx; }
.skus { display: grid; grid-template-columns: repeat(2, 1fr); gap: 18rpx; margin: 22rpx 0 32rpx; }
.sku { padding: 22rpx; background: #f5f5f7; border: 2rpx solid transparent; border-radius: 22rpx; display: grid; gap: 8rpx; font-size: 24rpx; transition: all .15s ease; }
.sku.on { font-weight: 700; box-shadow: 0 6rpx 16rpx rgba(0, 0, 0, .05); }
.spec { font-size: 26rpx; } .spec-price { font-variant-numeric: tabular-nums; }

.qty { display: flex; justify-content: space-between; align-items: center; }
.qty-label { font-size: 28rpx; color: #1d1d1f; font-weight: 500; }
.stepper { display: flex; align-items: center; background: #f5f5f7; border-radius: 980px; padding: 2rpx; }
.stepper .step { width: 68rpx; text-align: center; padding: 12rpx 0; font-weight: 600; }
.stepper input { width: 86rpx; text-align: center; font-weight: 600; }

.desc { color: #6e6e73; line-height: 1.7; font-size: 27rpx; margin-top: 20rpx; }

/* 底部操作条：店铺 + 购物车 + 双主按钮 */
.actions { position: fixed; left: 0; right: 0; bottom: 0; display: flex; gap: 14rpx; align-items: center; background: rgba(255, 255, 255, .95); backdrop-filter: blur(20px) saturate(180%); border-top: 1rpx solid rgba(0, 0, 0, .05); padding: 18rpx 24rpx; }
.entry { display: grid; justify-items: center; gap: 2rpx; width: 92rpx; flex-shrink: 0; }
.entry-badge-wrap { position: relative; }
.entry-icon { font-size: 40rpx; line-height: 1; }
.entry-badge { position: absolute; top: -10rpx; right: -18rpx; min-width: 30rpx; height: 30rpx; padding: 0 8rpx; border-radius: 980px; background: #ff3b30; color: #fff; font-size: 19rpx; font-weight: 700; display: grid; place-items: center; }
.entry-text { font-size: 20rpx; color: #6e6e73; }
.buy-row { flex: 1; display: flex; border-radius: 980rpx; overflow: hidden; box-shadow: 0 10rpx 24rpx rgba(255, 90, 60, .28); }
.buy-row button { flex: 1; height: 88rpx; display: flex; align-items: center; justify-content: center; font-size: 29rpx; font-weight: 600; color: #fff; border-radius: 0; }
.cart-btn { background: linear-gradient(135deg, #ffc53d 0%, #ff9f0a 100%); }
.buy-btn { background: linear-gradient(135deg, #ff6a3d 0%, #ff2d55 100%); }
.home-fab { position: fixed; right: 28rpx; bottom: 200rpx; width: 92rpx; height: 92rpx; display: grid; place-items: center; border-radius: 50%; background: rgba(255, 255, 255, .95); box-shadow: 0 10rpx 26rpx rgba(0, 0, 0, .16); z-index: 20; }
.home-fab-icon { font-size: 34rpx; line-height: 1; color: #1d1d1f; }
.home-fab-text { font-size: 18rpx; color: #6e6e73; }
</style>
