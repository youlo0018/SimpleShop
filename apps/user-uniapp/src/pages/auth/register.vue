<template>
  <view class="box"><view class="blob blob-a" /><view class="blob blob-b" /><view class="mark">S</view><view class="title">创建账号</view><view class="subtitle">注册后即可下单购物</view><input v-model="form.userName" placeholder="用户名（3-64位）" /><input v-model="form.password" type="password" placeholder="密码（至少8位含字母和数字）" /><input v-model="form.confirmPassword" type="password" placeholder="确认密码" /><input v-model="form.email" placeholder="邮箱" /><input v-model="form.phone" placeholder="手机号" />
    <view class="agreement" @tap="agreed = !agreed">
      <view :class="['checkbox', agreed && 'on']">{{ agreed ? '✓' : '' }}</view>
      <text class="agreement-text">我已阅读并同意</text>
      <text class="agreement-link" @tap.stop="showAgreement">《用户协议》</text>
    </view>
    <button class="submit" :disabled="submitting" @tap="submit">注册并登录</button></view>
</template>

<script setup>
import { reactive, ref } from 'vue'
import { post } from '@/common/request'
import { setSession } from '@/common/store'
import { trimStrings, validateRegister } from '@/common/validators'
import { ensurePlatform } from '@/common/platform-config'
import { RegisterSource } from '@/common/register-source'

const form = reactive({ userName: '', password: '', confirmPassword: '', email: '', phone: '' })
const agreed = ref(false)
const submitting = ref(false)
const showAgreement = () => uni.showModal({
  title: '用户协议',
  content: '注册即表示您同意本平台的服务条款与隐私政策：我们仅收集为您提供购物服务所必需的信息，并依法保护您的个人信息安全。',
  showCancel: false,
  confirmText: '我知道了'
})
const submit = async () => {
  if (submitting.value) return
  trimStrings(form)
  if (!validateRegister(form)) return
  submitting.value = true
  try {
    if (!agreed.value) return uni.showToast({ title: '请先阅读并同意用户协议', icon: 'none' })
    const platform = await ensurePlatform()
    if (!platform?.id) return uni.showToast({ title: '平台配置未就绪', icon: 'none' })
    const payload = {
      userName: form.userName, password: form.password, email: form.email, phone: form.phone,
      platformId: platform.id, agreedAgreement: true, registerSource: RegisterSource.MiniApp
    }
    const data = await post('/customers/Register', payload)
    setSession(data.token, data.user)
    uni.showToast({ title: '注册成功' })
    setTimeout(() => uni.navigateBack({ fail: () => uni.switchTab({ url: '/pages/profile/profile' }) }), 500)
  } finally { submitting.value = false }
}
</script>

<style scoped>.box { position: relative; overflow: hidden; min-height: 100vh; padding: 130rpx 50rpx 80rpx; box-sizing: border-box; }
.blob { position: absolute; border-radius: 50%; opacity: .5; }
.blob-a { width: 520rpx; height: 520rpx; top: -220rpx; left: -180rpx; background: radial-gradient(circle, rgba(10, 132, 255, .28), rgba(10, 132, 255, 0) 70%); }
.blob-b { width: 460rpx; height: 460rpx; bottom: -180rpx; right: -160rpx; background: radial-gradient(circle, rgba(94, 92, 230, .22), rgba(94, 92, 230, 0) 70%); }
.mark, .title, .subtitle, input, .submit { position: relative; }
.mark { width: 108rpx; height: 108rpx; display: grid; place-items: center; border-radius: 28rpx; background: linear-gradient(135deg, #0a84ff, #5e5ce6); color: #fff; font-size: 52rpx; font-weight: 700; box-shadow: 0 12rpx 32rpx rgba(10, 132, 255, .32); }
.title { font-size: 52rpx; font-weight: 800; letter-spacing: -.02em; margin-top: 44rpx; color: #1d1d1f; }
.subtitle { color: #86868b; font-size: 27rpx; margin: 10rpx 0 56rpx; }
input { height: 100rpx; background: #fff; border-radius: 22rpx; padding: 0 32rpx; margin-bottom: 26rpx; font-size: 28rpx; box-shadow: 0 4rpx 16rpx rgba(0, 0, 0, .04); }
.agreement { display: flex; align-items: center; gap: 12rpx; margin: 6rpx 0 30rpx; padding: 0 8rpx; font-size: 25rpx; color: #6e6e73; }
.checkbox { width: 34rpx; height: 34rpx; border-radius: 10rpx; border: 2rpx solid #c7c7cc; display: grid; place-items: center; color: #fff; font-size: 22rpx; }
.checkbox.on { background: #0071e3; border-color: #0071e3; }
.agreement-link { color: #0071e3; }
.submit { background: #0071e3; color: #fff; height: 100rpx; display: flex; align-items: center; justify-content: center; font-size: 30rpx; box-shadow: 0 8rpx 24rpx rgba(0, 113, 227, .28); }</style>
