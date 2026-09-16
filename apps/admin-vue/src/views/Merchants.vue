<template>
  <el-card class="page-card">
    <div class="toolbar">
      <el-input v-model="query.keyword" placeholder="商户 / 联系人 / 电话" clearable style="width:250px" @keyup.enter="load" />
      <el-select v-model="query.status" placeholder="状态" clearable style="width:130px"><el-option v-for="(label,value) in statusText" :key="value" :value="Number(value)" :label="label" /></el-select>
      <el-button type="primary" @click="load">查询</el-button>
      <el-button type="success" @click="openCreate">添加商户</el-button>
    </div>

    <el-table :data="rows" border>
      <el-table-column prop="merchantName" label="商户名称" min-width="170" />
      <el-table-column prop="contactName" label="联系人" width="100" />
      <el-table-column prop="contactPhone" label="电话" width="135" />
      <el-table-column label="所属平台" width="160"><template #default="{ row }">{{ platformText(row.platformId) }}</template></el-table-column>
      <el-table-column prop="commissionRate" label="佣金率%" width="95" />
      <el-table-column label="状态" width="100"><template #default="{ row }">{{ statusText[row.status] || '未知' }}</template></el-table-column>
      <el-table-column label="操作" width="230" fixed="right">
        <template #default="{ row }">
          <el-button link type="primary" @click="openEdit(row)">编辑</el-button>
          <el-button v-if="Number(row.status) === 10" link type="success" @click="review(row, 20)">通过</el-button>
          <el-button v-if="Number(row.status) === 10" link type="danger" @click="review(row, 30)">拒绝</el-button>
          <el-button v-if="[20, 40].includes(Number(row.status))" link :type="Number(row.status) === 40 ? 'success' : 'warning'" @click="setStatus(row, Number(row.status) === 40 ? 20 : 40)">{{ Number(row.status) === 40 ? '启用' : '禁用' }}</el-button>
        </template>
      </el-table-column>
    </el-table>
    <el-pagination class="pager" v-model:current-page="query.page" :page-size="query.pageSize" :total="total" layout="total, prev, pager, next" @current-change="load" />

    <el-dialog v-model="dialog" :title="form.id ? '编辑商户' : '添加商户'" width="560px" @closed="resetForm">
      <el-form ref="formRef" :model="form" :rules="rules" label-width="90px">
        <el-form-item label="所属平台" prop="platformId"><el-select v-model="form.platformId" filterable placeholder="选择平台" style="width:100%"><el-option v-for="platform in platforms" :key="platform.id" :value="platform.id" :label="platform.platformName" /></el-select></el-form-item>
        <el-form-item label="商户名称" prop="merchantName"><el-input v-model="form.merchantName" maxlength="64" /></el-form-item>
        <el-form-item label="联系人" prop="contactName"><el-input v-model="form.contactName" maxlength="32" /></el-form-item>
        <el-form-item label="手机号" prop="contactPhone"><el-input v-model="form.contactPhone" /></el-form-item>
        <el-form-item label="邮箱" prop="contactEmail"><el-input v-model="form.contactEmail" /></el-form-item>
        <el-form-item label="佣金率%" prop="commissionRate"><el-input-number v-model="form.commissionRate" :min="0" :max="100" :precision="2" /></el-form-item>
      </el-form>
      <template #footer><el-button @click="dialog = false">取消</el-button><el-button type="primary" @click="save">保存</el-button></template>
    </el-dialog>
  </el-card>
</template>

<script setup>
import { ElMessage } from 'element-plus'
import { onMounted, reactive, ref } from 'vue'
import request from '@/api/request'
import { EMAIL_PATTERN, PHONE_PATTERN, optionalPattern, trimForm } from '@/utils/validators'

const rows = ref([]); const platforms = ref([]); const total = ref(0); const dialog = ref(false); const formRef = ref(null)
const query = reactive({ keyword: '', status: null, page: 1, pageSize: 10 })
const emptyForm = () => ({ id: 0, platformId: '', merchantName: '', contactName: '', contactPhone: '', contactEmail: '', commissionRate: 5 })
const form = reactive(emptyForm())
const rules = {
  platformId: [{ required: true, message: '请选择所属平台', trigger: 'change' }],
  merchantName: [{ required: true, min: 2, max: 64, message: '商户名称必须为2-64个字符', trigger: 'blur' }],
  contactName: [{ required: true, max: 32, message: '请输入联系人', trigger: 'blur' }],
  contactPhone: [{ required: true, message: '请输入联系电话', trigger: 'blur' }, optionalPattern(PHONE_PATTERN, '手机号格式不正确')],
  contactEmail: [{ required: true, message: '请输入联系邮箱', trigger: 'blur' }, optionalPattern(EMAIL_PATTERN, '邮箱格式不正确')],
  commissionRate: [{ required: true, message: '请输入佣金率', trigger: 'change' }]
}
const statusText = { 0: '草稿', 10: '待审核', 20: '已入驻', 30: '已拒绝', 40: '已停用' }

const platformText = id => platforms.value.find(platform => String(platform.id) === String(id))?.platformName || id
const loadRefs = async () => {
  const data = await request.get('/platforms/List', { params: { page: 1, pageSize: 100 } }).catch(() => ({ items: [] }))
  platforms.value = data.items || []
}
const load = async () => { const data = await request.get('/merchants/List', { params: query }); rows.value = data.items || []; total.value = Number(data.total || 0) }
const resetForm = () => Object.assign(form, emptyForm())
const openCreate = async () => { await loadRefs(); resetForm(); form.platformId = platforms.value[0]?.id || ''; dialog.value = true }
const openEdit = async row => { await loadRefs(); Object.assign(form, { ...row }); dialog.value = true }
const review = async (row, status) => { await request.post('/merchants/Review', { id: row.id, status }); ElMessage.success('审核完成'); load() }
const setStatus = async (row, status) => { await request.post('/merchants/SetStatus', { id: row.id, status }); ElMessage.success('状态已更新'); load() }
const save = async () => {
  const valid = await formRef.value?.validate().catch(() => false)
  if (!valid) return ElMessage.warning('请按红色提示修正输入')
  trimForm(form)
  if (form.id) await request.post('/merchants/Update', form)
  else await request.post('/merchants/Create', form)
  ElMessage.success('已保存'); dialog.value = false; load()
}

onMounted(async () => { await Promise.all([loadRefs(), load()]) })
</script>
