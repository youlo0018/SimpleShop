<template>
  <el-card class="page-card">
    <div class="header-row"><h3>{{ form.id ? '编辑商品' : '添加商品' }}</h3><el-button @click="$router.push('/products')">返回列表</el-button></div>

    <el-form ref="formRef" :model="form" :rules="rules" label-width="100px" class="edit-form">
      <el-form-item label="商品名称" prop="name"><el-input v-model="form.name" maxlength="40" show-word-limit /></el-form-item>
      <el-form-item label="分类" prop="categoryId">
        <el-cascader v-model="categoryPath" :options="categoryOptions" :props="{ value: 'id', label: 'label', emitPath: true, checkStrictly: true }" style="width:420px" placeholder="选择分类" @change="selectCategory" />
      </el-form-item>
      <el-form-item label="商户" prop="merchantId">
        <el-select v-model="form.merchantId" filterable style="width:420px" placeholder="选择商户">
          <el-option v-for="merchant in merchants" :key="merchant.id" :value="merchant.id" :label="merchant.merchantName" />
        </el-select>
      </el-form-item>
      <el-form-item label="主图" prop="mainImage">
        <div class="upload-area">
          <el-upload :action="uploadUrl" name="file" accept="image/*" :headers="uploadHeaders" :show-file-list="false" :on-success="onUploadSuccess">
            <img v-if="form.mainImage" :src="absoluteUrl(form.mainImage)" class="main-image-preview">
            <el-button v-else type="primary">上传图片</el-button>
          </el-upload>
          <small class="muted">支持 JPG / PNG / WebP，最大 2MB</small>
        </div>
      </el-form-item>
      <el-form-item label="描述" prop="description">
        <div class="rich-editor">
          <div class="editor-toolbar">
            <button type="button" @click="exec('bold')"><b>B</b></button>
            <button type="button" @click="exec('italic')"><i>I</i></button>
            <button type="button" @click="exec('underline')"><u>U</u></button>
            <button type="button" @click="exec('insertUnorderedList')">列表</button>
            <small class="muted">{{ descriptionLength }}/255</small>
          </div>
          <div ref="editorRef" contenteditable class="editor-body" @input="form.description = $event.target.innerHTML"></div>
        </div>
      </el-form-item>
    </el-form>

    <el-divider content-position="left">销售规格</el-divider>
    <div class="spec-groups">
      <div v-for="(group, index) in specGroups" :key="index" class="spec-group">
        <el-input v-model="group.name" placeholder="规格名，如：颜色" style="width:180px" />
        <el-input v-model="group.values" placeholder="规格值，用逗号分隔：黑色,白色" style="flex:1" @change="generateSkus" />
        <el-button link type="danger" :disabled="specGroups.length <= 1" @click="removeGroup(index)">删除</el-button>
      </div>
      <div class="toolbar"><el-button type="primary" @click="addGroup">添加规格</el-button><el-button @click="generateSkus">重新生成SKU</el-button></div>
      <el-alert v-if="skuError" :title="skuError" type="error" show-icon :closable="false" />
    </div>

    <el-table :data="form.skus" border class="sku-table">
      <el-table-column prop="specName" label="规格组合名" min-width="130" />
      <el-table-column prop="specValue" label="规格值" min-width="150" />
      <el-table-column width="190" label="SKU编码"><template #default="{ row }"><el-input v-model="row.skuCode" placeholder="请输入SKU编码" /></template></el-table-column>
      <el-table-column width="140" label="售价"><template #default="{ row }"><el-input-number v-model="row.price" :min="0.01" :precision="2" controls-position="right" /></template></el-table-column>
      <el-table-column width="150" label="原价"><template #default="{ row }"><el-input-number v-model="row.originalPrice" :min="0" :precision="2" controls-position="right" /></template></el-table-column>
      <el-table-column width="160" label="库存"><template #default="{ row }"><el-input-number v-model="row.stock" :min="1" :precision="0" controls-position="right" /></template></el-table-column>
      <el-table-column width="90" label="操作"><template #default="{ $index }"><el-button link type="danger" @click="removeSku($index)">删除</el-button></template></el-table-column>
    </el-table>

    <div class="footer-actions"><el-button @click="$router.push('/products')">取消</el-button><el-button type="primary" :loading="saving" @click="save">保存</el-button></div>
  </el-card>
</template>

<script setup>
import { ElMessage } from 'element-plus'
import { computed, nextTick, onMounted, reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import request from '@/api/request'
import { maxLengthRule } from '@/utils/validators'

const route = useRoute(); const router = useRouter()
const apiBase = import.meta.env.VITE_API_BASE || 'http://127.0.0.1:5008/gateway'
const uploadUrl = `${apiBase}/products/Upload`
const uploadHeaders = computed(() => ({ Authorization: `Bearer ${localStorage.getItem('admin_token') || ''}` }))
const merchants = ref([]); const categoryTree = ref([]); const categoryPath = ref([]); const editorRef = ref(null); const formRef = ref(null)
const saving = ref(false); const skuError = ref('')
const form = reactive({ id: '0', platformId: 0, merchantId: '', categoryId: '', name: '', mainImage: '', description: '', skus: [] })
const specGroups = reactive([{ name: '', values: '' }])
const rules = {
  name: [{ required: true, message: '请输入商品名称', trigger: 'blur' }, { max: 40, message: '商品名称不能超过40个字符', trigger: 'blur' }],
  categoryId: [{ required: true, message: '请选择分类', trigger: 'change' }],
  merchantId: [{ required: true, message: '请选择商户', trigger: 'change' }],
  mainImage: [{ required: true, message: '请上传商品主图', trigger: 'change' }],
  description: [maxLengthRule(255, '商品描述')]
}
const descriptionLength = computed(() => (form.description || '').length)

const buildCategories = items => items.map(item => ({
  id: String(item.id), label: item.name,
  children: (item.children || []).length ? buildCategories(item.children) : undefined
}))
const categoryOptions = computed(() => buildCategories(categoryTree.value))
const flattenTree = (items, parents = []) => items.flatMap(item => {
  const path = [...parents, { id: item.id, name: item.name }]
  return [{ node: item, path }, ...flattenTree(item.children || [], path)]
})
const absoluteUrl = url => !url || /^https?:/.test(url) ? url : `${apiBase.split('/gateway')[0]}${url}`
const exec = command => document.execCommand(command, false, null)
const selectCategory = path => { form.categoryId = path?.at(-1) || '' }
const onUploadSuccess = response => {
  if (Number(response.code) === 200 && response.data?.url) form.mainImage = response.data.url
  else ElMessage.error(response.message || '上传失败')
}

const addGroup = () => { specGroups.push({ name: '', values: '' }); generateSkus() }
const removeGroup = index => { specGroups.splice(index, 1); generateSkus() }
const removeSku = index => { form.skus.splice(index, 1) }
const parseGroup = group => ({ name: group.name.trim(), values: group.values.split(/[,，]/).map(value => value.trim()).filter(Boolean) })
const generateSkus = () => {
  const groups = specGroups.map(parseGroup).filter(group => group.name && group.values.length)
  if (!groups.length) { form.skus = []; return }
  let combinations = groups[0].values.map(value => [{ name: groups[0].name, value }])
  for (const group of groups.slice(1))
    combinations = combinations.flatMap(prefix => group.values.map(value => [...prefix, { name: group.name, value }]))
  const old = new Map(form.skus.map(sku => [`${sku.specName}/${sku.specValue}`, sku]))
  form.skus = combinations.map(specs => {
    const specName = specs.map(spec => spec.name).join('/')
    const specValue = specs.map(spec => spec.value).join('/')
    const previous = old.get(`${specName}/${specValue}`)
    return previous || {
      skuCode: '', specName, specValue,
      price: 0.01, originalPrice: 0, stock: 10, image: form.mainImage
    }
  })
}

const loadReferences = async () => {
  const [categoryData, merchantData] = await Promise.all([
    request.get('/products/GetCategoryTree'),
    request.get('/merchants/List', { params: { page: 1, pageSize: 100 } }).catch(() => ({ items: [] }))
  ])
  categoryTree.value = categoryData || []; merchants.value = merchantData.items || []
}

const validateSkus = () => {
  if (!form.skus.length) return '请添加规格名和规格值以生成SKU'
  if (form.skus.some(sku => !sku.skuCode?.trim())) return 'SKU编码不能为空'
  if (form.skus.some(sku => sku.skuCode.trim().length > 40)) return 'SKU编码不能超过40个字符'
  if (form.skus.some(sku => !Number.isFinite(Number(sku.price)) || Number(sku.price) <= 0)) return 'SKU售价必须大于0'
  if (form.skus.some(sku => Math.round(Number(sku.price) * 100) !== Number(sku.price) * 100)) return 'SKU售价格式不正确（最多两位小数）'
  if (form.skus.some(sku => Number(sku.originalPrice) > 0 && Number(sku.originalPrice) < Number(sku.price))) return 'SKU原价不能低于售价'
  if (form.skus.some(sku => !Number.isInteger(Number(sku.stock)) || Number(sku.stock) <= 0)) return 'SKU库存必须为大于0的整数'
  if (form.skus.some(sku => (sku.specName || '').length > 40 || (sku.specValue || '').length > 120)) return '规格名/规格值过长'
  const codes = form.skus.map(sku => sku.skuCode.trim())
  if (new Set(codes.map(code => code.toLowerCase())).size !== codes.length) return 'SKU编码不能重复'
  return ''
}

const save = async () => {
  const valid = await formRef.value?.validate().catch(() => false)
  skuError.value = validateSkus()
  if (!valid || skuError.value) return ElMessage.warning('请按红色提示修正输入')
  saving.value = true
  try {
    form.platformId = merchants.value.find(item => String(item.id) === String(form.merchantId))?.platformId || 0
    const payload = { ...form, skus: form.skus.map(sku => ({ ...sku, skuCode: sku.skuCode.trim() })) }
    if (form.id && form.id !== '0') await request.post('/products/Update', payload)
    else await request.post('/products/CreateProduct', payload)
    ElMessage.success('保存成功'); router.push('/products')
  } finally { saving.value = false }
}

onMounted(async () => {
  await loadReferences()
  const productId = route.params.id
  if (productId) {
    const data = await request.get('/products/AdminDetail', { params: { id: productId } })
    Object.assign(form, data.product, { id: String(data.product.id), skus: (data.skus || []).map(sku => ({ ...sku })) })
    const matched = flattenTree(categoryTree.value).find(item => String(item.node.id) === String(form.categoryId))
    categoryPath.value = matched?.path.map(item => String(item.id)) || []
    const names = [...new Set(form.skus.flatMap(sku => (sku.specName || '').split('/')).filter(Boolean))]
    specGroups.splice(0, specGroups.length, ...names.map(name => ({
      name, values: [...new Set(form.skus.flatMap(sku => {
        const index = (sku.specName || '').split('/').indexOf(name)
        return index >= 0 ? [(sku.specValue || '').split('/')[index]] : []
      }).filter(Boolean))].join(',')
    })))
  }
  await nextTick(); editorRef.value.innerHTML = form.description || ''
})
</script>

<style scoped>
.header-row { display: flex; justify-content: space-between; align-items: center; margin-bottom: 14px; }
.rich-editor { width: 100%; border: 1px solid rgba(0, 0, 0, .08); border-radius: 10px; overflow: hidden; }
.editor-toolbar { display: flex; gap: 6px; padding: 7px; background: var(--apple-bg, #f5f5f7); border-bottom: 1px solid rgba(0, 0, 0, .06); }
.editor-toolbar button { cursor: pointer; padding: 4px 10px; border: 0; border-radius: 7px; background: white; transition: background .15s ease; }
.editor-toolbar button:hover { background: #e8e8ed; }
.editor-body { min-height: 180px; max-height: 320px; overflow-y: auto; padding: 12px; outline: none; }
.spec-group { display: flex; align-items: center; gap: 8px; margin-bottom: 10px; }
.sku-table { margin-top: 12px; }
.toolbar { margin: 10px 0; display: flex; gap: 8px; }
.footer-actions { display: flex; justify-content: flex-end; gap: 12px; margin-top: 24px; }
</style>
