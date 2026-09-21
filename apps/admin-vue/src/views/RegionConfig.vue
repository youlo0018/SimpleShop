<template>
  <el-card class="page-card">
    <div class="header-row">
      <div class="title-group">
        <span>地区地址</span>
        <span class="subtitle">小程序收货地址的省/市/区下拉数据</span>
      </div>
      <div class="toolbar">
        <el-select v-if="!platformScoped" v-model="platformId" filterable style="width: 220px">
          <el-option v-for="item in platforms" :key="item.id" :value="item.id" :label="item.platformName" />
        </el-select>
      </div>
    </div>
    <RegionEditor :platform-id="platformId" />
  </el-card>
</template>

<script setup>
import { onMounted, ref } from 'vue'
import request from '@/api/request'
import RegionEditor from '@/components/RegionEditor.vue'
import { currentPlatformId, isPlatformScoped } from '@/utils/tenant'

const platforms = ref([])
const platformId = ref(0)
const platformScoped = isPlatformScoped()

const loadPlatforms = async () => {
  if (platformScoped) { platformId.value = currentPlatformId(); return }
  const data = await request.get('/platforms/List', { params: { page: 1, pageSize: 100 } })
  platforms.value = data.items || []
  if (!platformId.value && platforms.value.length) platformId.value = platforms.value[0].id
}

onMounted(loadPlatforms)
</script>

<style scoped>
.header-row { display: flex; align-items: center; justify-content: space-between; margin-bottom: 12px; }
.title-group { display: flex; align-items: baseline; gap: 10px; font-size: 16px; font-weight: 600; }
.subtitle { color: #909399; font-size: 12px; font-weight: 400; }
</style>
