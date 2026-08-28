<template>
  <el-card class="page-card">
    <div class="header-row"><h3>订单详情</h3><el-button @click="$router.push('/orders')">返回列表</el-button></div>
    <template v-if="order">
      <el-descriptions :column="3" border>
        <el-descriptions-item label="订单号">{{ order.orderNo }}</el-descriptions-item>
        <el-descriptions-item label="订单状态">{{ statusText[order.orderStatus] || '未知' }}</el-descriptions-item>
        <el-descriptions-item label="买家">{{ order.customerName }}</el-descriptions-item>
        <el-descriptions-item label="总金额">¥{{ Number(order.totalPrice || 0).toFixed(2) }}</el-descriptions-item>
        <el-descriptions-item label="优惠金额">¥{{ Number(order.allDiscountPrice || 0).toFixed(2) }}</el-descriptions-item>
        <el-descriptions-item label="支付金额"><b class="amount">¥{{ Number(order.paymentPrice || 0).toFixed(2) }}</b></el-descriptions-item>
        <el-descriptions-item label="收货人">{{ order.receiverName }}</el-descriptions-item>
        <el-descriptions-item label="手机号">{{ order.receiverPhone }}</el-descriptions-item>
        <el-descriptions-item label="支付时间">{{ order.paymentAt || '-' }}</el-descriptions-item>
      </el-descriptions>

      <el-card shadow="never" class="section">
        <template #header>
          <div class="header-row"><span>商品明细</span><el-button v-if="canRefund" type="danger" @click="refundDialog = true">申请退款</el-button></div>
        </template>
        <el-table :data="items" border>
          <el-table-column prop="productName" label="商品" min-width="220" />
          <el-table-column prop="price" label="单价" width="115"><template #default="{ row }">¥{{ Number(row.price || 0).toFixed(2) }}</template></el-table-column>
          <el-table-column prop="quantity" label="数量" width="90" />
          <el-table-column prop="subtotalAmount" label="小计" width="125"><template #default="{ row }">¥{{ Number(row.price * row.quantity || 0).toFixed(2) }}</template></el-table-column>
        </el-table>
      </el-card>

      <el-card shadow="never" class="section">
        <template #header>退款记录</template>
        <el-empty v-if="!refunds.length" description="暂无退款记录" />
        <el-table v-else :data="refunds" border>
          <el-table-column prop="refundNo" label="退款单号" min-width="200" /><el-table-column prop="amount" label="金额" width="110"><template #default="{ row }">¥{{ Number(row.amount).toFixed(2) }}</template></el-table-column><el-table-column prop="status" label="状态" width="100"><template #default="{ row }">{{ refundStatus[row.status] }}</template></el-table-column><el-table-column prop="reason" label="原因" min-width="150" />
        </el-table>
      </el-card>

      <el-dialog v-model="refundDialog" title="申请全额退款" width="440px" @closed="resetRefundForm">
        <el-form label-width="90px"><el-form-item label="退款金额"><el-input-number v-model="refundAmount" :min="0.01" :max="Number(order.paymentPrice)" :precision="2" /></el-form-item><el-form-item label="原因"><el-input v-model="refundReason" placeholder="后台审核退款" /></el-form-item></el-form>
        <template #footer><el-button @click="refundDialog = false">取消</el-button><el-button type="primary" @click="createRefund">提交</el-button></template>
      </el-dialog>
    </template>
  </el-card>
</template>

<script setup>
import { ElMessage } from 'element-plus'
import { computed, onMounted, reactive, ref } from 'vue'
import { useRoute } from 'vue-router'
import request from '@/api/request'

const route = useRoute(); const order = ref(null); const items = ref([]); const refunds = ref([]); const refundDialog = ref(false)
const refundForm = reactive({ amount: 0, reason: '' }); const refundAmount = ref(0); const refundReason = ref('')
const statusText = { 10: '待支付', 20: '已支付', 40: '已发货', 50: '已完成', 60: '已退款', 90: '已取消', 91: '已关闭' }
const refundStatus = { 10: '待处理', 20: '已退款', 90: '已拒绝' }
const canRefund = computed(() => [20, 40, 50].includes(Number(order.value?.orderStatus)) && Boolean(Number(order.value?.isPayment)))

const resetRefundForm = () => { refundForm.amount = 0; refundForm.reason = ''; refundAmount.value = Number(order.value?.paymentPrice || 0); refundReason.value = '' }
const createRefund = async () => {
  const result = await request.post('/payments/Refund', {
    bizNo: order.value.orderNo, amount: refundAmount.value,
    reason: refundReason.value || '后台审核退款',
    items: items.value.map(item => ({ skuId: item.skuId, quantity: item.quantity }))
  })
  if (result?.success === false) { ElMessage.error(result.message || '退款申请失败'); return }
  ElMessage.success('退款申请已创建'); refundDialog.value = false; load()
}

const load = async () => {
  const detail = await request.get('/orders/Detail', { params: { id: route.params.id } })
  if (detail.success === false) return
  order.value = detail.order; items.value = detail.items || []
  const refundData = await request.get('/payments/Refunds', { params: { keyword: order.value.orderNo, page: 1, pageSize: 50 } })
  refunds.value = refundData.items || []
}
onMounted(() => { resetRefundForm(); load() })
</script>

<style scoped>.header-row{display:flex;justify-content:space-between;align-items:center}.section{margin-top:16px}.amount{color:#f56c6c}</style>
