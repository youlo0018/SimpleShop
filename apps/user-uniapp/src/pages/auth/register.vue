<template>
  <view class="box"><view class="title">创建账号</view><input v-model="form.userName" placeholder="用户名" /><input v-model="form.password" type="password" placeholder="密码" /><input v-model="form.email" placeholder="邮箱" /><input v-model="form.phone" placeholder="手机号" /><button class="submit" @tap="submit">注册并登录</button></view>
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

<style scoped>.box { padding: 110rpx 50rpx; } .title { font-size: 48rpx; font-weight: 800; margin-bottom: 70rpx; }
input { height: 96rpx; background: #fff; border-radius: 16rpx; padding: 0 28rpx; margin-bottom: 26rpx; }
.submit { background: #ff4d6d; color: #fff; border-radius: 16rpx; }</style>
