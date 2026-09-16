<template>
  <view class="page">
    <image class="cover" :src="selected?.image || product.mainImage || fallback" mode="aspectFill" />
    <view class="card">
      <view class="price" :style="{ color: theme.primary }"><text class="yen">¥</text>{{ selected?.price || '--' }}<text class="stock">库存 {{ selected?.stock || 0 }}</text></view>
      <view class="name">{{ product.name }}</view>
      <view class="desc" v-html="product.description || '本平台精选商品'"></view>
    </view>
    <view class="card"><view class="section">选择规格</view><view class="skus"><view v-for="sku in product.skus || []" :key="sku.id" :class="['sku', selected?.id === sku.id && 'on']" :style="selected?.id === sku.id && { color: theme.primary, borderColor: theme.primary }" @tap="selected = sku"><text class="spec">{{ sku.specName || '规格' }}：{{ sku.specValue || sku.skuCode }}</text><text>¥{{ sku.price }} / 库存{{ sku.stock }}</text></view></view><view class="qty"><text>数量</text><view class="stepper"><text @tap="stepDown">-</text><input v-model="quantity" type="number" @blur="normalizeQuantity" /><text @tap="stepUp">+</text></view></view></view>
    <view class="actions safe-bottom"><button class="ghost" @tap="addToCart">加入购物车</button><button class="primary" :style="{ background: theme.primary }" @tap="buyNow">立即购买</button></view>
  </view>
</template>

<script setup>
import { onLoad } from '@dcloudio/uni-app'
import { computed, ref } from 'vue'
import { get, post } from '@/common/request'
import { getTheme, requireLogin } from '@/common/store'
import { toast, validateQuantity } from '@/common/validators'

const fallback = 'https://dummyimage.com/750x750/edf2f7/94a3b8&text=Product'
const theme = ref(getTheme()); const product = ref({}); const selected = ref(null); const quantity = ref(1)

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

const addToCart = async () => {
  if (!requireLogin()) return
  if (!checkQuantity()) return
  await post('/carts/Add', {
    userId: 0, productId: product.value.id, skuId: selected.value.id, merchantId: selected.value.merchantId,
    platformId: selected.value.platformId, productName: product.value.name,
    price: Number(selected.value.price), quantity: Number(quantity.value)
  })
  uni.showToast({ title: '已加入购物车' })
}
const buyNow = () => {
  if (!requireLogin()) return
  if (!checkQuantity()) return
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
.cover { width: 750rpx; height: 750rpx; background: #f5f5f7; }
.card { background: #fff; border-radius: 28rpx; margin: 20rpx 24rpx; padding: 30rpx; box-shadow: 0 2rpx 12rpx rgba(0, 0, 0, .04); }
.price { font-size: 52rpx; font-weight: 800; font-variant-numeric: tabular-nums; line-height: 1.1; }
.price .yen { font-size: 30rpx; font-weight: 700; margin-right: 4rpx; }
.price .stock { font-size: 22rpx; font-weight: 500; opacity: .55; margin-left: 14rpx; }
.name { font-size: 36rpx; font-weight: 700; letter-spacing: -.01em; margin: 16rpx 0; }
.desc { color: #6e6e73; line-height: 1.65; font-size: 27rpx; }
.section { font-weight: 700; letter-spacing: -.01em; margin-bottom: 18rpx; } .skus { display: grid; grid-template-columns: repeat(2,1fr); gap: 16rpx; margin-bottom: 30rpx; }
.sku { padding: 20rpx; background: #f5f5f7; border: 2rpx solid transparent; border-radius: 18rpx; display: grid; gap: 8rpx; font-size: 24rpx; }
.sku.on { background: #fff; font-weight: 700; box-shadow: 0 2rpx 10rpx rgba(0, 0, 0, .06); } .spec { font-size: 26rpx; }
.qty { display: flex; justify-content: space-between; align-items: center; }
.stepper { display: flex; align-items: center; background: #f5f5f7; border-radius: 980px; } .stepper text { width: 64rpx; text-align: center; padding: 10rpx 0; font-weight: 600; } .stepper input { width: 82rpx; text-align: center; }
/* 底部操作条：毛玻璃 + 胶囊双按钮 */
.actions { position: fixed; left: 0; right: 0; bottom: 0; display: flex; gap: 18rpx; background: rgba(255, 255, 255, .92); backdrop-filter: blur(20px); border-top: 1rpx solid rgba(0, 0, 0, .06); padding: 16rpx 24rpx; }
.actions button { flex: 1; color: #fff; height: 88rpx; display: flex; align-items: center; justify-content: center; font-size: 29rpx; }
.ghost { background: #f5f5f7; } .actions .ghost { color: #1d1d1f; } .primary { background: #0071e3; box-shadow: 0 6rpx 18rpx rgba(0, 113, 227, .3); }
</style>
