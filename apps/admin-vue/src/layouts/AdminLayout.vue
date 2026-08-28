<template>
  <el-container class="admin-shell">
    <el-aside width="220px">
      <div class="brand">SimpleShop</div>
      <el-menu router :default-active="$route.path" background-color="#001529" text-color="#a6adb4" active-text-color="#fff">
        <el-menu-item index="/dashboard"><span>工作台</span></el-menu-item>
        <el-menu-item v-if="allow('user:read')" index="/users"><span>用户管理</span></el-menu-item>
        <el-menu-item v-if="allow('category:read')" index="/categories"><span>分类管理</span></el-menu-item>
        <el-menu-item v-if="allow('product:read')" index="/products"><span>商品管理</span></el-menu-item>
        <el-menu-item v-if="allow('order:read')" index="/orders"><span>订单管理</span></el-menu-item>
        <el-menu-item v-if="allow('refund:read')" index="/refunds"><span>退款管理</span></el-menu-item>
        <el-menu-item v-if="allow('merchant:read')" index="/merchants"><span>商户管理</span></el-menu-item>
        <el-menu-item v-if="allow('platform:read')" index="/platforms"><span>平台管理</span></el-menu-item>
        <el-menu-item v-if="allow('platform:update')" index="/app-design"><span>小程序装修</span></el-menu-item>
        <el-menu-item v-if="allow('permission:manage')" index="/permissions"><span>角色权限</span></el-menu-item>
      </el-menu>
    </el-aside>
    <el-container>
      <el-header height="64px">
        <div>{{ $route.meta.title }}</div>
        <div class="header-user">
          <span>{{ auth.user?.userName || '管理员' }}</span>
          <el-button link type="danger" @click="exit">退出</el-button>
        </div>
      </el-header>
      <el-main><router-view /></el-main>
    </el-container>
  </el-container>
</template>

<script setup>
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { hasPermission as allow } from '@/utils/permission'

const auth = useAuthStore()
const router = useRouter()
const exit = () => { auth.logout(); router.push('/login') }
</script>

<style scoped>
.admin-shell { min-height: 100vh; }
.el-aside { background: #001529; }
.brand { height: 64px; display: grid; place-items: center; color: white; font-weight: 700; font-size: 20px; }
.el-menu { border-right: 0; }
.el-header { display: flex; justify-content: space-between; align-items: center; background: white; }
.header-user { display: flex; gap: 12px; align-items: center; }
</style>
