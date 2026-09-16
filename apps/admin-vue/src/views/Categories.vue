<template>
  <el-card class="page-card">
    <div class="toolbar">
      <el-button type="primary" @click="openCreate()">新增分类</el-button>
      <el-button @click="load">刷新</el-button>
    </div>

    <el-table :data="rows" row-key="id" border :tree-props="{ children: 'children' }">
      <el-table-column prop="name" label="分类名称" min-width="280" />
      <el-table-column prop="sort" label="排序" width="90" />
      <el-table-column label="状态" width="100"><template #default="{ row }">{{ row.isActive === false ? '停用' : '启用' }}</template></el-table-column>
      <el-table-column label="操作" width="230" fixed="right">
        <template #default="{ row }">
          <el-button link type="primary" @click="openCreate(row)">添加子分类</el-button>
          <el-button link type="primary" @click="openEdit(row)">修改</el-button>
          <el-button link :type="row.isActive === false ? 'success' : 'warning'" @click="toggle(row)">{{ row.isActive === false ? '启用' : '禁用' }}</el-button>
          <el-button link type="danger" @click="remove(row)">删除</el-button>
        </template>
      </el-table-column>
    </el-table>

    <el-dialog v-model="dialog" :title="form.id ? '修改分类' : '新增分类'" width="480px" @closed="resetForm">
      <el-form ref="formRef" :model="form" :rules="rules" label-width="90px">
        <el-form-item label="名称" prop="name"><el-input v-model="form.name" maxlength="64" show-word-limit /></el-form-item>
        <el-form-item label="父级分类">
          <el-tree-select
            v-model="form.parentId"
            :data="parentOptions"
            node-key="id"
            check-strictly
            clearable
            placeholder="不选择则为顶层分类"
            :props="{ label: 'label', disabled: 'disabled' }"
            style="width:100%"
          />
        </el-form-item>
        <el-form-item label="排序" prop="sort"><el-input-number v-model="form.sort" :min="0" /></el-form-item>
      </el-form>
      <template #footer><el-button @click="dialog = false">取消</el-button><el-button type="primary" @click="save">保存</el-button></template>
    </el-dialog>
  </el-card>
</template>

<script setup>
import { ElMessage } from 'element-plus'
import { computed, onMounted, reactive, ref } from 'vue'
import request from '@/api/request'
import { integerRule, requiredRule, trimForm } from '@/utils/validators'

const rows = ref([]); const dialog = ref(false); const formRef = ref(null)
const form = reactive({ id: 0, name: '', parentId: '0', sort: 0 })
const rules = {
  name: [requiredRule('请输入分类名称'), { max: 64, message: '分类名称不能超过64个字符', trigger: 'blur' }],
  sort: [integerRule(0, 9999, '排序')]
}
const parentOptions = computed(() => [
  { id: '0', label: '顶层分类', level: 0 },
  ...buildParentOptions(rows.value, [], 1)
])

const buildParentOptions = (items, path, level) => items.map(item => {
  const currentPath = [...path, item.id]
  return {
    id: String(item.id), label: item.name,
    // 一级可选作二级父级，二级可选作三级父级；三级节点不允许继续挂子级。
    disabled: level >= 3,
    children: (item.children || []).length ? buildParentOptions(item.children, currentPath, level + 1) : undefined
  }
})

const resetForm = () => { Object.assign(form, { id: 0, name: '', parentId: '0', sort: 0 }) }
const load = async () => { rows.value = await request.get('/products/GetCategoryTree') }

const findCategoryById = (items, id) => {
  for (const item of items) {
    if (String(item.id) === String(id)) return item
    const child = findCategoryById(item.children || [], id)
    if (child) return child
  }
  return null
}

const getPathById = (items, id, path = []) => {
  for (const item of items) {
    const currentPath = [...path, String(item.id)]
    if (String(item.id) === String(id)) return currentPath
    const result = getPathById(item.children || [], id, currentPath)
    if (result.length) return result
  }
  return []
}

const getParentPathById = (items, id, path = []) => {
  for (const item of items) {
    const currentPath = [...path, String(item.id)]
    const matchedChild = (item.children || []).find(child => String(child.id) === String(id))
    if (matchedChild) return currentPath
    const result = getParentPathById(item.children || [], id, currentPath)
    if (result.length) return result
  }
  return []
}

const openCreate = row => {
  resetForm()
  if (row) {
    form.parentId = String(row.id)
  }
  dialog.value = true
}

const openEdit = row => {
  resetForm()
  Object.assign(form, { id: row.id, name: row.name, parentId: Number(row.parentId || 0) ? String(row.parentId) : '0', sort: Number(row.sort || 0) })
  dialog.value = true
}

const save = async () => {
  const valid = await formRef.value?.validate().catch(() => false)
  if (!valid) return
  trimForm(form)
  const parentId = form.parentId || '0'
  if (form.id) await request.put(`/products/UpdateCategory/${form.id}`, { name: form.name, parentId, sort: form.sort })
  else await request.post('/products/CreateCategory', { name: form.name, parentId, sort: form.sort })
  ElMessage.success('已保存'); dialog.value = false; load()
}

const toggle = async row => {
  await request.post('/products/DisableCategory', { id: row.id, isActive: row.isActive === false }); ElMessage.success('已更新'); load()
}

const remove = async row => {
  await request.delete(`/products/DeleteCategory/${row.id}`); ElMessage.success('已删除'); load()
}

onMounted(load)
</script>
