<template>
  <view class="page">
    <image class="cover" :src="selected?.image || product.mainImage || fallback" mode="aspectFill" />
    <view class="card">
      <view class="price" :style="{ color: theme.primary }">¥{{ selected?.price || '--' }}</view>
      <view class="name">{{ product.name }}</view>
      <view class="desc" v-html="product.description || '本平台精选商品'"></view>
    </view>
    <view class="card"><view class="section">选择规格</view><view class="skus"><view v-for="sku in product.skus || []" :key="sku.id" :class="['sku', selected?.id === sku.id && 'on']" :style="selected?.id === sku.id && { color: theme.primary, borderColor: theme.primary }" @tap="selected = sku"><text class="spec">{{ sku.specName || '规格' }}：{{ sku.specValue || sku.skuCode }}</text><text>¥{{ sku.price }} / 库存{{ sku.stock }}</text></view></view><view class="qty"><text>数量</text><view class="stepper"><text @tap="quantity > 1 && quantity--">-</text><input v-model="quantity" type="number" /><text @tap="quantity++">+</text></view></view></view>
    <view class="actions safe-bottom"><button class="ghost" @tap="addToCart">加入购物车</button><button class="primary" :style="{ background: theme.primary }" @tap="buyNow">立即购买</button></view>
  </view>
</template>

<script setup>
import { onLoad } from '@dcloudio/uni-app'
import { ref } from 'vue'
import { get, post } from '@/common/request'
import { getTheme, requireLogin } from '@/common/store'

const fallback = 'https://dummyimage.com/750x750/edf2f7/94a3b8&text=Product'
const theme = ref(getTheme()); const product = ref({}); const selected = ref(null); const quantity = ref(1)

const addToCart = async () => {
  if (!requireLogin() || !selected.value) return
  await post('/carts/Add', {
    userId: 0, productId: product.value.id, skuId: selected.value.id, merchantId: selected.value.merchantId,
    platformId: selected.value.platformId, productName: product.value.name,
    price: Number(selected.value.price), quantity: Number(quantity.value)
  })
  uni.showToast({ title: '已加入购物车' })
}
const buyNow = () => {
  if (!requireLogin() || !selected.value) return
  uni.setStorageSync('checkout', [{
    skuId: selected.value.id, productId: product.value.id, merchantId: selected.value.merchantId,
    platformId: selected.value.platformId, productName: product.value.name, mainImage: product.value.mainImage,
    price: Number(selected.value.price), quantity: Number(quantity.value), checked: true
  }])
  uni.navigateTo({ url: '/pages/checkout/checkout?type=buy' })
}
onLoad(async options => {
  theme.value = getTheme()
  const data = await get('/products/GetProductDetail', { id: options.id })
  product.value = { ...data.product, skus: (data.skus || []).filter(sku => sku.isActive !== false) }
  selected.value = product.value.skus[0] || null
  uni.setNavigationBarTitle({ title: product.value.name || '商品详情' })
})
</script>

<style scoped>
.cover { width: 750rpx; height: 750rpx; background: #edf2f7; }
.card { background: #fff; border-radius: 20rpx; margin: 20rpx; padding: 26rpx; }
.price { font-size: 42rpx; font-weight: 800; } .name { font-size: 34rpx; font-weight: 700; margin: 14rpx 0; }
.desc { color: #667085; line-height: 1.55; }
.section { font-weight: 700; margin-bottom: 18rpx; } .skus { display: grid; grid-template-columns: repeat(2,1fr); gap: 16rpx; margin-bottom: 30rpx; }
.sku { padding: 18rpx; background: #f8fafc; border: 2rpx solid transparent; border-radius: 14rpx; display: grid; gap: 8rpx; font-size: 24rpx; }
.sku.on { background: #fff5f6; font-weight: 700; } .spec { font-size: 26rpx; }
.qty { display: flex; justify-content: space-between; align-items: center; }
.stepper { display: flex; align-items: center; border: 1px solid #dfe6ef; border-radius: 10rpx; } .stepper text { width: 64rpx; text-align: center; padding: 10rpx 0; } .stepper input { width: 82rpx; text-align: center; }
.actions { position: fixed; left: 0; right: 0; bottom: 0; display: flex; background: #fff; padding-top: 16rpx; }
.actions button { width: 50%; border-radius: 0; color: #fff; } .ghost { background: #ffa53d; } .primary { background: #ff4d6d; }
</style>
