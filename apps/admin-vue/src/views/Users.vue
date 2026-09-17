<template>
  <el-card class="page-card">
    <div class="toolbar">
      <el-input v-model="query.keyword" placeholder="用户名 / 手机号 / 邮箱" clearable style="width:280px" @keyup.enter="load" />
      <el-button type="primary" @click="load">查询</el-button>
      <el-button type="success" @click="openCreate">添加用户</el-button>
    </div>

    <el-table :data="rows" border>
      <el-table-column prop="userName" label="用户名" min-width="140" />
      <el-table-column prop="phone" label="手机号" width="130" />
      <el-table-column prop="email" label="邮箱" min-width="170" />
      <el-table-column label="角色" min-width="170"><template #default="{ row }">{{ roleText(row) }}</template></el-table-column>
      <el-table-column label="状态" width="90">
        <template #default="{ row }"><el-tag :type="row.isEnabled ? 'success' : 'danger'">{{ row.isEnabled ? '启用' : '禁用' }}</el-tag></template>
      </el-table-column>
      <el-table-column label="操作" width="150" fixed="right">
        <template #default="{ row }">
          <el-button link type="primary" @click="openEdit(row)">修改</el-button>
          <el-button link :type="row.isEnabled ? 'danger' : 'primary'" @click="toggle(row)">{{ row.isEnabled ? '禁用' : '启用' }}</el-button>
        </template>
      </el-table-column>
    </el-table>
    <el-pagination class="pager" v-model:current-page="query.page" :page-size="query.pageSize" :total="total" layout="total, prev, pager, next" @current-change="load" />

    <el-dialog v-model="dialog" :title="form.id ? '修改用户' : '添加用户'" width="620px" @closed="resetForm">
      <el-form ref="formRef" :model="form" :rules="rules" label-width="100px">
        <el-row :gutter="12">
          <el-col :span="12"><el-form-item label="用户名" prop="userName"><el-input v-model="form.userName" /></el-form-item></el-col>
          <el-col :span="12"><el-form-item label="密码" prop="password"><el-input v-model="form.password" show-password :placeholder="form.id ? '留空则不修改密码' : ''" /></el-form-item></el-col>
          <el-col :span="12"><el-form-item label="手机号" prop="phone"><el-input v-model="form.phone" /></el-form-item></el-col>
          <el-col :span="12"><el-form-item label="邮箱" prop="email"><el-input v-model="form.email" /></el-form-item></el-col>
          <el-col :span="12"><el-form-item label="角色" prop="role"><el-select v-model="form.role" style="width:100%"><el-option v-for="role in roleOptions" :key="role.code" :value="role.code" :label="`${role.name}（${scopeText(role.tenantType)}）`" /></el-select></el-form-item></el-col>
          <el-col v-if="Number(selectedRole?.tenantType) === 1" :span="12"><el-form-item label="所属平台" prop="platformId"><el-select v-model="form.platformId" filterable style="width:100%"><el-option v-for="platform in platforms" :key="platform.id" :value="platform.id" :label="platform.platformName" /></el-select></el-form-item></el-col>
          <el-col v-if="Number(selectedRole?.tenantType) === 2" :span="12"><el-form-item label="所属商户" prop="merchantId"><el-select v-model="form.merchantId" filterable style="width:100%"><el-option v-for="merchant in merchants" :key="merchant.id" :value="merchant.id" :label="merchant.merchantName" /></el-select></el-form-item></el-col>
        </el-row>
      </el-form>
      <template #footer><el-button @click="dialog = false">取消</el-button><el-button type="primary" @click="save">保存</el-button></template>
    </el-dialog>
  </el-card>
</template>

<script setup>
import { ElMessage } from 'element-plus'
import { computed, onMounted, reactive, ref } from 'vue'
import request from '@/api/request'
import { EMAIL_PATTERN, PHONE_PATTERN, isStrongPassword, lengthRule, optionalPattern, trimForm } from '@/utils/validators'

const rows = ref([]); const bindings = ref([]); const total = ref(0); const dialog = ref(false)
const roles = ref([]); const platforms = ref([]); const merchants = ref([]); const formRef = ref(null)
const query = reactive({ keyword: '', page: 1, pageSize: 10 })
const emptyForm = () => ({ id: 0, userName: '', password: '', phone: '', email: '', role: 'customer', platformId: '', merchantId: '' })
const form = reactive(emptyForm())
const rules = {
  userName: [lengthRule(3, 64, '用户名')],
  password: [{
    validator: (rule, value, callback) => {
      // 新建必填；编辑留空表示不改密码，但一旦填写就与后端同样要求强度。
      if (!value) return form.id ? callback() : callback(new Error('请输入密码'))
      return isStrongPassword(value) ? callback() : callback(new Error('密码至少8位且包含字母和数字'))
    }, trigger: 'blur'
  }],
  phone: [optionalPattern(PHONE_PATTERN, '手机号格式不正确')],
  email: [optionalPattern(EMAIL_PATTERN, '邮箱格式不正确')],
  role: [{ required: true, message: '请选择角色', trigger: 'change' }],
  platformId: [{ required: true, message: '请选择所属平台', trigger: 'change' }],
  merchantId: [{ required: true, message: '请选择所属商户', trigger: 'change' }]
}

const roleOptions = computed(() => [{ code: 'customer', name: '商城客户', tenantType: 0 }, ...roles.value])
const selectedRole = computed(() => roleOptions.value.find(role => role.code === form.role))
const scopeText = type => ({ 0: '无范围', 1: '平台', 2: '商户' }[type] || '未知')
const roleText = row => {
  const boundRoles = bindings.value.filter(item => String(item.userId) === String(row.id))
  return boundRoles.length ? [...new Set(boundRoles.map(item => item.roleName))].join('、') : '商城客户'
}

const resetForm = () => Object.assign(form, emptyForm())
const openCreate = () => { resetForm(); dialog.value = true }
const openEdit = row => {
  resetForm()
  Object.assign(form, { id: row.id, userName: row.userName, phone: row.phone || '', email: row.email || '', role: row.role || 'customer' })
  const binding = bindings.value.find(item => String(item.userId) === String(row.id))
  if (binding) {
    form.role = binding.roleCode || 'customer'
    form.platformId = binding.platformId || ''
    form.merchantId = binding.merchantId || ''
  }
  dialog.value = true
}

const toggle = async row => { await request.post('/users/UpdateStatus', { id: row.id, isEnabled: !row.isEnabled }); ElMessage.success('状态已更新'); load() }
const save = async () => {
  const valid = await formRef.value?.validate().catch(() => false)
  if (!valid) return ElMessage.warning('请按红色提示修正输入')
  trimForm(form)
  const payload = { ...form, platformId: form.platformId || 0, merchantId: form.merchantId || 0 }
  if (form.id) await request.post('/users/Update', payload)
  else await request.post('/users/Create', payload)
  dialog.value = false; ElMessage.success('已保存'); load()
}

const load = async () => {
  const [userData, bindingData, roleData] = await Promise.all([
    request.get('/users/Users', { params: query }),
    request.get('/permissions/Bindings').catch(() => []),
    request.get('/permissions/Roles').catch(() => [])
  ])
  rows.value = userData.items || []; bindings.value = bindingData || []; roles.value = roleData || []
  total.value = Number(userData.total || 0)
}

onMounted(async () => {
  await load()
  const [platformData, merchantData] = await Promise.all([
    request.get('/platforms/List', { params: { page: 1, pageSize: 100 } }).catch(() => ({ items: [] })),
    request.get('/merchants/List', { params: { page: 1, pageSize: 100 } }).catch(() => ({ items: [] }))
  ])
  platforms.value = platformData.items || []; merchants.value = merchantData.items || []
})
</script>
