<template>
  <el-card class="page-card">
    <div class="header-row"><h3>订单详情</h3><el-button @click="$router.push('/orders')">返回列表</el-button></div>
    <template v-if="order">
      <el-descriptions :column="3" border>
        <el-descriptions-item label="订单号">{{ order.orderNo }}</el-descriptions-item>
        <el-descriptions-item label="订单状态">{{ statusText[order.orderStatus] || '未知' }}</el-descriptions-item>
        <el-descriptions-item label="买家">{{ order.customerName }}</el-descriptions-item>
        <el-descriptions-item label="总金额">¥{{ Number(order.totalPrice || 0).toFixed(2) }}</el-descriptions-item>
        <el-descriptions-item label="活动折扣">¥{{ Number(order.activityDiscountPrice || 0).toFixed(2) }}</el-descriptions-item>
        <el-descriptions-item label="卡券抵扣">¥{{ Number(order.couponDiscountPrice || 0).toFixed(2) }}</el-descriptions-item>
        <el-descriptions-item label="优惠合计">¥{{ Number(order.allDiscountPrice || 0).toFixed(2) }}</el-descriptions-item>
        <el-descriptions-item label="支付金额"><b class="amount">¥{{ Number(order.paymentPrice || 0).toFixed(2) }}</b></el-descriptions-item>
        <el-descriptions-item label="收货人">{{ order.receiverName }}</el-descriptions-item>
        <el-descriptions-item label="手机号">{{ order.receiverPhone }}</el-descriptions-item>
        <el-descriptions-item label="支付时间">{{ order.paymentAt || '-' }}</el-descriptions-item>
      </el-descriptions>

      <el-card shadow="never" class="section">
        <template #header>
          <div class="header-row"><span>商品明细</span><el-button v-if="canRefund" type="danger" @click="openRefund">申请退款</el-button></div>
        </template>
        <el-table :data="items" border>
          <el-table-column prop="productName" label="商品" min-width="200" />
          <el-table-column prop="price" label="单价" width="105"><template #default="{ row }">¥{{ Number(row.price || 0).toFixed(2) }}</template></el-table-column>
          <el-table-column prop="quantity" label="数量" width="70" />
          <el-table-column label="优惠方式" min-width="150">
            <template #default="{ row }">
              <el-tag v-if="Number(row.marketingType) === 1" type="warning" size="small">活动：{{ row.marketingName }}</el-tag>
              <el-tag v-else-if="Number(row.marketingType) === 2" type="success" size="small">券：{{ row.marketingName }}</el-tag>
              <span v-else class="muted">—</span>
            </template>
          </el-table-column>
          <el-table-column label="优惠金额" width="105"><template #default="{ row }">¥{{ Number(row.discountAmount || 0).toFixed(2) }}</template></el-table-column>
          <el-table-column label="实付小计" width="115"><template #default="{ row }">¥{{ Number(row.price * row.quantity - (row.discountAmount || 0)).toFixed(2) }}</template></el-table-column>
        </el-table>
      </el-card>

      <el-card shadow="never" class="section">
        <template #header>退款记录</template>
        <el-empty v-if="!refunds.length" description="暂无退款记录" />
        <el-table v-else :data="refunds" border>
          <el-table-column prop="refundNo" label="退款单号" min-width="200" /><el-table-column prop="amount" label="金额" width="110"><template #default="{ row }">¥{{ Number(row.amount).toFixed(2) }}</template></el-table-column><el-table-column prop="status" label="状态" width="100"><template #default="{ row }">{{ refundStatus[row.status] }}</template></el-table-column><el-table-column prop="reason" label="原因" min-width="150" />
        </el-table>
      </el-card>

      <el-dialog v-model="refundDialog" title="申请退款" width="440px" @closed="resetRefundForm">
        <el-form ref="refundFormRef" :model="refundForm" :rules="refundRules" label-width="90px">
          <el-form-item label="退款金额" prop="amount">
            <el-input-number v-model="refundForm.amount" :min="0.01" :max="remainingRefund" :precision="2" style="width:100%" />
          </el-form-item>
          <el-form-item label="原因" prop="reason">
            <el-input v-model="refundForm.reason" maxlength="255" placeholder="后台审核退款" />
          </el-form-item>
          <el-form-item label="可退余额"><span class="muted">¥{{ remainingRefund.toFixed(2) }}</span></el-form-item>
        </el-form>
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
import { maxLengthRule, trimForm } from '@/utils/validators'

const route = useRoute(); const order = ref(null); const items = ref([]); const refunds = ref([]); const refundDialog = ref(false)
const refundForm = reactive({ amount: 0, reason: '' }); const refundFormRef = ref(null)
const statusText = { 10: '待支付', 20: '已支付', 40: '已发货', 50: '已完成', 60: '已退款', 90: '已取消', 91: '已关闭' }
const refundStatus = { 10: '待处理', 20: '已退款', 90: '已拒绝' }
const canRefund = computed(() => [20, 40, 50].includes(Number(order.value?.orderStatus)) && Boolean(Number(order.value?.isPayment)))

// 可退余额 = 实付金额 - 已占用的退款（待处理/已退款），与后端累计退款上限一致。
const remainingRefund = computed(() => {
  const paid = Number(order.value?.paymentPrice || 0)
  const committed = (refunds.value || [])
    .filter(item => [10, 20].includes(Number(item.status)))
    .reduce((sum, item) => sum + Number(item.amount || 0), 0)
  return Math.max(0, Math.round((paid - committed) * 100) / 100)
})

const refundRules = {
  amount: [{
    validator: (rule, value, callback) => {
      const amount = Number(value)
      if (!Number.isFinite(amount) || amount <= 0) return callback(new Error('退款金额必须大于0'))
      if (Math.round(amount * 100) !== amount * 100) return callback(new Error('金额最多保留两位小数'))
      if (amount > remainingRefund.value) return callback(new Error(`退款金额不能超过可退余额 ¥${remainingRefund.value.toFixed(2)}`))
      callback()
    },
    trigger: 'blur'
  }],
  reason: [maxLengthRule(255, '退款原因')]
}

const openRefund = () => { resetRefundForm(); refundDialog.value = true }
const resetRefundForm = () => { refundForm.amount = remainingRefund.value || Number(order.value?.paymentPrice || 0); refundForm.reason = '' }
const createRefund = async () => {
  const valid = await refundFormRef.value?.validate().catch(() => false)
  if (!valid) return
  trimForm(refundForm)
  const result = await request.post('/payments/Refund', {
    bizNo: order.value.orderNo, amount: refundForm.amount,
    reason: refundForm.reason || '后台审核退款',
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
onMounted(load)
</script>

<style scoped>.header-row{display:flex;justify-content:space-between;align-items:center}.section{margin-top:16px}.amount{color:var(--apple-red,#ff3b30);font-weight:600}</style>
