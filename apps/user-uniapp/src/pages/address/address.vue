<template>
  <view class="page">
    <view v-if="!list.length" class="empty"><text class="emoji">📍</text><text>还没有收货地址</text><text class="empty-sub">添加地址后即可下单收货</text></view>
    <view v-for="item in list" :key="item.id" class="card" @tap="choose(item)">
      <view class="line"><b>{{ item.receiverName }}</b><text>{{ item.receiverPhone }}</text><text v-if="item.isDefault" class="tag">默认</text></view>
      <view class="addr">{{ item.province }}{{ item.city }}{{ item.district }}{{ item.detail }}</view>
    </view>
    <button class="submit safe-bottom" @tap="dialog = true">新增地址</button>
    <view v-if="dialog" class="mask" @tap="dialog = false"><view class="form safe-bottom" @tap.stop>
      <input v-model="form.receiverName" placeholder="收货人" /><input v-model="form.receiverPhone" placeholder="手机号" />
      <picker mode="multiSelector" :range="regionRange" :value="regionValue" @columnchange="onRegionColumnChange" @change="onRegionChange">
        <view class="region-picker"><text :class="{ placeholder: !form.province }">{{ regionText }}</text><text class="caret">›</text></view>
      </picker>
      <input v-model="form.detail" placeholder="详细地址" /><label @tap="form.isDefault = !form.isDefault"><checkbox :checked="form.isDefault" />设为默认</label><button class="submit" @tap="save">保存</button>
    </view></view>
  </view>
</template>

<script setup>
import { onShow } from '@dcloudio/uni-app'
import { computed, reactive, ref } from 'vue'
import { get, post } from '@/common/request'
import { getPlatform, isLogin, requireLogin } from '@/common/store'
import { trimStrings, validateAddress } from '@/common/validators'

const list = ref([]); const dialog = ref(false); const saving = ref(false)
const emptyForm = { id: 0, receiverName: '', receiverPhone: '', province: '广东省', city: '深圳市', district: '南山区', detail: '', isDefault: false }
const form = reactive({ ...emptyForm })

// 省市区数据：平台可在后台自定义，默认内置全国数据；按平台缓存到本地，避免每次进入都请求。
const regions = ref([])
const regionValue = ref([0, 0, 0])
const provinces = computed(() => regions.value.map(item => item.name))
const cities = computed(() => (regions.value[regionValue.value[0]]?.children || []).map(item => item.name))
const districts = computed(() => (regions.value[regionValue.value[0]]?.children?.[regionValue.value[1]]?.children || []).map(item => item.name))
const regionRange = computed(() => [provinces.value, cities.value, districts.value])
const regionText = computed(() => `${form.province || ''} ${form.city || ''} ${form.district || ''}`.trim() || '请选择省/市/区')

const loadRegions = async () => {
  const platformId = getPlatform()?.id || 0
  const cached = uni.getStorageSync('platform_regions')
  if (cached?.platformId === platformId && cached.regions?.length) { regions.value = cached.regions; applyRegionDefaults(); return }
  const data = await get('/platform-configs/Regions', { platformId }).catch(() => null)
  regions.value = data?.regions || []
  if (regions.value.length) uni.setStorageSync('platform_regions', { platformId, regions: regions.value })
  applyRegionDefaults()
}
// 当前表单值与地区数据对齐：找不到时回落到第一项，避免出现下拉里不存在的省市区。
const applyRegionDefaults = () => {
  if (!regions.value.length) return
  if (!provinces.value.includes(form.province)) form.province = provinces.value[0]
  const cityList = (regions.value[provinces.value.indexOf(form.province)]?.children || []).map(item => item.name)
  if (!cityList.includes(form.city)) form.city = cityList[0] || ''
  const districtList = (regions.value[provinces.value.indexOf(form.province)]?.children?.[cityList.indexOf(form.city)]?.children || []).map(item => item.name)
  if (!districtList.includes(form.district)) form.district = districtList[0] || ''
  regionValue.value = [
    Math.max(0, provinces.value.indexOf(form.province)),
    Math.max(0, cityList.indexOf(form.city)),
    Math.max(0, districtList.indexOf(form.district))
  ]
}
const onRegionColumnChange = event => {
  const { column, value } = event.detail
  const next = [...regionValue.value]
  next[column] = value
  if (column === 0) { next[1] = 0; next[2] = 0 }
  if (column === 1) next[2] = 0
  regionValue.value = next
}
const onRegionChange = event => {
  const [p, c, d] = event.detail.value
  form.province = provinces.value[p] || ''
  form.city = cities.value[c] || ''
  form.district = districts.value[d] || ''
}

const load = async () => { if (isLogin()) list.value = await get('/customers/Addresses') || [] }
const save = async () => {
  if (saving.value) return
  trimStrings(form)
  if (!validateAddress(form)) return
  saving.value = true
  try {
    await post('/customers/SaveAddress', { ...form })
    Object.assign(form, emptyForm); applyRegionDefaults(); dialog.value = false; load()
  } finally { saving.value = false }
}
const choose = item => {
  const pages = getCurrentPages(); const current = pages[pages.length - 1]
  if (current.options?.from === 'checkout') { uni.setStorageSync('selectedAddress', item); uni.navigateBack() }
}
onShow(() => { requireLogin(); load(); loadRegions() })
</script>

<style scoped>.page { min-height: 100vh; box-sizing: border-box; padding: 24rpx 0 200rpx; }
.empty { display: grid; justify-items: center; gap: 16rpx; color: #a1a1a6; padding-top: 180rpx; font-size: 28rpx; }
.empty .emoji { font-size: 104rpx; line-height: 1; }
.empty .empty-sub { font-size: 24rpx; color: #c7c7cc; }
.card { background: #fff; border-radius: 26rpx; margin: 0 24rpx 22rpx; padding: 30rpx; box-shadow: 0 6rpx 24rpx rgba(0, 0, 0, .05); }
.line { display: flex; gap: 16rpx; align-items: center; margin-bottom: 10rpx; } .line b { font-size: 30rpx; } .line text { color: #86868b; }
.tag { color: #0071e3 !important; background: rgba(0, 113, 227, .1); font-size: 20rpx !important; font-weight: 600; padding: 4rpx 14rpx; border-radius: 980px; }
.addr { color: #6e6e73; line-height: 1.55; }
.submit { position: fixed; left: 30rpx; right: 30rpx; bottom: calc(50px + 20rpx); background: #0071e3; color: #fff; box-shadow: 0 10rpx 26rpx rgba(0, 113, 227, .3); }
.mask { position: fixed; inset: 0; background: rgba(0, 0, 0, .4); display: flex; align-items: flex-end; }
.form { width: 100%; background: #fff; border-radius: 36rpx 36rpx 0 0; padding: 44rpx 40rpx; box-shadow: 0 -10rpx 40rpx rgba(0, 0, 0, .08); } .form input { height: 96rpx; background: #f5f5f7; border-radius: 20rpx; padding: 0 26rpx; margin-bottom: 20rpx; font-size: 28rpx; }
.region-picker { height: 96rpx; background: #f5f5f7; border-radius: 20rpx; padding: 0 26rpx; margin-bottom: 20rpx; display: flex; align-items: center; justify-content: space-between; font-size: 28rpx; }
.region-picker .placeholder { color: #a1a1a6; }
.region-picker .caret { color: #a1a1a6; font-size: 32rpx; }</style>
