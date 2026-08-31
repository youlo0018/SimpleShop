<template>
  <view class="page">
    <view class="tabs"><view v-for="(text, value) in tabs" :key="value" :class="['tab', statusFilter === Number(value) && 'on']" @tap="filter(value)">{{ text }}</view></view>
    <view v-if="!orders.length" class="empty">暂无订单</view>
    <view v-for="order in orders" :key="order.id" class="card" @tap="show(order)">
      <view class="head"><text>{{ order.orderNo }}</text><text class="status" :style="{ color: statusColor(order) }">{{ status(order) }}</text></view>
      <view class="foot"><text>{{ formatTime(order.createdAt) }}</text><b>¥{{ order.paymentPrice }}</b></view>
    </view>

    <view v-if="detail" class="mask" @tap="detail = null"><view class="sheet safe-bottom" @tap.stop>
      <view class="title">订单详情</view>
      <view class="kv"><text>订单号</text><b>{{ detail.orderNo }}</b></view><view class="kv"><text>状态</text><b>{{ status(detail) }}</b></view>
      <view v-for="item in detail.items || []" :key="item.id" class="goods"><text>{{ item.productName }} ×{{ item.quantity }}</text><b>¥{{ item.subtotalAmount }}</b></view>
      <view class="kv"><text>收货人</text><b>{{ detail.receiverName }} {{ detail.receiverPhone }}</b></view><view class="addr">{{ detail.receiverAddress }}</view>
      <button v-if="[20,40,50].includes(Number(detail.orderStatus))" class="submit" @tap.stop="refund">申请退款</button>
    </view></view>
  </view>
</template>

<script setup>
import { onShow } from '@dcloudio/uni-app'
import { ref } from 'vue'
import { get, post } from '@/common/request'
import { isLogin, requireLogin } from '@/common/store'

const orders = ref([]); const detail = ref(null); const statusFilter = ref(0)
const tabs = { 0: '全部', 10: '待支付', 20: '待发货', 40: '待收货', 50: '已完成', 60: '已退款' }
const status = order => ({ 10: '待支付', 20: '待发货', 40: '待收货', 50: '已完成', 60: '已退款', 90: '已取消', 91: '已关闭' }[Number(order.orderStatus)] || order.orderStatus)
// 状态语义配色：待支付橙、进行中蓝、完成绿、退款红、关闭灰
const statusColor = order => ({
  10: '#ff9500', 20: '#0071e3', 40: '#0071e3', 50: '#34c759', 60: '#ff3b30', 90: '#8e8e93', 91: '#8e8e93'
}[Number(order.orderStatus)] || '#8e8e93')
const formatTime = value => (value || '').slice(0, 19).replace('T', ' ')

const load = async () => {
  if (!isLogin()) { requireLogin(); return }
  const data = await get('/orders/List', { customerId: 0, status: statusFilter.value || undefined, page: 1, pageSize: 50 })
  orders.value = data.items || []
}
const filter = value => { statusFilter.value = Number(value); load() }
const show = async order => {
  const result = await get('/orders/Detail', { id: order.id, customerId: 0 })
  if (result.success === false) return uni.showToast({ title: result.message || '订单不存在', icon: 'none' })
  detail.value = { ...result.order, items: result.items || [] }
}
const refund = async () => {
  await post('/payments/Refund', {
    bizNo: detail.value.orderNo, amount: Number(detail.value.paymentPrice), reason: '用户申请退款',
    items: (detail.value.items || []).map(item => ({ skuId: item.skuId, quantity: Number(item.quantity) }))
  })
  uni.showToast({ title: '退款申请已提交' }); detail.value = null; load()
}
onShow(load)
</script>

<style scoped>
/* iOS 分段控件样式标签页 */
.tabs { display: flex; gap: 8rpx; overflow-x: auto; padding: 22rpx 24rpx; position: sticky; top: 0; background: rgba(245, 245, 247, .92); backdrop-filter: blur(20px); z-index: 5; }
.tab { background: transparent; padding: 12rpx 26rpx; border-radius: 980px; font-size: 24rpx; color: #6e6e73; font-weight: 500; white-space: nowrap; transition: all .15s ease; }
.tab.on { background: #fff; color: #1d1d1f; font-weight: 600; box-shadow: 0 2rpx 8rpx rgba(0, 0, 0, .1); }
.empty { text-align: center; color: #86868b; padding: 140rpx 0; }
.card { background: #fff; border-radius: 24rpx; margin: 20rpx 24rpx; padding: 26rpx 28rpx; box-shadow: 0 2rpx 10rpx rgba(0, 0, 0, .04); }
.head { display: flex; justify-content: space-between; color: #86868b; font-size: 24rpx; font-variant-numeric: tabular-nums; } .status { font-weight: 600; }
.foot { display: flex; justify-content: space-between; align-items: baseline; margin-top: 18rpx; font-variant-numeric: tabular-nums; } .foot b { color: #1d1d1f; font-size: 32rpx; font-weight: 700; }
.mask { position: fixed; inset: 0; background: rgba(0, 0, 0, .4); display: flex; align-items: flex-end; } .sheet { width: 100%; background: #fff; border-radius: 32rpx 32rpx 0 0; padding: 38rpx; line-height: 2; }
.title { font-size: 34rpx; font-weight: 700; letter-spacing: -.01em; text-align: center; margin-bottom: 20rpx; } .kv { display: flex; justify-content: space-between; } .kv text { color: #86868b; } .goods { display: flex; justify-content: space-between; margin: 16rpx 0; }
.addr { color: #86868b; } .submit { margin-top: 22rpx; background: #0071e3; color: #fff; }</style>
