<template>
  <view class="box"><view class="mark">S</view><view class="title">创建账号</view><view class="subtitle">注册后即可下单购物</view><input v-model="form.userName" placeholder="用户名" /><input v-model="form.password" type="password" placeholder="密码" /><input v-model="form.email" placeholder="邮箱" /><input v-model="form.phone" placeholder="手机号" /><button class="submit" @tap="submit">注册并登录</button></view>
</template>

<script setup>
import { reactive } from 'vue'
import { post } from '@/common/request'
import { setSession } from '@/common/store'

const form = reactive({ userName: '', password: '', email: '', phone: '', role: 'customer' })
const submit = async () => {
  if (!form.userName || !form.password || !form.phone) return uni.showToast({ title: '请完善账号信息', icon: 'none' })
  const data = await post('/users/Register', form)
  setSession(data.token, data.user)
  uni.showToast({ title: '注册成功' })
  setTimeout(() => uni.navigateBack({ fail: () => uni.switchTab({ url: '/pages/profile/profile' }) }), 500)
}
</script>

<style scoped>.box { padding: 120rpx 50rpx; }
.mark { width: 108rpx; height: 108rpx; display: grid; place-items: center; border-radius: 28rpx; background: linear-gradient(135deg, #0a84ff, #5e5ce6); color: #fff; font-size: 52rpx; font-weight: 700; box-shadow: 0 12rpx 32rpx rgba(10, 132, 255, .32); }
.title { font-size: 52rpx; font-weight: 800; letter-spacing: -.02em; margin-top: 44rpx; color: #1d1d1f; }
.subtitle { color: #86868b; font-size: 27rpx; margin: 10rpx 0 56rpx; }
input { height: 100rpx; background: #fff; border-radius: 22rpx; padding: 0 30rpx; margin-bottom: 26rpx; font-size: 28rpx; }
.submit { background: #0071e3; color: #fff; height: 100rpx; display: flex; align-items: center; justify-content: center; font-size: 30rpx; box-shadow: 0 8rpx 24rpx rgba(0, 113, 227, .28); }</style>
