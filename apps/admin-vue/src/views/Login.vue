<template>
  <div class="login-page">
    <el-form class="login-box" @submit.prevent="submit">
      <h2>SimpleShop 运营后台</h2>
      <el-form-item><el-input v-model="form.userName" placeholder="用户名" size="large" /></el-form-item>
      <el-form-item><el-input v-model="form.password" type="password" placeholder="密码" size="large" show-password /></el-form-item>
      <el-button type="primary" size="large" :loading="loading" native-type="submit">登 录</el-button>
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
.login-page { min-height: 100vh; display: grid; place-items: center; background: linear-gradient(135deg,#1677ff,#36cfc9); }
.login-box { width: 380px; padding: 40px; border-radius: 12px; background: white; }
.login-box h2 { text-align: center; margin-top: 0; }
.login-box .el-button { width: 100%; }
</style>
