<template>
  <el-card class="page-card">
    <div class="toolbar"><el-button type="primary" @click="openCreate">新增平台</el-button><el-button @click="load">刷新</el-button></div>

    <el-table :data="rows" border>
      <el-table-column prop="platformCode" label="编码" min-width="140" />
      <el-table-column prop="platformName" label="名称" min-width="160" />
      <el-table-column prop="contactEmail" label="联系邮箱" min-width="180" />
      <el-table-column prop="defaultCommissionRate" label="默认佣金率%" width="115" />
      <el-table-column label="状态" width="90"><template #default="{ row }">{{ row.isEnabled ? '启用' : '停用' }}</template></el-table-column>
      <el-table-column label="操作" width="130" fixed="right">
        <template #default="{ row }">
          <el-button link type="primary" @click="openEdit(row)">编辑</el-button>
          <el-button link :type="row.isEnabled ? 'danger' : 'success'" @click="toggle(row)">{{ row.isEnabled ? '禁用' : '启用' }}</el-button>
        </template>
      </el-table-column>
    </el-table>
    <el-pagination class="pager" v-model:current-page="query.page" :page-size="query.pageSize" :total="total" layout="total, prev, pager, next" @current-change="load" />

    <el-dialog v-model="dialog" :title="form.id ? '编辑平台' : '新增平台'" width="520px" @closed="resetForm">
      <el-form ref="formRef" :model="form" :rules="rules" label-width="110px">
        <el-form-item label="平台编码" prop="platformCode"><el-input v-model="form.platformCode" :disabled="!!form.id" /></el-form-item>
        <el-form-item label="平台名称" prop="platformName"><el-input v-model="form.platformName" maxlength="64" /></el-form-item>
        <el-form-item label="联系邮箱" prop="contactEmail"><el-input v-model="form.contactEmail" /></el-form-item>
        <el-form-item label="默认佣金率%" prop="defaultCommissionRate"><el-input-number v-model="form.defaultCommissionRate" :min="0" :max="100" :precision="2" /></el-form-item>
      </el-form>
      <template #footer><el-button @click="dialog = false">取消</el-button><el-button type="primary" @click="save">保存</el-button></template>
    </el-dialog>
  </el-card>
</template>

<script setup>
import { ElMessage } from 'element-plus'
import { onMounted, reactive, ref } from 'vue'
import request from '@/api/request'
import { EMAIL_PATTERN, PLATFORM_CODE_PATTERN, optionalPattern, trimForm } from '@/utils/validators'

const rows = ref([]); const total = ref(0); const dialog = ref(false); const formRef = ref(null)
const query = reactive({ keyword: '', page: 1, pageSize: 10 })
const emptyForm = () => ({ id: 0, platformCode: '', platformName: '', contactEmail: '', defaultCommissionRate: 5 })
const form = reactive(emptyForm())
const rules = {
  platformCode: [{ required: true, message: '请输入平台编码', trigger: 'blur' }, optionalPattern(PLATFORM_CODE_PATTERN, '3-32位字母开头，可用数字、横线、下划线')],
  platformName: [{ required: true, min: 2, max: 64, message: '平台名称必须为2-64个字符', trigger: 'blur' }],
  contactEmail: [{ required: true, message: '请输入联系邮箱', trigger: 'blur' }, optionalPattern(EMAIL_PATTERN, '邮箱格式不正确')],
  defaultCommissionRate: [{ required: true, message: '请输入佣金率', trigger: 'change' }]
}

const load = async () => { const data = await request.get('/platforms/List', { params: query }); rows.value = data.items || []; total.value = Number(data.total || 0) }
const resetForm = () => Object.assign(form, emptyForm())
const openCreate = () => { resetForm(); dialog.value = true }
const openEdit = row => { Object.assign(form, { ...row }); dialog.value = true }
const toggle = async row => { await request.post('/platforms/SetEnabled', { id: row.id, isEnabled: !row.isEnabled }); ElMessage.success('状态已更新'); load() }
const save = async () => {
  const valid = await formRef.value?.validate().catch(() => false)
  if (!valid) return ElMessage.warning('请按红色提示修正输入')
  trimForm(form)
  if (form.id) await request.put(`/platforms/Edit/${form.id}`, form)
  else await request.post('/platforms/Create', form)
  ElMessage.success('已保存'); dialog.value = false; load()
}
onMounted(load)
</script>
