<template>
  <view class="page">
    <view v-if="!platformId" class="empty">请先返回首页选择平台</view>
    <template v-else>
      <view v-if="!items.length" class="empty">暂无可领取的优惠券</view>
      <view v-for="item in items" :key="item.id" class="ticket">
        <view class="left">
          <view class="value"><text class="yen">¥</text>{{ benefitValue(item) }}</view>
          <view class="condition">{{ benefitText(item) }}</view>
        </view>
        <view class="mid">
          <view class="name">{{ item.name }}</view>
          <view class="desc">{{ item.templateDescription || item.templateName }}</view>
          <view class="meta">有效期领取后{{ item.validDays }}天 · 剩余{{ item.remainingStock }}张</view>
        </view>
        <button class="claim" :class="{ off: !item.canClaim }" :disabled="!item.canClaim" @tap="claim(item)">{{ item.canClaim ? '领取' : (item.claimedCount >= item.perUserLimit ? '已领取' : '已抢完') }}</button>
      </view>
      <view class="mine-entry" @tap="goMine">查看我的券包 ›</view>
    </template>
  </view>
</template>

<script setup>
import { ref } from 'vue'
import { onShow } from '@dcloudio/uni-app'
import { get, post } from '@/common/request'
import { getPlatform, isLogin, requireLogin } from '@/common/store'

const platformId = ref(''); const items = ref([])

const benefitValue = item => item.couponType === 2 ? `${(Number(item.discountValue) * 10).toFixed(1)}折` : `¥${Number(item.discountValue).toFixed(2)}`
const benefitText = item => item.couponType === 3 ? '无门槛' : `满${Number(item.threshold).toFixed(2)}可用`

const load = async () => {
  const platform = getPlatform()
  platformId.value = platform?.id || ''
  if (!platformId.value || !isLogin()) return
  const data = await get('/marketing/ClaimableCoupons', { platformId: platformId.value })
  items.value = data.items || []
}
const claim = async item => {
  if (!requireLogin()) return
  await post('/marketing/ClaimCoupon', { couponActivityId: item.id })
  uni.showToast({ title: '领取成功' })
  load()
}
const goMine = () => uni.navigateTo({ url: '/pages/coupon/mine' })
onShow(load)
</script>

<style scoped>
.page { padding: 24rpx; min-height: 100vh; }
.empty { text-align: center; color: #86868b; padding-top: 200rpx; }
.ticket { display: flex; align-items: center; background: #fff; border-radius: 24rpx; padding: 28rpx 24rpx; margin-bottom: 20rpx; box-shadow: 0 2rpx 12rpx rgba(0,0,0,.05); }
.left { width: 170rpx; text-align: center; border-right: 1rpx dashed #e5e5ea; }
.value { color: #ff3b30; font-weight: 800; font-size: 44rpx; }
.value .yen { font-size: 24rpx; margin-right: 2rpx; }
.condition { color: #86868b; font-size: 22rpx; margin-top: 6rpx; }
.mid { flex: 1; padding: 0 20rpx; }
.name { font-weight: 700; font-size: 30rpx; }
.desc { color: #6e6e73; font-size: 24rpx; margin-top: 8rpx; }
.meta { color: #a1a1a6; font-size: 22rpx; margin-top: 8rpx; }
.claim { width: 140rpx; height: 64rpx; line-height: 64rpx; border-radius: 980px; background: #0071e3; color: #fff; font-size: 26rpx; padding: 0; }
.claim.off { background: #e5e5ea; color: #86868b; }
.mine-entry { text-align: center; color: #0071e3; margin-top: 30rpx; font-size: 26rpx; }
</style>
