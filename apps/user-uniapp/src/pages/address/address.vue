<template>
  <view class="page">
    <view v-for="item in list" :key="item.id" class="card" @tap="choose(item)">
      <view class="line"><b>{{ item.receiverName }}</b><text>{{ item.receiverPhone }}</text><text v-if="item.isDefault" class="tag">默认</text></view>
      <view class="addr">{{ item.province }}{{ item.city }}{{ item.district }}{{ item.detail }}</view>
    </view>
    <button class="submit safe-bottom" @tap="dialog = true">新增地址</button>
    <view v-if="dialog" class="mask" @tap="dialog = false"><view class="form safe-bottom" @tap.stop>
      <input v-model="form.receiverName" placeholder="收货人" /><input v-model="form.receiverPhone" placeholder="手机号" />
      <view class="row"><input v-model="form.province" placeholder="省" /><input v-model="form.city" placeholder="市" /><input v-model="form.district" placeholder="区" /></view>
      <input v-model="form.detail" placeholder="详细地址" /><label @tap="form.isDefault = !form.isDefault"><checkbox :checked="form.isDefault" />设为默认</label><button class="submit" @tap="save">保存</button>
    </view></view>
  </view>
</template>

<script setup>
import { onShow } from '@dcloudio/uni-app'
import { reactive, ref } from 'vue'
import { get, post } from '@/common/request'
import { isLogin, requireLogin } from '@/common/store'

const list = ref([]); const dialog = ref(false)
const emptyForm = { id: 0, receiverName: '', receiverPhone: '', province: '广东省', city: '深圳市', district: '南山区', detail: '', isDefault: false }
const form = reactive({ ...emptyForm })

const load = async () => { if (isLogin()) list.value = await get('/users/Addresses') || [] }
const save = async () => {
  if (!form.receiverName || !form.receiverPhone || !form.detail) return uni.showToast({ title: '请完善地址', icon: 'none' })
  await post('/users/SaveAddress', form)
  Object.assign(form, emptyForm); dialog.value = false; load()
}
const choose = item => {
  const pages = getCurrentPages(); const current = pages[pages.length - 1]
  if (current.options?.from === 'checkout') { uni.setStorageSync('selectedAddress', item); uni.navigateBack() }
}
onShow(() => { requireLogin(); load() })
</script>

<style scoped>.page { padding: 20rpx 20rpx 140rpx; } .card { background: #fff; border-radius: 18rpx; margin-bottom: 20rpx; padding: 26rpx; }
.line { display: flex; gap: 16rpx; align-items: center; margin-bottom: 10rpx; } .tag { color: #ff4d6d; font-size: 22rpx; } .addr { color: #667085; line-height: 1.5; }
.submit { position: fixed; left: 30rpx; right: 30rpx; bottom: calc(50px + 20rpx); background: #ff4d6d; color: #fff; }
.mask { position: fixed; inset: 0; background: rgba(15,23,42,.55); display: flex; align-items: flex-end; }
.form { width: 100%; background: #fff; border-radius: 28rpx 28rpx 0 0; padding: 38rpx; } .form input { height: 88rpx; border-bottom: 1px solid #edf2f7; margin-bottom: 18rpx; }
.row { display: flex; gap: 12rpx; } .row input { min-width: 0; }</style>
