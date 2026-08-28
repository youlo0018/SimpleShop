import { createRouter, createWebHashHistory } from 'vue-router'
import { hasPermission } from '@/utils/permission'

const routes = [
  { path: '/login', component: () => import('@/views/Login.vue'), meta: { title: '登录' } },
  {
    path: '/',
    component: () => import('@/layouts/AdminLayout.vue'),
    redirect: '/dashboard',
    children: [
      { path: 'dashboard', component: () => import('@/views/Dashboard.vue'), meta: { title: '工作台', permission: 'dashboard:view' } },
      { path: 'users', component: () => import('@/views/Users.vue'), meta: { title: '用户管理', permission: 'user:read' } },
      { path: 'categories', component: () => import('@/views/Categories.vue'), meta: { title: '分类管理', permission: 'category:read' } },
      { path: 'products', component: () => import('@/views/Products.vue'), meta: { title: '商品管理', permission: 'product:read' } },
      { path: 'products/create', component: () => import('@/views/ProductEdit.vue'), meta: { title: '添加商品', permission: 'product:create' } },
      { path: 'products/:id/edit', component: () => import('@/views/ProductEdit.vue'), meta: { title: '编辑商品', permission: 'product:create' } },
      { path: 'products/:id/detail', component: () => import('@/views/ProductDetail.vue'), meta: { title: '商品详情', permission: 'product:read' } },
      { path: 'orders', component: () => import('@/views/Orders.vue'), meta: { title: '订单管理', permission: 'order:read' } },
      { path: 'orders/:id/detail', component: () => import('@/views/OrderDetail.vue'), meta: { title: '订单详情', permission: 'order:read' } },
      { path: 'refunds', component: () => import('@/views/Refunds.vue'), meta: { title: '退款管理', permission: 'refund:read' } },
      { path: 'refunds/:id/detail', component: () => import('@/views/RefundDetail.vue'), meta: { title: '退款详情', permission: 'refund:read' } },
      { path: 'merchants', component: () => import('@/views/Merchants.vue'), meta: { title: '商户管理', permission: 'merchant:read' } },
      { path: 'platforms', component: () => import('@/views/Platforms.vue'), meta: { title: '平台管理', permission: 'platform:read' } },
      { path: 'app-design', component: () => import('@/views/AppDesign.vue'), meta: { title: '小程序装修', permission: 'platform:update' } },
      { path: 'permissions', component: () => import('@/views/Permissions.vue'), meta: { title: '角色权限', permission: 'permission:manage' } }
    ]
  }
]

const router = createRouter({ history: createWebHashHistory(), routes })

router.beforeEach((to) => {
  document.title = `${to.meta.title || 'SimpleShop'} - 运营后台`
  if (to.path !== '/login' && !localStorage.getItem('admin_token')) return '/login'
  if (to.meta.permission && !hasPermission(to.meta.permission)) return '/dashboard'
})

export default router
