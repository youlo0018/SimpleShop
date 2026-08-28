<template>
  <el-card class="page-card">
    <div class="toolbar">
      <el-input v-model="query.keyword" placeholder="退款单号 / 订单号" clearable style="width:280px" @keyup.enter="load" />
      <el-select v-model="query.status" placeholder="状态" clearable style="width:130px"><el-option v-for="(label, value) in statusText" :key="value" :value="Number(value)" :label="label" /></el-select>
      <el-button type="primary" @click="load">查询</el-button>
    </div>
    <el-table :data="rows" border>
      <el-table-column prop="refundNo" label="退款单号" min-width="200" />
      <el-table-column prop="bizNo" label="订单号" min-width="200" />
      <el-table-column prop="amount" label="金额" width="115"><template #default="{ row }">¥{{ Number(row.amount).toFixed(2) }}</template></el-table-column>
      <el-table-column label="状态" width="100"><template #default="{ row }">{{ statusText[row.status] || '未知' }}</template></el-table-column>
      <el-table-column prop="createdAt" label="申请时间" width="180" />
      <el-table-column label="操作" width="240" fixed="right">
        <template #default="{ row }">
          <el-button link type="primary" @click="$router.push(`/refunds/${row.id}/detail`)">详情</el-button>
          <el-button v-if="Number(row.status) === 10" link type="success" @click="decide(row, 'approve')">同意</el-button>
          <el-button v-if="Number(row.status) === 10" link type="danger" @click="decide(row, 'reject')">不同意</el-button>
        </template>
      </el-table-column>
    </el-table>
    <el-pagination class="pager" v-model:current-page="query.page" :page-size="query.pageSize" :total="total" layout="total, prev, pager, next" @current-change="load" />
  </el-card>
</template>

<script setup>
import { ElMessage } from 'element-plus'
import { onMounted, reactive, ref } from 'vue'
import request from '@/api/request'

const rows = ref([]); const total = ref(0)
const query = reactive({ keyword: '', status: null, page: 1, pageSize: 10 })
const statusText = { 10: '待处理', 20: '已退款', 90: '已拒绝' }
const load = async () => { const data = await request.get('/payments/Refunds', { params: query }); rows.value = data.items || []; total.value = Number(data.total || 0) }
const decide = async (row, action) => {
  await request.post(`/payments/${action === 'approve' ? 'ApproveRefund' : 'RejectRefund'}`, { id: row.id, reason: action === 'approve' ? null : '后台列表拒绝' })
  ElMessage.success(action === 'approve' ? '退款已同意' : '退款已拒绝'); load()
}
onMounted(load)
</script>
