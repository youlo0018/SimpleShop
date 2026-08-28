<template>
  <view class="box"><view class="title">欢迎回来</view><input v-model="form.userName" placeholder="用户名" /><input v-model="form.password" type="password" placeholder="密码" /><button class="submit" @tap="submit">登录</button><view class="link" @tap="goRegister">没有账号？立即注册</view></view>
</template>

<script setup>
import { reactive } from 'vue'
import { post } from '@/common/request'
import { setSession } from '@/common/store'

const form = reactive({ userName: '', password: '' })
const goRegister = () => uni.navigateTo({ url: '/pages/auth/register' })
const submit = async () => {
  if (!form.userName || !form.password) return uni.showToast({ title: '请输入账号密码', icon: 'none' })
  const data = await post('/users/Login', form)
  setSession(data.token, data.user)
  uni.showToast({ title: '登录成功' })
  setTimeout(() => uni.navigateBack({ fail: () => uni.switchTab({ url: '/pages/profile/profile' }) }), 500)
}
</script>

<style scoped>.box { padding: 110rpx 50rpx; } .title { font-size: 48rpx; font-weight: 800; margin-bottom: 70rpx; }
input { height: 96rpx; background: #fff; border-radius: 16rpx; padding: 0 28rpx; margin-bottom: 26rpx; }
.submit { background: #ff4d6d; color: #fff; border-radius: 16rpx; } .link { text-align: center; color: #667085; margin-top: 34rpx; }</style>
