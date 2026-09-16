<template>
  <el-card class="page-card">
    <div class="header-row"><h3>退款单详情</h3><div><el-button @click="$router.push('/refunds')">返回列表</el-button><el-button v-if="canDecide" type="success" :disabled="Number(refund?.status) !== 10" @click="approve">同意</el-button><el-button v-if="canDecide" type="danger" :disabled="Number(refund?.status) !== 10" @click="rejectDialog = true">不同意</el-button></div></div>

    <template v-if="refund">
      <el-descriptions :column="3" border>
        <el-descriptions-item label="退款单号">{{ refund.refundNo }}</el-descriptions-item>
        <el-descriptions-item label="订单号">{{ refund.bizNo }}</el-descriptions-item>
        <el-descriptions-item label="状态">{{ statusText[refund.status] || '未知' }}</el-descriptions-item>
        <el-descriptions-item label="退款金额"><b>¥{{ Number(refund.amount).toFixed(2) }}</b></el-descriptions-item>
        <el-descriptions-item label="申请时间">{{ refund.createdAt }}</el-descriptions-item>
        <el-descriptions-item label="退款时间">{{ refund.refundedAt || '-' }}</el-descriptions-item>
      </el-descriptions>

      <el-card shadow="never" class="section">
        <template #header>关联订单</template>
        <el-descriptions v-if="order" :column="3" border>
          <el-descriptions-item label="订单号">{{ order.orderNo }}</el-descriptions-item>
          <el-descriptions-item label="总金额">¥{{ Number(order.totalPrice).toFixed(2) }}</el-descriptions-item>
          <el-descriptions-item label="支付金额">¥{{ Number(order.paymentPrice).toFixed(2) }}</el-descriptions-item>
          <el-descriptions-item label="买家">{{ order.customerName }}</el-descriptions-item>
          <el-descriptions-item label="订单状态">{{ orderStatus[order.orderStatus] }}</el-descriptions-item>
          <el-descriptions-item label="是否退款">{{ order.isRefund ? '是' : '否' }}</el-descriptions-item>
        </el-descriptions>
        <el-empty v-else description="未找到关联订单" />
      </el-card>

      <el-card shadow="never" class="section">
        <template #header>该订单历史退款</template>
        <el-table :data="allRefunds" border><el-table-column prop="refundNo" label="退款单号" min-width="220" /><el-table-column prop="amount" label="金额" width="110"><template #default="{ row }">¥{{ Number(row.amount).toFixed(2) }}</template></el-table-column><el-table-column label="状态" width="100"><template #default="{ row }">{{ statusText[row.status] }}</template></el-table-column><el-table-column prop="createdAt" label="申请时间" width="180" /></el-table>
      </el-card>
    </template>

    <el-dialog v-model="rejectDialog" title="拒绝退款" width="420px" @closed="resetReject">
      <el-form ref="rejectFormRef" :model="rejectForm" :rules="rejectRules">
        <el-form-item prop="reason"><el-input v-model="rejectForm.reason" type="textarea" maxlength="255" show-word-limit placeholder="拒绝原因" /></el-form-item>
      </el-form>
      <template #footer><el-button @click="rejectDialog=false">取消</el-button><el-button type="danger" @click="reject">确认拒绝</el-button></template>
    </el-dialog>
  </el-card>
</template>

<script setup>
import { ElMessage } from 'element-plus'
import { computed, onMounted, reactive, ref } from 'vue'
import { useRoute } from 'vue-router'
import request from '@/api/request'
import { maxLengthRule, requiredRule, trimForm } from '@/utils/validators'

const route = useRoute(); const refund = ref(null); const items = ref([]); const allRefunds = ref([]); const order = ref(null); const rejectDialog = ref(false)
const rejectFormRef = ref(null); const rejectForm = reactive({ reason: '' })
const rejectRules = { reason: [requiredRule('请输入拒绝原因'), maxLengthRule(255, '拒绝原因')] }
const statusText = { 10: '待处理', 20: '已退款', 90: '已拒绝' }
const orderStatus = { 10: '待支付', 20: '已支付', 40: '已发货', 50: '已完成', 60: '已退款', 90: '已取消', 91: '已关闭' }
const canDecide = computed(() => !['customer'].includes(localStorage.getItem('admin_user') ? JSON.parse(localStorage.getItem('admin_user')).tenantType : 'customer'))

const resetReject = () => { rejectForm.reason = ''; rejectFormRef.value?.clearValidate() }
const approve = async () => { await request.post('/payments/ApproveRefund', { id: refund.value.id }); ElMessage.success('退款已同意'); load() }
const reject = async () => {
  const valid = await rejectFormRef.value?.validate().catch(() => false)
  if (!valid) return
  trimForm(rejectForm)
  await request.post('/payments/RejectRefund', { id: refund.value.id, reason: rejectForm.reason })
  ElMessage.success('退款已拒绝'); rejectDialog.value = false; load()
}

const load = async () => {
  const data = await request.get('/payments/RefundDetail', { params: { id: route.params.id } })
  refund.value = data.refund; items.value = data.items || []; allRefunds.value = data.allRefunds || []
  const orderData = await request.get('/orders/List', { params: { keyword: refund.value.bizNo, page: 1, pageSize: 1 } })
  order.value = (orderData.items || []).find(item => item.orderNo === refund.value.bizNo) || null
}
onMounted(load)
</script>

<style scoped>.header-row{display:flex;justify-content:space-between;align-items:center}.section{margin-top:16px}</style>
