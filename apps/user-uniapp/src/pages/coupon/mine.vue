<template>
  <view class="page">
    <view class="tabs">
      <view v-for="tab in tabs" :key="tab.value" :class="['tab', status === tab.value && 'on']" @tap="switchTab(tab.value)">{{ tab.label }}</view>
    </view>
    <view v-if="!items.length" class="empty">暂无优惠券</view>
    <view v-for="item in items" :key="item.id" class="ticket" :class="{ dim: Number(item.status) !== 1 }">
      <view class="left">
        <view class="value"><text v-if="!isDiscount(item)" class="yen">¥</text>{{ benefitValue(item) }}</view>
        <view class="condition">{{ benefitText(item) }}</view>
      </view>
      <view class="mid">
        <view class="name">{{ item.activityName || item.templateName }}</view>
        <view class="meta">有效期至 {{ (item.expireAt || '').slice(0, 10) }}</view>
        <view class="meta" v-if="item.usedOrderNo">用于订单 {{ item.usedOrderNo }}</view>
        <view class="source">{{ Number(item.source) === 2 ? '满赠获得' : '领券中心' }}</view>
      </view>
      <view class="status">{{ statusText[item.status] }}</view>
    </view>
    <view class="center-entry" @tap="goCenter">去领券中心 ›</view>
  </view>
</template>

<script setup>
import { ref } from 'vue'
import { onShow } from '@dcloudio/uni-app'
import { get } from '@/common/request'
import { requireLogin } from '@/common/store'

const tabs = [{ value: 0, label: '全部' }, { value: 1, label: '未使用' }, { value: 2, label: '已使用' }, { value: 3, label: '已过期' }]
const status = ref(0); const items = ref([])
const statusText = { 1: '未使用', 2: '已使用', 3: '已过期' }
const isDiscount = item => Number(item.couponType) === 2
const benefitValue = item => isDiscount(item) ? `${(Number(item.discountValue) * 10).toFixed(1)}折` : Number(item.discountValue).toFixed(2)
const benefitText = item => Number(item.couponType) === 3 ? '无门槛' : `满${Number(item.threshold).toFixed(2)}可用`

const load = async () => {
  if (!requireLogin()) return
  const data = await get('/marketing/MyCoupons', status.value ? { status: status.value } : {})
  items.value = data.items || []
}
const switchTab = value => { status.value = value; load() }
const goCenter = () => uni.navigateTo({ url: '/pages/coupon/center' })
onShow(load)
</script>

<style scoped>
.page { padding: 20rpx 24rpx 60rpx; min-height: 100vh; }
.tabs { display: flex; gap: 8rpx; padding: 12rpx 0 24rpx; }
.tab { padding: 12rpx 26rpx; border-radius: 980px; font-size: 24rpx; color: #6e6e73; background: transparent; }
.tab.on { background: #fff; color: #1d1d1f; font-weight: 600; box-shadow: 0 2rpx 8rpx rgba(0,0,0,.08); }
.empty { text-align: center; color: #86868b; padding-top: 200rpx; }
.ticket { display: flex; align-items: center; background: #fff; border-radius: 24rpx; padding: 28rpx 24rpx; margin-bottom: 20rpx; box-shadow: 0 2rpx 12rpx rgba(0,0,0,.05); }
.ticket.dim { opacity: .55; }
.left { width: 170rpx; text-align: center; border-right: 1rpx dashed #e5e5ea; }
.value { color: #ff3b30; font-weight: 800; font-size: 44rpx; }
.value .yen { font-size: 24rpx; margin-right: 2rpx; }
.condition { color: #86868b; font-size: 22rpx; margin-top: 8rpx; }
.mid { flex: 1; padding: 0 20rpx; }
.name { font-weight: 700; font-size: 30rpx; }
.meta { color: #86868b; font-size: 23rpx; margin-top: 8rpx; }
.source { color: #a1a1a6; font-size: 21rpx; margin-top: 8rpx; }
.status { color: #0071e3; font-size: 24rpx; font-weight: 600; }
.center-entry { text-align: center; color: #0071e3; margin-top: 30rpx; font-size: 26rpx; }
</style>
