<template>
  <el-container class="admin-shell">
    <el-aside width="232px" class="sidebar">
      <div class="brand">
        <div class="brand-mark">S</div>
        <div class="brand-text">
          <span class="brand-name">SimpleShop</span>
          <span class="brand-sub">运营后台</span>
        </div>
      </div>

      <el-scrollbar class="menu-scroll">
        <el-menu router :default-active="$route.path">
          <p class="menu-group">概览</p>
          <el-menu-item index="/dashboard"><el-icon><Odometer /></el-icon><span>工作台</span></el-menu-item>

          <template v-if="allow('user:read') || allow('category:read') || allow('product:read')">
            <p class="menu-group">用户与商品</p>
            <el-menu-item v-if="allow('user:read')" index="/users"><el-icon><User /></el-icon><span>用户管理</span></el-menu-item>
            <el-menu-item v-if="allow('category:read')" index="/categories"><el-icon><Collection /></el-icon><span>分类管理</span></el-menu-item>
            <el-menu-item v-if="allow('product:read')" index="/products"><el-icon><Goods /></el-icon><span>商品管理</span></el-menu-item>
          </template>

          <template v-if="allow('order:read') || allow('refund:read')">
            <p class="menu-group">交易</p>
            <el-menu-item v-if="allow('order:read')" index="/orders"><el-icon><List /></el-icon><span>订单管理</span></el-menu-item>
            <el-menu-item v-if="allow('refund:read')" index="/refunds"><el-icon><RefreshLeft /></el-icon><span>退款管理</span></el-menu-item>
          </template>

          <template v-if="allow('merchant:read') || allow('platform:read') || allow('platform:update')">
            <p class="menu-group">运营</p>
            <el-menu-item v-if="allow('merchant:read')" index="/merchants"><el-icon><Shop /></el-icon><span>商户管理</span></el-menu-item>
            <el-menu-item v-if="allow('platform:read') && !platformScoped" index="/platforms"><el-icon><OfficeBuilding /></el-icon><span>平台管理</span></el-menu-item>
            <el-menu-item v-if="allow('platform:update')" index="/app-design"><el-icon><Cellphone /></el-icon><span>小程序装修</span></el-menu-item>
            <el-menu-item v-if="allow('platform:update')" index="/regions"><el-icon><Location /></el-icon><span>地区地址</span></el-menu-item>
          </template>

          <template v-if="allow('marketing:read')">
            <p class="menu-group">营销</p>
            <el-menu-item index="/marketing"><el-icon><PriceTag /></el-icon><span>营销管理</span></el-menu-item>
          </template>

          <template v-if="allow('permission:manage')">
            <p class="menu-group">系统</p>
            <el-menu-item index="/permissions"><el-icon><Key /></el-icon><span>角色权限</span></el-menu-item>
          </template>
        </el-menu>
      </el-scrollbar>
    </el-aside>

    <el-container class="main-column">
      <el-header height="60px" class="topbar">
        <h1 class="page-title">{{ $route.meta.title }}</h1>
        <div class="header-user">
          <div class="user-chip">
            <span class="avatar">{{ avatarText }}</span>
            <span class="user-name">{{ auth.user?.userName || '管理员' }}</span>
          </div>
          <el-tooltip content="退出登录" placement="bottom">
            <button class="logout-btn" type="button" @click="exit">
              <el-icon><SwitchButton /></el-icon>
            </button>
          </el-tooltip>
        </div>
      </el-header>
      <el-main class="content"><router-view /></el-main>
    </el-container>
  </el-container>
</template>

<script setup>
import { computed } from 'vue'
import { useRouter } from 'vue-router'
import {
  Cellphone, Collection, Goods, Key, List, Location, Odometer, OfficeBuilding,
  PriceTag, RefreshLeft, Shop, SwitchButton, User
} from '@element-plus/icons-vue'
import { useAuthStore } from '@/stores/auth'
import { hasPermission as allow } from '@/utils/permission'
import { isPlatformScoped } from '@/utils/tenant'

const auth = useAuthStore()
const platformScoped = isPlatformScoped()
const router = useRouter()
const avatarText = computed(() => (auth.user?.userName || 'A').slice(0, 1).toUpperCase())
const exit = () => { auth.logout(); router.push('/login') }
</script>

<style scoped>
.admin-shell { height: 100vh; }

/* ---------- 侧边栏：macOS 半透明浅色 ---------- */
.sidebar {
  display: flex;
  flex-direction: column;
  background: rgba(246, 246, 248, .82);
  backdrop-filter: blur(20px) saturate(180%);
  border-right: 1px solid rgba(0, 0, 0, .06);
}

.brand {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 18px 18px 14px;
}
.brand-mark {
  width: 34px; height: 34px;
  display: grid; place-items: center;
  border-radius: 10px;
  background: linear-gradient(135deg, #0a84ff, #5e5ce6);
  color: #fff; font-size: 17px; font-weight: 700;
  box-shadow: 0 2px 8px rgba(10, 132, 255, .35);
}
.brand-text { display: flex; flex-direction: column; line-height: 1.25; }
.brand-name { font-size: 15px; font-weight: 700; letter-spacing: -.02em; color: var(--apple-text); }
.brand-sub { font-size: 11px; color: var(--apple-text-3); }

.menu-scroll { flex: 1; }
.menu-scroll :deep(.el-menu) {
  background: transparent;
  border-right: 0;
  padding: 0 10px 12px;
}
.menu-group {
  margin: 14px 10px 4px;
  font-size: 11px;
  font-weight: 600;
  color: var(--apple-text-3);
  letter-spacing: .04em;
}
.menu-scroll :deep(.el-menu-item) {
  height: 38px;
  line-height: 38px;
  margin: 1px 0;
  border-radius: 9px;
  color: var(--apple-text);
  font-size: 13.5px;
  font-weight: 500;
  transition: background .15s ease, color .15s ease;
}
.menu-scroll :deep(.el-menu-item:hover) { background: rgba(0, 0, 0, .05); }
.menu-scroll :deep(.el-menu-item.is-active) {
  background: var(--apple-blue);
  color: #fff;
  box-shadow: 0 1px 5px rgba(0, 113, 227, .35);
}
.menu-scroll :deep(.el-menu-item .el-icon) { font-size: 16px; margin-right: 9px; }

/* ---------- 顶栏：毛玻璃 ---------- */
.main-column { height: 100vh; }
.topbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 28px;
  background: rgba(255, 255, 255, .72);
  backdrop-filter: blur(20px) saturate(180%);
  border-bottom: 1px solid rgba(0, 0, 0, .06);
}
.page-title {
  margin: 0;
  font-size: 17px;
  font-weight: 600;
  letter-spacing: -.02em;
  color: var(--apple-text);
}
.header-user { display: flex; gap: 14px; align-items: center; }
.user-chip { display: flex; align-items: center; gap: 8px; }
.avatar {
  width: 28px; height: 28px;
  display: grid; place-items: center;
  border-radius: 50%;
  background: linear-gradient(135deg, #0a84ff, #5e5ce6);
  color: #fff; font-size: 12.5px; font-weight: 600;
}
.user-name { font-size: 13.5px; font-weight: 500; color: var(--apple-text); }
.logout-btn {
  width: 30px; height: 30px;
  display: grid; place-items: center;
  border: none; border-radius: 50%;
  background: transparent;
  color: var(--apple-text-2);
  font-size: 15px;
  cursor: pointer;
  transition: background .15s ease, color .15s ease;
}
.logout-btn:hover { background: rgba(255, 59, 48, .1); color: var(--apple-red); }

/* ---------- 内容区 ---------- */
.content {
  background: var(--apple-bg);
  padding: 24px 28px;
  overflow-y: auto;
}
</style>
