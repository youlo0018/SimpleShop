<template>
  <view class="page">
    <view class="card avatar-card" @tap="chooseAvatar">
      <image v-if="form.avatar" class="avatar" :src="assetUrl(form.avatar)" mode="aspectFill" />
      <view v-else class="avatar avatar-placeholder">{{ (user.userName || 'U').slice(0, 1).toUpperCase() }}</view>
      <view class="avatar-hint">点击更换头像</view>
    </view>

    <view class="card">
      <view class="row">
        <text class="label">用户名</text>
        <text class="value muted">{{ user.userName || '--' }}</text>
      </view>
      <view class="row">
        <text class="label">性别</text>
        <picker :range="genderOptions" range-key="label" :value="genderIndex" @change="onGenderChange">
          <text class="value">{{ genderOptions[genderIndex]?.label || '未设置' }} ›</text>
        </picker>
      </view>
      <view class="row">
        <text class="label">生日</text>
        <picker mode="date" :value="form.birth" :end="today" @change="onBirthChange">
          <text class="value">{{ form.birth || '未设置' }} ›</text>
        </picker>
      </view>
      <view class="row">
        <text class="label">邮箱</text>
        <input v-model="form.email" class="input" placeholder="请输入邮箱" />
      </view>
      <view class="row">
        <text class="label">手机号</text>
        <input v-model="form.phone" class="input" type="number" maxlength="11" placeholder="请输入手机号" />
      </view>
    </view>

    <button class="submit" :disabled="saving" :style="{ background: theme.primary }" @tap="submit">保存</button>
  </view>
</template>

<script setup>
import { computed, reactive, ref } from 'vue'
import { onShow } from '@dcloudio/uni-app'
import { get, post } from '@/common/request'
import { getTheme, getUser, requireLogin, setSession } from '@/common/store'

const BASE_URL = 'http://127.0.0.1:5008/gateway'
const theme = ref(getTheme())
const user = ref({})
const saving = ref(false)
const today = new Date().toISOString().slice(0, 10)
const genderOptions = [
  { label: '未设置', value: 0 },
  { label: '男', value: 1 },
  { label: '女', value: 2 }
]
const form = reactive({ avatar: '', gender: 0, birth: '', email: '', phone: '' })
const genderIndex = computed(() => Math.max(0, genderOptions.findIndex(item => item.value === Number(form.gender))))

// 头像 URL 可能是相对网关路径（/gateway/products/File/x），补上服务端 origin 才能在小程序/H5 显示。
const assetUrl = path => !path ? '' : (String(path).startsWith('http') ? path : BASE_URL.replace('/gateway', '') + path)

const load = async () => {
  if (!requireLogin()) return
  const profile = await get('/customers/Profile').catch(() => null)
  if (!profile) return
  user.value = profile
  form.avatar = profile.avatar || ''
  form.gender = Number(profile.gender || 0)
  form.birth = profile.birth ? String(profile.birth).slice(0, 10) : ''
  form.email = profile.email || ''
  form.phone = profile.phone || ''
}

const chooseAvatar = () => {
  uni.chooseImage({
    count: 1,
    sizeType: ['compressed'],
    success: ({ tempFilePaths }) => {
      const filePath = tempFilePaths?.[0]
      if (!filePath) return
      uni.showLoading({ title: '上传中' })
      uni.uploadFile({
        url: `${BASE_URL}/files/Upload`,
        filePath,
        name: 'file',
        header: { Authorization: `Bearer ${uni.getStorageSync('token') || ''}` },
        success: ({ data }) => {
          const payload = JSON.parse(data || '{}')
          if (Number(payload.code) !== 200 || !payload.data?.url) {
            return uni.showToast({ title: payload.message || '上传失败', icon: 'none' })
          }
          form.avatar = payload.data.url
        },
        fail: () => uni.showToast({ title: '上传失败', icon: 'none' }),
        complete: () => uni.hideLoading()
      })
    }
  })
}

const onGenderChange = event => { form.gender = genderOptions[Number(event.detail.value)]?.value ?? 0 }
const onBirthChange = event => { form.birth = event.detail.value }

const submit = async () => {
  if (saving.value) return
  saving.value = true
  try {
    const updated = await post('/customers/SaveProfile', {
      avatar: form.avatar, gender: Number(form.gender),
      birth: form.birth ? `${form.birth}T00:00:00` : null,
      email: form.email, phone: form.phone
    })
    setSession(uni.getStorageSync('token'), { ...user.value, ...updated })
    uni.showToast({ title: '已保存' })
    setTimeout(() => uni.navigateBack(), 500)
  } finally { saving.value = false }
}

onShow(() => { theme.value = getTheme(); load() })
</script>

<style scoped>
.page { min-height: 100vh; padding: 24rpx; box-sizing: border-box; background: #f5f5f7; }
.card { background: #fff; border-radius: 28rpx; padding: 30rpx 32rpx; margin-bottom: 22rpx; box-shadow: 0 6rpx 20rpx rgba(0, 0, 0, .04); }
.avatar-card { display: grid; justify-items: center; gap: 16rpx; padding: 44rpx 0 34rpx; }
.avatar { width: 160rpx; height: 160rpx; border-radius: 50%; background: #f2f2f4; }
.avatar-placeholder { display: grid; place-items: center; font-size: 60rpx; font-weight: 800; color: #fff; background: linear-gradient(135deg, #0a84ff, #5e5ce6); }
.avatar-hint { color: #86868b; font-size: 24rpx; }
.row { display: flex; align-items: center; justify-content: space-between; min-height: 96rpx; border-bottom: 1rpx solid rgba(60, 60, 67, .06); }
.row:last-child { border-bottom: 0; }
.label { color: #1d1d1f; font-size: 28rpx; }
.value { color: #6e6e73; font-size: 27rpx; }
.value.muted { color: #a1a1a6; }
.input { flex: 1; text-align: right; font-size: 27rpx; color: #1d1d1f; }
.submit { margin-top: 34rpx; height: 96rpx; display: flex; align-items: center; justify-content: center; color: #fff; font-size: 30rpx; border-radius: 980px; box-shadow: 0 8rpx 24rpx rgba(0, 113, 227, .24); }
</style>
