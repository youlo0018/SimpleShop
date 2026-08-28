<template>
  <el-card class="page-card">
    <div class="toolbar">
      <span class="muted">角色管理</span>
      <div>
        <el-button v-if="isSuperAdmin" type="primary" @click="permissionDialog = true">新增权限</el-button>
        <el-button type="success" @click="openCreateRole">新增角色</el-button>
      </div>
    </div>

    <el-table :data="roles" border>
      <el-table-column prop="code" label="角色编码" min-width="180" />
      <el-table-column prop="name" label="角色名称" min-width="150" />
      <el-table-column label="范围" width="100"><template #default="{ row }">{{ scopeText(row.tenantType) }}</template></el-table-column>
      <el-table-column prop="description" label="说明" min-width="220" />
      <el-table-column label="系统角色" width="95"><template #default="{ row }">{{ row.isSystem ? '是' : '否' }}</template></el-table-column>
      <el-table-column label="操作" width="110" fixed="right"><template #default="{ row }"><el-button link type="primary" @click="openEditRole(row)">编辑权限</el-button></template></el-table-column>
    </el-table>

    <el-dialog v-model="roleDialog" :title="roleForm.id ? '编辑角色权限' : '新增角色'" width="680px" @closed="resetRoleForm">
      <el-form ref="roleFormRef" :model="roleForm" :rules="roleRules" label-width="90px">
        <el-row :gutter="12">
          <el-col :span="12"><el-form-item label="编码" prop="code"><el-input v-model="roleForm.code" :disabled="!!roleForm.id" /></el-form-item></el-col>
          <el-col :span="12"><el-form-item label="名称" prop="name"><el-input v-model="roleForm.name" /></el-form-item></el-col>
        </el-row>
        <el-form-item label="范围"><el-radio-group v-model="roleForm.tenantType" :disabled="!!roleForm.id"><el-radio-button :value="1">平台</el-radio-button><el-radio-button :value="2">商户</el-radio-button></el-radio-group></el-form-item>
        <el-form-item label="说明"><el-input v-model="roleForm.description" /></el-form-item>
        <el-form-item label="全部权限"><el-checkbox v-model="allSelected" @change="toggleAll">选择当前范围内全部权限</el-checkbox></el-form-item>
        <el-form-item label="权限树"><el-tree ref="permissionTreeRef" :data="permissionTree" node-key="value" show-checkbox default-expand-all style="width:100%" @check="syncAllState" /></el-form-item>
      </el-form>
      <template #footer><el-button @click="roleDialog = false">取消</el-button><el-button type="primary" @click="saveRole">保存</el-button></template>
    </el-dialog>

    <el-dialog v-model="permissionDialog" title="新增权限" width="580px" @closed="resetPermissionForm">
      <el-form ref="permissionFormRef" :model="permissionForm" :rules="permissionRules" label-width="90px">
        <el-form-item label="编码" prop="code"><el-input v-model="permissionForm.code" placeholder="例如 product:export" /></el-form-item>
        <el-form-item label="名称" prop="name"><el-input v-model="permissionForm.name" /></el-form-item>
        <el-form-item label="层面" prop="allowedScopes"><el-select v-model="permissionForm.allowedScopes" style="width:100%"><el-option :value="1" label="平台" /><el-option :value="2" label="商户" /><el-option :value="3" label="平台 + 商户" /></el-select></el-form-item>
        <el-form-item label="资源" prop="resource"><el-input v-model="permissionForm.resource" placeholder="product / order / merchant" /></el-form-item>
        <el-form-item label="动作" prop="action"><el-input v-model="permissionForm.action" placeholder="read / export / approve" /></el-form-item>
        <el-form-item label="接口路径" prop="interfacePath"><el-input v-model="permissionForm.interfacePath" placeholder="/gateway/products/CreateProduct" /></el-form-item>
      </el-form>
      <template #footer><el-button @click="permissionDialog = false">取消</el-button><el-button type="primary" @click="createPermission">保存</el-button></template>
    </el-dialog>
  </el-card>
</template>

<script setup>
import { ElMessage } from 'element-plus'
import { computed, nextTick, onMounted, reactive, ref } from 'vue'
import request from '@/api/request'

const permissions = ref([]); const roles = ref([])
const roleDialog = ref(false); const permissionDialog = ref(false)
const permissionTreeRef = ref(null); const roleFormRef = ref(null); const permissionFormRef = ref(null)
const allSelected = ref(false)
const roleForm = reactive({ id: 0, code: '', name: '', tenantType: 1, description: '', permissions: [] })
const permissionForm = reactive({ code: '', name: '', resource: '', action: '', interfacePath: '', allowedScopes: 3 })
const roleRules = {
  code: [{ required: true, pattern: /^[a-z][a-z0-9-]{2,79}$/, message: '3-80位小写字母开头编码', trigger: 'blur' }],
  name: [{ required: true, min: 2, max: 64, message: '角色名称必须为2-64个字符', trigger: 'blur' }]
}
const permissionRules = {
  code: [{ required: true, pattern: /^[a-z][a-z0-9:-]{2,79}$/, message: '3-80位权限编码', trigger: 'blur' }],
  name: [{ required: true, min: 2, max: 64, message: '权限名称必须为2-64个字符', trigger: 'blur' }],
  allowedScopes: [{ required: true, message: '请选择权限层面', trigger: 'change' }],
  resource: [{ required: true, max: 40, message: '请输入资源', trigger: 'blur' }],
  action: [{ required: true, max: 40, message: '请输入动作', trigger: 'blur' }],
  interfacePath: [{ required: true, pattern: /^\/gateway\//, message: '必须以 /gateway/ 开头', trigger: 'blur' }]
}

const isSuperAdmin = computed(() => {
  const user = JSON.parse(localStorage.getItem('admin_user') || 'null')
  return !!user && ((user.permissions || []).includes('*') || (user.roles || []).includes('platform-admin'))
})
const scopeText = type => Number(type) === 1 ? '平台' : '商户'
const visiblePermissions = computed(() => permissions.value.filter(item => (Number(item.allowedScopes) || 3) & roleForm.tenantType))
const permissionTree = computed(() => [1, 2].filter(scope => scope === roleForm.tenantType).map(scope => ({
  label: scope === 1 ? '平台权限' : '商户权限', value: `layer:${scope}`,
  children: buildGroups(scope)
})))

function buildGroups(scope) {
  const groups = new Map()
  for (const item of visiblePermissions.value.filter(permission => (Number(permission.allowedScopes) || 3) & scope)) {
    const resource = item.resource || '通用'
    const groupKey = `group:${scope}:${resource}`
    if (!groups.has(groupKey)) groups.set(groupKey, { label: resource, value: groupKey, children: [] })
    groups.get(groupKey).children.push({ label: `${item.name}（${item.interfacePath || '页面'}）`, value: `${scope}:${item.code}` })
  }
  return [...groups.values()]
}

const leafKeys = () => {
  const keys = []
  const walk = nodes => nodes.forEach(node => node.children?.length ? walk(node.children) : keys.push(node.value))
  walk(permissionTree.value); return keys
}
const toggleAll = selected => { permissionTreeRef.value?.setCheckedKeys(selected ? leafKeys() : []); allSelected.value = selected }
const syncAllState = () => {
  const selected = permissionTreeRef.value?.getCheckedKeys(true) || []
  allSelected.value = leafKeys().length > 0 && leafKeys().every(key => selected.includes(key))
}

const resetRoleForm = () => Object.assign(roleForm, { id: 0, code: '', name: '', tenantType: 1, description: '', permissions: [] })
const resetPermissionForm = () => Object.assign(permissionForm, { code: '', name: '', resource: '', action: '', interfacePath: '', allowedScopes: 3 })
const openCreateRole = () => { resetRoleForm(); roleDialog.value = true }
const openEditRole = async row => {
  resetRoleForm()
  Object.assign(roleForm, {
    ...row,
    id: row.id,
    tenantType: Number(row.tenantType || 1),
    permissions: [...row.permissions || []]
  })
  roleDialog.value = true
  await nextTick()
  const codes = roleForm.permissions.filter(code => String(code).includes(':'))
  const keys = leafKeys().filter(key => codes.includes(key.split(':').slice(1).join(':')))
  permissionTreeRef.value?.setCheckedKeys(codes.includes('*') ? leafKeys() : keys)
  syncAllState()
}

const saveRole = async () => {
  const valid = await roleFormRef.value?.validate().catch(() => false)
  if (!valid) return ElMessage.warning('请按红色提示修正输入')
  const selectedKeys = permissionTreeRef.value?.getCheckedKeys(false) || []
  roleForm.permissions = [...new Set(selectedKeys.map(String).filter(value => /^\d:/.test(value)).map(value => value.split(':').slice(1).join(':')))]
  if (formEditable.value && roleForm.id) await request.put(`/permissions/Roles/${roleForm.id}/Permissions`, roleForm)
  else if (formEditable.value) await request.post('/permissions/CreateRole', roleForm)
  ElMessage.success('已保存'); roleDialog.value = false; load()
}
const formEditable = computed(() => true)

const createPermission = async () => {
  const valid = await permissionFormRef.value?.validate().catch(() => false)
  if (!valid) return ElMessage.warning('请按红色提示修正输入')
  await request.post('/permissions/CreatePermission', permissionForm)
  ElMessage.success('权限已创建'); permissionDialog.value = false; load()
}

const load = async () => {
  const [permissionData, roleData] = await Promise.all([request.get('/permissions/Permissions'), request.get('/permissions/Roles')])
  permissions.value = permissionData || []; roles.value = roleData || []
}
onMounted(load)
</script>
