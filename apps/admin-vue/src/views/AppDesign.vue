<template>
  <el-card class="page-card">
    <template #header>
      <div class="header-row">
        <span>小程序装修</span>
        <div class="toolbar">
          <el-select v-model="platformId" filterable style="width:260px" @change="load">
            <el-option v-for="platform in platforms" :key="platform.id" :value="platform.id" :label="platform.platformName" />
          </el-select>
          <el-button @click="addModule('hero')">加品牌头</el-button>
          <el-button @click="addModule('banners')">加轮播</el-button>
          <el-button @click="addModule('quickNav')">加快捷入口</el-button>
          <el-button @click="addModule('categories')">加分类</el-button>
          <el-button @click="addModule('products')">加商品</el-button>
          <el-button type="primary" :loading="saving" @click="save(true)">保存并发布</el-button>
        </div>
      </div>
    </template>

    <el-row :gutter="20">
      <el-col :span="8">
        <el-card shadow="never">
          <template #header>平台与主题</template>
          <el-form label-width="82px">
            <el-form-item label="商城名称"><el-input v-model="design.home.appName" /></el-form-item>
            <el-form-item label="副标题"><el-input v-model="design.home.slogan" /></el-form-item>
            <el-form-item label="公告"><el-input v-model="design.home.notice" type="textarea" :rows="2" /></el-form-item>
            <el-form-item label="主色"><el-color-picker v-model="design.theme.primary" /></el-form-item>
            <el-form-item label="底部标签色"><el-color-picker v-model="design.theme.tabColor" /></el-form-item>
            <el-form-item label="背景色"><el-color-picker v-model="design.theme.background" /></el-form-item>
            <el-form-item label="首页标签"><el-input v-model="design.tabs.home" /></el-form-item>
            <el-form-item label="分类标签"><el-input v-model="design.tabs.category" /></el-form-item>
            <el-form-item label="购物车标签"><el-input v-model="design.tabs.cart" /></el-form-item>
            <el-form-item label="我的标签"><el-input v-model="design.tabs.profile" /></el-form-item>
          </el-form>
          <el-divider>高级JSON</el-divider>
          <el-input v-model="rawJson" type="textarea" :rows="10" />
          <el-button class="mt12" @click="applyRawJson">应用JSON</el-button>
        </el-card>
      </el-col>

      <el-col :span="16">
        <el-card v-for="(module, index) in design.home.modules" :key="`${module.type}-${index}`" shadow="never" class="module-card">
          <template #header>
            <div class="header-row">
              <span>{{ moduleTitle(module) }}</span>
              <div>
                <el-button :disabled="!index" link @click="move(index, -1)">上移</el-button>
                <el-button :disabled="index === design.home.modules.length - 1" link @click="move(index, 1)">下移</el-button>
                <el-button link type="danger" @click="design.home.modules.splice(index, 1)">删除</el-button>
              </div>
            </div>
          </template>

          <el-form label-width="88px">
            <el-form-item label="模块标题"><el-input v-model="module.title" /></el-form-item>

            <template v-if="module.type === 'hero'">
              <el-form-item label="副标题"><el-input v-model="module.subtitle" /></el-form-item>
              <el-form-item label="背景图"><el-input v-model="module.backgroundImage" placeholder="https://..." /></el-form-item>
            </template>

            <template v-if="module.type === 'notice'"><el-form-item label="内容"><el-input v-model="module.text" /></el-form-item></template>

            <template v-if="module.type === 'banners'">
              <el-form-item v-for="(banner, bannerIndex) in module.items" :key="bannerIndex" :label="`图${bannerIndex + 1}`">
                <div class="line"><el-input v-model="banner.image" placeholder="图片地址" /><el-button link type="danger" @click="module.items.splice(bannerIndex, 1)">删</el-button></div>
                <div class="line"><el-input v-model="banner.title" placeholder="标题" /><el-select v-model="banner.linkType" style="width:130px"><el-option value="products" label="商品列表" /><el-option value="category" label="分类页" /><el-option value="cart" label="购物车" /></el-select><el-input v-model="banner.linkValue" placeholder="参数" /></div>
              </el-form-item>
              <el-button @click="module.items.push({ image: '', title: '', linkType: 'products', linkValue: '' })">添加轮播图</el-button>
            </template>

            <template v-if="module.type === 'quickNav'">
              <el-form-item v-for="(item, itemIndex) in module.items" :key="itemIndex" :label="`入口${itemIndex + 1}`">
                <div class="line"><el-input v-model="item.icon" placeholder="emoji" style="width:72px" /><el-input v-model="item.title" placeholder="名称" /></div>
                <div class="line"><el-select v-model="item.linkType"><el-option value="category" label="分类" /><el-option value="cart" label="购物车" /><el-option value="orders" label="订单" /><el-option value="products" label="商品列表" /><el-option value="address" label="地址" /></el-select><el-input v-model="item.linkValue" placeholder="分类ID" /><el-button link type="danger" @click="module.items.splice(itemIndex, 1)">删</el-button></div>
              </el-form-item>
              <el-button @click="module.items.push({ icon: '🎁', title: '新品', linkType: 'products', linkValue: '' })">添加入口</el-button>
            </template>

            <template v-if="module.type === 'categories'"><el-form-item label="数量"><el-input-number v-model="module.limit" :min="1" :max="20" /></el-form-item></template>

            <template v-if="module.type === 'products'">
              <el-form-item label="布局"><el-radio-group v-model="module.layout"><el-radio-button value="grid">两列</el-radio-button><el-radio-button value="list">横滑</el-radio-button></el-radio-group></el-form-item>
              <el-form-item label="分类ID"><el-input v-model="module.categoryId" placeholder="留空=全部" /></el-form-item>
              <el-form-item label="商户ID"><el-input v-model="module.merchantId" placeholder="留空=全部" /></el-form-item>
              <el-form-item label="数量"><el-input-number v-model="module.limit" :min="1" :max="30" /></el-form-item>
            </template>
          </el-form>
        </el-card>
      </el-col>
    </el-row>
  </el-card>
</template>

<script setup>
import { ElMessage, ElMessageBox } from 'element-plus'
import { onMounted, ref } from 'vue'
import request from '@/api/request'
import { isHexColor } from '@/utils/validators'

const platforms = ref([]); const platformId = ref(''); const saving = ref(false)
const rawJson = ref('')
const design = ref(emptyDesign())

function emptyDesign() {
  return {
    theme: { primary: '#0071e3', background: '#f5f5f7', tabColor: '#0071e3' },
    home: { appName: '', slogan: '', notice: '', banners: [], modules: [] },
    tabs: { home: '首页', category: '分类', cart: '购物车', profile: '我的' }
  }
}

const moduleTitle = module => ({ hero: '品牌头', notice: '公告', banners: '轮播图', quickNav: '快捷入口', categories: '分类', products: '商品' }[module.type] || module.type)
const makeModule = type => ({
  hero: { type, title: '', subtitle: '', backgroundImage: '' },
  notice: { type, title: '', text: '' },
  banners: { type, title: '', items: [] },
  quickNav: { type, title: '快捷入口', items: [] },
  categories: { type, title: '精选分类', limit: 8 },
  products: { type, key: `section-${Date.now()}`, title: '为你推荐', layout: 'grid', categoryId: '', merchantId: '', limit: 10 }
}[type])
const addModule = type => design.value.home.modules.push(makeModule(type))
const move = (index, offset) => {
  const modules = design.value.home.modules
  const target = index + offset
  if (target < 0 || target >= modules.length) return
  [modules[index], modules[target]] = [modules[target], modules[index]]
}

const loadPlatforms = async () => {
  const data = await request.get('/platforms/List', { params: { page: 1, pageSize: 100 } })
  platforms.value = data.items || []
  if (!platformId.value && platforms.value.length) platformId.value = platforms.value[0].id
}

const load = async () => {
  if (!platformId.value) return
  const data = await request.get('/platform-configs/Admin', { params: { platformId: platformId.value } })
  design.value = { ...emptyDesign(), ...(data.design || {}) }
  design.value.theme = { ...emptyDesign().theme, ...(data.design?.theme || {}) }
  design.value.home = { ...emptyDesign().home, ...(data.design?.home || {}) }
  design.value.tabs = { ...emptyDesign().tabs, ...(data.design?.tabs || {}) }
  design.value.home.modules = data.design?.home?.modules || [makeModule('quickNav'), makeModule('categories'), makeModule('products')]
  rawJson.value = JSON.stringify(design.value, null, 2)
}

const applyRawJson = () => {
  try {
    const parsed = JSON.parse(rawJson.value)
    // 结构校验：缺 theme/home/tabs 会让小程序端渲染报错，而不是在此静默通过。
    if (!parsed?.theme || !parsed?.home || !parsed?.tabs) throw new Error('missing sections')
    design.value = parsed
    ElMessage.success('JSON已应用')
  }
  catch { ElMessage.error('JSON格式不正确或缺少 theme/home/tabs 结构') }
}

// 装修配置强校验：前端拦截必填/长度/颜色/链接参数，后端只兜底 JSON 语法与大小。
const validateDesign = () => {
  const home = design.value.home || {}
  const tabs = design.value.tabs || {}
  const theme = design.value.theme || {}
  if (!String(home.appName || '').trim()) return '请填写商城名称'
  if (String(home.appName).length > 20) return '商城名称不能超过20个字符'
  if (String(home.slogan || '').length > 40) return '副标题不能超过40个字符'
  if (String(home.notice || '').length > 200) return '公告不能超过200个字符'
  if (![theme.primary, theme.background, theme.tabColor].every(isHexColor)) return '主题颜色必须是 #RRGGBB 格式'
  if (['home', 'category', 'cart', 'profile'].some(key => !String(tabs[key] || '').trim() || String(tabs[key]).length > 8))
    return '底部标签文案必填且不超过8个字符'
  for (const module of home.modules || []) {
    if (module.type === 'banners') {
      if (!(module.items || []).length) return '轮播图模块至少添加一张图片'
      if (module.items.some(item => !String(item.image || '').trim())) return '轮播图图片地址不能为空'
      if (module.items.some(item => item.linkType === 'category' && !String(item.linkValue || '').trim())) return '轮播图选择分类页时必须填写分类参数'
    }
    if (module.type === 'quickNav' && (module.items || []).some(item => !String(item.title || '').trim())) return '快捷入口名称不能为空'
    if (module.type === 'quickNav' && (module.items || []).some(item => item.linkType === 'category' && !String(item.linkValue || '').trim())) return '快捷入口选择分类时必须填写分类ID'
    if (module.type === 'categories' && !(Number(module.limit) >= 1 && Number(module.limit) <= 20)) return '分类模块数量必须为1-20'
    if (module.type === 'products' && !(Number(module.limit) >= 1 && Number(module.limit) <= 30)) return '商品模块数量必须为1-30'
  }
  return ''
}

const save = async publish => {
  if (!platformId.value) return ElMessage.warning('请先选择平台')
  const error = validateDesign()
  if (error) return ElMessage.warning(error)
  if (publish) {
    const confirmed = await ElMessageBox.confirm('发布后小程序端将立即生效，确认发布？', '发布确认', { type: 'warning' }).then(() => true).catch(() => false)
    if (!confirmed) return
  }
  saving.value = true
  try {
    await request.post('/platform-configs/Save', { platformId: platformId.value, configJson: JSON.stringify(design.value), publish })
    ElMessage.success(publish ? '已发布' : '已保存'); load()
  } finally { saving.value = false }
}

onMounted(async () => { await loadPlatforms(); load() })
</script>

<style scoped>
.header-row { display: flex; align-items: center; justify-content: space-between; gap: 12px; }
.toolbar { display: flex; flex-wrap: wrap; align-items: center; }
.module-card { margin-bottom: 16px; }
.line { display: flex; align-items: center; gap: 8px; margin-bottom: 8px; width: 100%; }
.mt12 { margin-top: 12px; }
</style>
