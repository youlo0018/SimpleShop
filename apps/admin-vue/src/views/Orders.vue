<template>
  <el-card class="page-card">
    <div class="toolbar">
      <el-input v-model="query.keyword" placeholder="订单号 / 收货人 / 电话" clearable style="width:260px" @keyup.enter="load" />
      <el-select v-model="query.status" placeholder="订单状态" clearable style="width:150px">
        <el-option v-for="(label, value) in statusText" :key="value" :value="Number(value)" :label="label" />
      </el-select>
      <el-button type="primary" @click="load">查询</el-button>
    </div>

    <el-table :data="rows" border>
      <el-table-column prop="orderNo" label="订单号" min-width="200" />
      <el-table-column prop="customerName" label="买家" width="120" />
      <el-table-column prop="paymentPrice" label="支付金额" width="115"><template #default="{ row }">¥{{ Number(row.paymentPrice || 0).toFixed(2) }}</template></el-table-column>
      <el-table-column label="状态" width="100"><template #default="{ row }">{{ statusText[row.orderStatus] || '未知' }}</template></el-table-column>
      <el-table-column label="支付/退款" width="120"><template #default="{ row }">{{ row.isPayment ? '已支付' : '未支付' }}<template v-if="row.isRefund"> / 退款</template></template></el-table-column>
      <el-table-column prop="createdAt" label="创建时间" width="180" />
      <el-table-column label="操作" width="160" fixed="right">
        <template #default="{ row }">
          <el-button link type="primary" @click="$router.push(`/orders/${row.id}/detail`)">详情</el-button>
          <el-button link type="success" :disabled="!canShip(row)" @click="openShip(row)">发货</el-button>
        </template>
      </el-table-column>
    </el-table>

    <el-pagination class="pager" v-model:current-page="query.page" :page-size="query.pageSize" :total="total" layout="total, prev, pager, next" @current-change="load" />

    <el-dialog v-model="shipDialog" title="订单发货" width="480px" @closed="resetShipForm">
      <el-form label-width="90px">
        <el-form-item label="物流公司"><el-input v-model="shipForm.logisticsCompany" /></el-form-item>
        <el-form-item label="运单号"><el-input v-model="shipForm.trackingNo" /></el-form-item>
        <el-form-item label="发货明细"><div class="muted">将按订单全部商品明细发货</div></el-form-item>
      </el-form>
      <template #footer><el-button @click="shipDialog = false">取消</el-button><el-button type="primary" :loading="shipping" @click="ship">确认发货</el-button></template>
    </el-dialog>
  </el-card>
</template>

<script setup>
import { ElMessage } from 'element-plus'
import { onMounted, reactive, ref } from 'vue'
import request from '@/api/request'

const rows = ref([]); const total = ref(0); const shipDialog = ref(false); const shipping = ref(false)
const query = reactive({ keyword: '', page: 1, pageSize: 10, status: null })
const shipForm = reactive({ orderId: '', logisticsCompany: '', trackingNo: '' })
const statusText = { 10: '待支付', 20: '已支付', 40: '已发货', 50: '已完成', 60: '已退款', 90: '已取消', 91: '已关闭' }

const load = async () => {
  const data = await request.get('/orders/List', { params: query })
  rows.value = data.items || []; total.value = Number(data.total || 0)
}
const resetShipForm = () => Object.assign(shipForm, { orderId: '', logisticsCompany: '', trackingNo: '' })
const openShip = async row => {
  resetShipForm(); shipForm.orderId = row.id; shipDialog.value = true
}
const canShip = row => Number(row.orderStatus) === 20 && Boolean(Number(row.isPayment))
const ship = async () => {
  if (!shipForm.logisticsCompany || !shipForm.trackingNo) { ElMessage.warning('请输入物流公司和运单号'); return }
  shipping.value = true
  try {
    const detail = await request.get('/orders/Detail', { params: { id: shipForm.orderId } })
    const items = (detail.items || []).map(item => ({ orderItemId: item.id, skuId: item.skuId, quantity: item.quantity }))
    await request.post('/orders/Shipment', { ...shipForm, items })
    ElMessage.success('发货成功'); shipDialog.value = false; load()
  } finally { shipping.value = false }
}

onMounted(load)
</script>
