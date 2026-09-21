<template>
  <el-card class="page-card">
    <div class="header-row"><h3>商品详情</h3><el-button @click="$router.push('/products')">返回列表</el-button></div>
    <el-descriptions v-if="product" :column="2" border>
      <el-descriptions-item label="商品名称">{{ product.name }}</el-descriptions-item>
      <el-descriptions-item label="分类">{{ categoryName(product.categoryId) }}</el-descriptions-item>
      <el-descriptions-item v-if="!merchantScoped" label="商户">{{ merchantName(product.merchantId) }}</el-descriptions-item>
      <el-descriptions-item label="状态">{{ statusText[product.status] || '未知' }}</el-descriptions-item>
      <el-descriptions-item label="审核状态">{{ reviewText[product.reviewStatus] || '未审核' }}</el-descriptions-item>
      <el-descriptions-item label="创建时间">{{ product.createdAt }}</el-descriptions-item>
    </el-descriptions>

    <el-card v-if="product" shadow="never" class="section"><template #header>主图</template><el-image fit="contain" :src="absoluteUrl(product.mainImage)" class="detail-image" preview-teleported /></el-card>
    <el-card v-if="product" shadow="never" class="section"><template #header>商品描述</template><div v-html="product.description"></div></el-card>

    <el-card shadow="never" class="section">
      <template #header>规格明细</template>
      <el-table :data="skus" border><el-table-column prop="specName" label="规格名" /><el-table-column prop="specValue" label="规格值" /><el-table-column prop="skuCode" label="SKU编码" /><el-table-column prop="price" label="售价" width="110"><template #default="{ row }">¥{{ Number(row.price).toFixed(2) }}</template></el-table-column><el-table-column prop="stock" label="库存" width="90" /></el-table>
    </el-card>
  </el-card>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue'
import { useRoute } from 'vue-router'
import request from '@/api/request'
import { isMerchantScoped } from '@/utils/tenant'

const route = useRoute(); const product = ref(null); const skus = ref([]); const categories = ref([]); const merchants = ref([])
const merchantScoped = isMerchantScoped()
const apiBase = import.meta.env.VITE_API_BASE || 'http://127.0.0.1:5008/gateway'
const statusText = { 0: '草稿', 1: '已上架', 2: '已下架' }
const reviewText = { 0: '待审核', 1: '已审核', 2: '已拒绝' }
const flattenCategories = items => items.flatMap(item => [item, ...flattenCategories(item.children || [])])
const allCategories = computed(() => flattenCategories(categories.value))
const categoryName = id => allCategories.value.find(item => String(item.id) === String(id))?.name || id
const merchantName = id => merchants.value.find(item => String(item.id) === String(id))?.merchantName || id
const absoluteUrl = url => !url || /^https?:/.test(url) ? url : `${apiBase.split('/gateway')[0]}${url}`

onMounted(async () => {
  const [detail, categoryData, merchantData] = await Promise.all([
    request.get('/products/AdminDetail', { params: { id: route.params.id } }),
    request.get('/products/GetCategoryTree'),
    merchantScoped ? Promise.resolve({ items: [] }) : request.get('/merchants/List', { params: { page: 1, pageSize: 100 } }).catch(() => ({ items: [] }))
  ])
  product.value = detail.product; skus.value = detail.skus || []
  categories.value = categoryData || []; merchants.value = merchantData.items || []
})
</script>

<style scoped>.header-row{display:flex;justify-content:space-between;align-items:center}.section{margin-top:16px}.detail-image{width:280px;height:220px}</style>
