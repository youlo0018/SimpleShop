<template>
  <view class="box"><view class="blob blob-a" /><view class="blob blob-b" /><view class="mark">S</view><view class="title">欢迎回来</view><view class="subtitle">登录 SimpleShop 商城</view><input v-model="form.userName" placeholder="用户名" /><input v-model="form.password" type="password" placeholder="密码" /><button class="submit" @tap="submit">登录</button><view class="link" @tap="goRegister">没有账号？立即注册</view></view>
</template>

<script setup>
import { reactive, ref } from 'vue'
import { post } from '@/common/request'
import { setSession } from '@/common/store'
import { ensurePlatform } from '@/common/platform-config'
import { trimStrings, validateLogin } from '@/common/validators'

const form = reactive({ userName: '', password: '' })
const submitting = ref(false)
const goRegister = () => uni.navigateTo({ url: '/pages/auth/register' })
const submit = async () => {
  if (submitting.value) return
  trimStrings(form)
  if (!validateLogin(form)) return
  submitting.value = true
  try {
    const platform = await ensurePlatform()
    if (!platform?.id) return uni.showToast({ title: '平台配置未就绪', icon: 'none' })
    const data = await post('/customers/Login', { ...form, platformId: platform.id })
    setSession(data.token, data.user)
    uni.showToast({ title: '登录成功' })
    setTimeout(() => uni.navigateBack({ fail: () => uni.switchTab({ url: '/pages/profile/profile' }) }), 500)
  } finally { submitting.value = false }
}
</script>

<style scoped>.box { position: relative; overflow: hidden; min-height: 100vh; padding: 150rpx 50rpx 80rpx; box-sizing: border-box; }
.blob { position: absolute; border-radius: 50%; opacity: .5; }
.blob-a { width: 520rpx; height: 520rpx; top: -220rpx; left: -180rpx; background: radial-gradient(circle, rgba(10, 132, 255, .28), rgba(10, 132, 255, 0) 70%); }
.blob-b { width: 460rpx; height: 460rpx; bottom: -180rpx; right: -160rpx; background: radial-gradient(circle, rgba(94, 92, 230, .22), rgba(94, 92, 230, 0) 70%); }
.mark, .title, .subtitle, input, .submit, .link { position: relative; }
.mark { width: 108rpx; height: 108rpx; display: grid; place-items: center; border-radius: 28rpx; background: linear-gradient(135deg, #0a84ff, #5e5ce6); color: #fff; font-size: 52rpx; font-weight: 700; box-shadow: 0 12rpx 32rpx rgba(10, 132, 255, .32); }
.title { font-size: 52rpx; font-weight: 800; letter-spacing: -.02em; margin-top: 44rpx; color: #1d1d1f; }
.subtitle { color: #86868b; font-size: 27rpx; margin: 10rpx 0 56rpx; }
input { height: 100rpx; background: #fff; border-radius: 22rpx; padding: 0 32rpx; margin-bottom: 26rpx; font-size: 28rpx; box-shadow: 0 4rpx 16rpx rgba(0, 0, 0, .04); }
.submit { background: #0071e3; color: #fff; height: 100rpx; display: flex; align-items: center; justify-content: center; font-size: 30rpx; box-shadow: 0 8rpx 24rpx rgba(0, 113, 227, .28); } .link { text-align: center; color: #0071e3; margin-top: 34rpx; font-size: 26rpx; }</style>
