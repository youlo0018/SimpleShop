<template>
  <div class="login-page">
    <el-form class="login-card" @submit.prevent="submit">
      <div class="login-mark">S</div>
      <h1>SimpleShop 运营后台</h1>
      <p class="login-sub">登录以管理平台、商户与订单</p>
      <el-form-item><el-input v-model="form.userName" placeholder="用户名" size="large" autocomplete="username" /></el-form-item>
      <el-form-item><el-input v-model="form.password" type="password" placeholder="密码" size="large" show-password autocomplete="current-password" /></el-form-item>
      <el-button type="primary" size="large" :loading="loading" native-type="submit">登 录</el-button>
      <p class="login-foot">© 2026 SimpleShop</p>
    </el-form>
  </div>
</template>

<script setup>
import { ElMessage } from 'element-plus'
import { reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import request from '@/api/request'
import { useAuthStore } from '@/stores/auth'

const form = reactive({ userName: '', password: '' })
const loading = ref(false)
const router = useRouter()
const auth = useAuthStore()

async function submit() {
  if (!form.userName || !form.password) return ElMessage.warning('请输入账号密码')
  loading.value = true
  try {
    const data = await request.post('/users/Login', form)
    auth.setSession(data.token, data.user)
    router.push('/dashboard')
  } finally { loading.value = false }
}
</script>

<style scoped>
.login-page {
  min-height: 100vh;
  display: grid;
  place-items: center;
  background: var(--apple-bg);
  position: relative;
  overflow: hidden;
}
/* 柔和的环境光斑，Apple 发布会页风格 */
.login-page::before, .login-page::after {
  content: '';
  position: absolute;
  width: 620px; height: 620px;
  border-radius: 50%;
  filter: blur(110px);
  pointer-events: none;
}
.login-page::before { background: rgba(10, 132, 255, .16); top: -180px; left: -120px; }
.login-page::after { background: rgba(94, 92, 230, .13); bottom: -200px; right: -140px; }

.login-card {
  position: relative;
  width: 400px;
  padding: 44px 40px 32px;
  border-radius: 22px;
  background: rgba(255, 255, 255, .86);
  backdrop-filter: blur(24px) saturate(180%);
  border: 1px solid rgba(255, 255, 255, .65);
  box-shadow: 0 24px 80px rgba(0, 0, 0, .13);
  display: flex;
  flex-direction: column;
}
.login-mark {
  width: 56px; height: 56px;
  margin: 0 auto;
  display: grid; place-items: center;
  border-radius: 15px;
  background: linear-gradient(135deg, #0a84ff, #5e5ce6);
  color: #fff; font-size: 27px; font-weight: 700;
  box-shadow: 0 8px 24px rgba(10, 132, 255, .38);
}
h1 {
  margin: 18px 0 4px;
  text-align: center;
  font-size: 21px;
  font-weight: 700;
  letter-spacing: -.02em;
  color: var(--apple-text);
}
.login-sub {
  margin: 0 0 26px;
  text-align: center;
  font-size: 13px;
  color: var(--apple-text-3);
}
.login-card .el-button { width: 100%; margin-top: 4px; }
.login-foot { margin: 26px 0 0; text-align: center; font-size: 11.5px; color: #a1a1a6; }
</style>
