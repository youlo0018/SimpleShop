<template>
  <div class="region-editor">
    <div class="editor-head">
      <el-tag :type="isCustom ? 'success' : 'info'" size="small">{{ isCustom ? '当前：平台自定义' : '当前：内置默认全国数据' }}</el-tag>
      <span class="stats">省 <b>{{ stats.provinces }}</b> · 市 <b>{{ stats.cities }}</b> · 区县 <b>{{ stats.districts }}</b></span>
      <span class="spacer" />
      <el-button size="small" :loading="loading" @click="load">刷新</el-button>
      <el-button size="small" type="primary" :loading="saving" @click="save">保存自定义</el-button>
      <el-button size="small" type="danger" plain @click="reset">恢复默认</el-button>
    </div>

    <div class="editor-row">
      <el-cascader
        v-model="selectedPath"
        :options="options"
        :props="cascaderProps"
        placeholder="选择省/市/区（选中后可增删）"
        clearable
        style="width: 320px"
      />
      <el-input v-model="addName" maxlength="32" :placeholder="`新增${levelText}名称`" style="width: 200px" @keyup.enter="add" />
      <el-button type="primary" plain :disabled="levelText === '区县'" @click="add">添加到{{ levelText }}</el-button>
      <el-button type="danger" plain :disabled="!selectedPath.length" @click="remove">删除选中</el-button>
    </div>
    <div class="hint">默认使用内置全国数据；不选→新增省，选省→新增市，选到市→新增区县；修改后点「保存自定义」，小程序地址下拉即改用该数据。</div>
  </div>
</template>

<script setup>
import { ElMessage, ElMessageBox } from 'element-plus'
import { computed, ref, watch } from 'vue'
import request from '@/api/request'

const props = defineProps({
  platformId: { type: [String, Number], default: '' }
})

const regions = ref([])
const isCustom = ref(false)
const loading = ref(false)
const saving = ref(false)
const selectedPath = ref([])
const addName = ref('')

const cascaderProps = { checkStrictly: true, emitPath: true, expandTrigger: 'click' }

const toOptions = (items = []) => items.map((item, index) => ({
  value: index,
  label: item.name,
  children: item.children?.length ? toOptions(item.children) : undefined
}))
const options = computed(() => toOptions(regions.value))
const stats = computed(() => {
  let cities = 0; let districts = 0
  regions.value.forEach(province => (province.children || []).forEach(city => { cities += 1; districts += (city.children || []).length }))
  return { provinces: regions.value.length, cities, districts }
})
const levelText = computed(() => {
  if (!selectedPath.value.length) return '省'
  if (selectedPath.value.length === 1) return '市'
  return '区县'
})

const load = async () => {
  if (!props.platformId) return
  loading.value = true
  try {
    const data = await request.get('/platform-configs/Regions', { params: { platformId: props.platformId } })
    isCustom.value = Boolean(data?.isCustom)
    regions.value = data?.regions || []
    selectedPath.value = []
  } finally { loading.value = false }
}

const add = () => {
  const name = addName.value.trim()
  if (!name) return ElMessage.warning('请输入名称')
  const level = levelText.value
  const [p, c] = selectedPath.value
  if (p === undefined) {
    regions.value.push({ name, children: [] })
  } else if (c === undefined) {
    const province = regions.value[p]
    province.children = province.children || []
    province.children.push({ name, children: [] })
  } else {
    const city = regions.value[p].children[c]
    city.children = city.children || []
    city.children.push({ name })
  }
  addName.value = ''
  selectedPath.value = []
  ElMessage.success(`已添加${level}`)
}

const remove = async () => {
  const [p, c, d] = selectedPath.value
  if (p === undefined) return ElMessage.warning('请先选择要删除的地区')
  const target = d !== undefined ? regions.value[p].children[c].children[d].name : c !== undefined ? regions.value[p].children[c].name : regions.value[p].name
  const confirmed = await ElMessageBox.confirm(`确认删除「${target}」及其下级地区？`, '删除地区', { type: 'warning' }).then(() => true).catch(() => false)
  if (!confirmed) return
  if (d !== undefined) regions.value[p].children[c].children.splice(d, 1)
  else if (c !== undefined) regions.value[p].children.splice(c, 1)
  else regions.value.splice(p, 1)
  selectedPath.value = []
}

const plainRegions = list => list.map(item => ({ name: item.name, children: plainRegions(item.children || []) }))

const save = async () => {
  if (!props.platformId) return ElMessage.warning('请先选择平台')
  if (!regions.value.length) return ElMessage.error('至少保留一个省份，或点「恢复默认」')
  saving.value = true
  try {
    await request.post('/platform-configs/SaveRegions', { platformId: props.platformId, regionsJson: JSON.stringify(plainRegions(regions.value)) })
    ElMessage.success('已保存自定义地区数据')
    await load()
  } finally { saving.value = false }
}

const reset = async () => {
  const confirmed = await ElMessageBox.confirm('恢复默认将清除该平台自定义地区数据，确认？', '恢复默认', { type: 'warning' }).then(() => true).catch(() => false)
  if (!confirmed) return
  await request.post('/platform-configs/SaveRegions', { platformId: props.platformId, regionsJson: '' })
  ElMessage.success('已恢复内置默认数据')
  await load()
}

watch(() => props.platformId, load, { immediate: true })
</script>

<style scoped>
.editor-head { display: flex; align-items: center; gap: 12px; margin-bottom: 12px; }
.stats { color: #606266; font-size: 12px; }
.stats b { color: #303133; }
.spacer { flex: 1; }
.editor-row { display: flex; align-items: center; gap: 8px; margin-bottom: 8px; }
.hint { color: #909399; font-size: 12px; }
</style>
