<template>
  <el-card class="page-card">
    <template #header>
      <div class="header-row">
        <span>经营概览</span>
        <el-radio-group v-model="granularity" @change="load">
          <el-radio-button value="day">按日</el-radio-button>
          <el-radio-button value="month">按月</el-radio-button>
        </el-radio-group>
      </div>
    </template>

    <el-row :gutter="16">
      <el-col v-for="card in metrics" :key="card.key" :span="6">
        <el-card shadow="hover" class="metric-card" :class="{ active: activeMetric === card.key }" @click="activeMetric = card.key">
          <p class="metric-label">{{ card.label }}</p>
          <h2>{{ formatValue(card.key, summary[card.key]) }}</h2>
        </el-card>
      </el-col>
    </el-row>

    <el-card class="chart-card" shadow="never">
      <div class="header-row chart-header">
        <b>{{ metricName }}{{ granularity === 'month' ? '月度趋势' : '近30日趋势' }}</b>
      </div>

      <div class="legend">
        <span><i class="dot actual"></i>实际值</span>
        <span><i class="dot average"></i>移动平均</span>
      </div>

      <svg viewBox="0 0 760 320" class="trend-chart">
        <defs>
          <linearGradient id="area-fill" x1="0" x2="0" y1="0" y2="1">
            <stop offset="0%" stop-color="#0071e3" stop-opacity=".16" />
            <stop offset="100%" stop-color="#0071e3" stop-opacity="0" />
          </linearGradient>
        </defs>
        <g>
          <line v-for="tick in yAxisTicks" :key="`grid-${tick.y}`" x1="58" :y1="tick.y" x2="730" :y2="tick.y" stroke="rgba(0,0,0,.05)" />
          <line x1="58" y1="20" x2="58" y2="260" stroke="rgba(0,0,0,.1)" />
          <line x1="58" y1="260" x2="730" y2="260" stroke="rgba(0,0,0,.1)" />
          <text v-for="(tick, index) in yAxisTicks" :key="`y-${index}`" x="50" :y="tick.y + 4" text-anchor="end" class="axis-text">{{ tick.text }}</text>
          <text v-for="tick in xAxisTicks" :key="`x-${tick.x}`" :x="tick.x" y="282" text-anchor="middle" class="axis-text">{{ tick.text }}</text>
        </g>
        <path :d="areaPath" fill="url(#area-fill)" />
        <path :d="actualPath" fill="none" stroke="#0071e3" stroke-width="2.5" stroke-linejoin="round" />
        <path :d="movingAveragePath" fill="none" stroke="#34c759" stroke-width="2" stroke-dasharray="4 5" />
        <circle v-for="(point, index) in chartPoints" :key="index" :cx="point.x" :cy="point.y" r="4" fill="#fff" stroke="#0071e3" stroke-width="2">
          <title>{{ trend[index]?.date }}：{{ formatValue(activeMetric, trend[index]?.[activeMetric]) }}</title>
        </circle>
      </svg>
    </el-card>
  </el-card>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue'
import request from '@/api/request'

const granularity = ref('day')
const activeMetric = ref('dailyActiveUsers')
const summary = ref({}); const trend = ref([])
const metrics = [
  { key: 'dailyActiveUsers', label: '当日活跃用户' },
  { key: 'dailyOrders', label: '当日订单数' },
  { key: 'gmv', label: '当日GMV' },
  { key: 'soldItems', label: '当日销售商品数' }
]
const parseNumber = value => Number(value || 0)
const formatValue = (key, value) => key === 'gmv' ? `¥${parseNumber(value).toFixed(2)}` : String(Math.round(parseNumber(value)))
const metricName = computed(() => metrics.find(item => item.key === activeMetric.value)?.label || '')
const values = computed(() => trend.value.map(item => parseNumber(item[activeMetric.value])))

function niceScale(maxValue) {
  const target = Math.max(maxValue, 1) * 1.08
  const magnitude = 10 ** Math.floor(Math.log10(target))
  const normalized = target / magnitude
  const step = normalized <= 1 ? 1 : normalized <= 2 ? 2 : normalized <= 5 ? 5 : 10
  return step * magnitude
}
const maxValue = computed(() => niceScale(Math.max(...values.value, 1)))
const yAxisTicks = computed(() => [1, .75, .5, .25, 0].map(ratio => ({
  y: 260 - ratio * 220, text: formatValue(activeMetric.value, maxValue.value * ratio)
})))
const chartPoints = computed(() => {
  const count = values.value.length
  const width = count > 1 ? 672 : 0
  return values.value.map((value, index) => ({
    x: 58 + index * (width / Math.max(count - 1, 1)),
    y: 260 - (Math.min(value, maxValue.value) / maxValue.value) * 220
  }))
})
const xAxisTicks = computed(() => {
  const count = trend.value.length; if (!count) return []
  return [0, .2, .4, .6, .8, 1].map(ratio => ({
    x: 58 + ratio * 672,
    text: trend.value[Math.min(Math.round(ratio * (count - 1)), count - 1)]?.date || ''
  }))
})
const pathOf = points => points.length ? `M ${points.map(point => `${point.x} ${point.y}`).join(' L ')}` : ''
const actualPath = computed(() => pathOf(chartPoints.value))
const areaPath = computed(() => chartPoints.value.length ? `${actualPath.value} L ${chartPoints.value.at(-1).x} 260 L ${chartPoints.value[0].x} 260 Z` : '')

const movingAverages = computed(() => values.value.map((_, index) => {
  const window = values.value.slice(Math.max(0, index - 2), index + 1)
  return window.reduce((sum, value) => sum + value, 0) / window.length
}))
const movingAveragePath = computed(() => pathOf(movingAverages.value.map((value, index) => ({
  x: chartPoints.value[index]?.x, y: 260 - (Math.min(value, maxValue.value) / maxValue.value) * 220
})).filter(point => point.x !== undefined)))

const load = async () => {
  const data = await request.get('/reports/Report', { params: { granularity: granularity.value } })
  summary.value = data.summary || {}; trend.value = data.trend || []
}
onMounted(load)
</script>

<style scoped>
.header-row { display: flex; align-items: center; justify-content: space-between; gap: 12px; }
.metric-card { cursor: pointer; margin-bottom: 18px; border-radius: 14px; transition: transform .18s ease, box-shadow .18s ease; }
.metric-card:hover { transform: translateY(-2px); box-shadow: 0 6px 20px rgba(0, 0, 0, .08); }
.metric-card.active { box-shadow: 0 0 0 2.5px rgba(0, 113, 227, .5), 0 6px 20px rgba(0, 113, 227, .1); }
.metric-label { color: var(--apple-text-2, #6e6e73); font-size: 13px; margin: 0; }
.metric-card h2 { color: var(--apple-text, #1d1d1f); margin: 8px 0 0; font-size: 26px; font-weight: 700; letter-spacing: -.02em; font-variant-numeric: tabular-nums; }
.chart-card { margin-top: 10px; } .chart-header { margin-bottom: 8px; }
.legend { display: flex; gap: 18px; color: var(--apple-text-2, #6e6e73); font-size: 12px; margin: 6px 0 8px; }
.legend span { display: inline-flex; align-items: center; gap: 5px; }
.dot { width: 14px; height: 3px; display: inline-block; } .dot.actual { background: #0071e3; } .dot.average { background: #34c759; }
.trend-chart { width: 100%; height: 340px; } .axis-text { fill: #86868b; font-size: 11px; }
</style>
