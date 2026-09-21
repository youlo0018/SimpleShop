<template>
  <el-card class="page-card">
    <div class="toolbar">
      <el-input v-model="query.keyword" placeholder="商品名称" clearable style="width:240px" @keyup.enter="load" />
      <el-select v-model="query.status" placeholder="状态" clearable style="width:140px">
        <el-option v-for="(label, value) in statusText" :key="value" :value="Number(value)" :label="label" />
      </el-select>
      <el-button type="primary" @click="load">查询</el-button>
      <el-button type="success" @click="$router.push('/products/create')">添加商品</el-button>
    </div>

    <el-table :data="rows" border>
      <el-table-column label="商品" min-width="300"><template #default="{ row }">
        <div class="product-cell">
          <el-image fit="cover" class="product-image" :src="absoluteUrl(row.mainImage)" preview-teleported><template #error><div class="image-fallback">无图</div></template></el-image>
          <div><div>{{ row.name }}</div><small class="muted">SKU {{ (row.skus || []).length }}</small></div>
        </div>
      </template></el-table-column>
      <el-table-column label="分类" width="130"><template #default="{ row }">{{ categoryName(row.categoryId) }}</template></el-table-column>
      <el-table-column v-if="!merchantScoped" label="商户" width="150"><template #default="{ row }">{{ merchantName(row.merchantId) }}</template></el-table-column>
      <el-table-column label="价格" width="110"><template #default="{ row }">{{ skuRange(row.skus) }}</template></el-table-column>
      <el-table-column label="状态" width="100"><template #default="{ row }">{{ statusText[row.status] || '未知' }}</template></el-table-column>
      <el-table-column label="审核" width="90"><template #default="{ row }">{{ reviewText[row.reviewStatus] || '未审核' }}</template></el-table-column>
      <el-table-column label="操作" width="240" fixed="right">
        <template #default="{ row }">
          <el-button link type="primary" @click="$router.push(`/products/${row.id}/detail`)">详情</el-button>
          <el-button link type="primary" @click="$router.push(`/products/${row.id}/edit`)">编辑</el-button>
          <el-button link type="danger" @click="togglePublish(row)">{{ Number(row.status) === 1 ? '下架' : '审核上架' }}</el-button>
        </template>
      </el-table-column>
    </el-table>

    <el-pagination class="pager" v-model:current-page="query.page" :page-size="query.pageSize" :total="total" layout="total, prev, pager, next" @current-change="load" />
  </el-card>
</template>

<script setup>
import { ElMessage } from 'element-plus'
import { computed, onMounted, reactive, ref } from 'vue'
import request from '@/api/request'
import { isMerchantScoped } from '@/utils/tenant'

const apiBase = import.meta.env.VITE_API_BASE || 'http://127.0.0.1:5008/gateway'
const rows = ref([]); const categories = ref([]); const merchants = ref([]); const total = ref(0)
const merchantScoped = isMerchantScoped()
const query = reactive({ keyword: '', page: 1, pageSize: 10, status: null })
const statusText = { 0: '草稿', 1: '已上架', 2: '已下架' }
const reviewText = { 0: '待审核', 1: '已审核', 2: '已拒绝' }

const absoluteUrl = url => !url || /^https?:/.test(url) ? url : `${apiBase.split('/gateway')[0]}${url}`
const loadRefs = async () => {
  const [categoryData, merchantData] = await Promise.all([
    request.get('/products/GetCategoryTree'),
    merchantScoped ? Promise.resolve({ items: [] }) : request.get('/merchants/List', { params: { page: 1, pageSize: 100 } }).catch(() => ({ items: [] }))
  ])
  categories.value = categoryData || []
  merchants.value = merchantData.items || []
}
const load = async () => {
  const data = await request.get('/products/List', { params: query })
  rows.value = data.items || []; total.value = Number(data.total || 0)
}
const flattenCategories = items => items.flatMap(item => [item, ...flattenCategories(item.children || [])])
const allCategories = computed(() => flattenCategories(categories.value))
const categoryName = id => allCategories.value.find(item => String(item.id) === String(id))?.name || id
const merchantName = id => merchants.value.find(item => String(item.id) === String(id))?.merchantName || id
const skuRange = skus => {
  const prices = (skus || []).map(sku => Number(sku.price)).filter(value => value > 0)
  if (!prices.length) return '-'
  const min = Math.min(...prices); const max = Math.max(...prices)
  return min === max ? `¥${min.toFixed(2)}` : `¥${min.toFixed(2)} - ¥${max.toFixed(2)}`
}

const togglePublish = async row => {
  if (Number(row.status) === 1) await request.post('/products/OffShelf', { id: row.id, approved: true })
  else await request.post('/products/PublishProduct', { id: row.id, approved: true })
  ElMessage.success('已更新'); load()
}

onMounted(async () => { await Promise.all([loadRefs(), load()]) })
</script>

<style scoped>
.product-cell { display: flex; align-items: center; gap: 10px; }
.product-image { width: 52px; height: 52px; border-radius: 10px; background: var(--apple-bg, #f5f5f7); }
.image-fallback { display: grid; place-items: center; width: 100%; height: 100%; color: #a1a1a6; font-size: 12px; }
.muted { color: var(--apple-text-3, #86868b); }
</style>
