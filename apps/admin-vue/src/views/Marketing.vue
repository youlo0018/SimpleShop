<template>
  <el-card class="page-card">
    <el-tabs v-model="tab">
      <!-- ============ 活动管理 ============ -->
      <el-tab-pane label="活动管理" name="activities">
        <div class="toolbar">
          <el-select v-if="canChoosePlatform" v-model="platformId" filterable style="width:220px" placeholder="选择平台" @change="loadActivities">
            <el-option v-for="item in platforms" :key="item.id" :value="item.id" :label="item.platformName" />
          </el-select>
          <el-input v-model="activityQuery.keyword" placeholder="活动名称" clearable style="width:200px" @keyup.enter="loadActivities" />
          <el-select v-model="activityQuery.activityType" placeholder="类型" clearable style="width:130px">
            <el-option :value="1" label="满减" /><el-option :value="2" label="满折" /><el-option :value="3" label="满赠" />
          </el-select>
          <el-button type="primary" @click="loadActivities">查询</el-button>
          <el-button v-if="editable" type="success" @click="openActivity()">新建活动</el-button>
        </div>
        <el-table :data="activities" border>
          <el-table-column prop="name" label="活动名称" min-width="160" />
          <el-table-column label="类型" width="90"><template #default="{ row }">{{ activityTypeText[row.activityType] }}</template></el-table-column>
          <el-table-column label="门槛/优惠" min-width="150"><template #default="{ row }">{{ activityBenefit(row) }}</template></el-table-column>
          <el-table-column label="范围" min-width="120"><template #default="{ row }">{{ scopeText(row.scopeType, row.merchantId) }}</template></el-table-column>
          <el-table-column label="有效期" min-width="210"><template #default="{ row }">{{ row.startAt?.slice(0, 10) }} ~ {{ row.endAt ? row.endAt.slice(0, 10) : '长期' }}</template></el-table-column>
          <el-table-column label="启用" width="80"><template #default="{ row }"><el-switch :model-value="row.isEnabled" :disabled="!editable" @change="toggleActivity(row)" /></template></el-table-column>
          <el-table-column label="操作" width="110" fixed="right"><template #default="{ row }"><el-button v-if="editable" link type="primary" @click="openActivity(row)">编辑</el-button></template></el-table-column>
        </el-table>
        <el-pagination class="pager" v-model:current-page="activityQuery.page" :page-size="activityQuery.pageSize" :total="activityTotal" layout="total, prev, pager, next" @current-change="loadActivities" />
      </el-tab-pane>

      <!-- ============ 券模板 ============ -->
      <el-tab-pane label="券模板" name="templates">
        <div class="toolbar">
          <el-select v-if="canChoosePlatform" v-model="platformId" filterable style="width:220px" placeholder="选择平台" @change="loadTemplates">
            <el-option v-for="item in platforms" :key="item.id" :value="item.id" :label="item.platformName" />
          </el-select>
          <el-input v-model="templateQuery.keyword" placeholder="模板名称" clearable style="width:200px" @keyup.enter="loadTemplates" />
          <el-button type="primary" @click="loadTemplates">查询</el-button>
          <el-button v-if="editable" type="success" @click="openTemplate()">新建券模板</el-button>
        </div>
        <el-table :data="templates" border>
          <el-table-column prop="name" label="模板名称" min-width="160" />
          <el-table-column label="类型" width="90"><template #default="{ row }">{{ couponTypeText[row.couponType] }}</template></el-table-column>
          <el-table-column label="门槛/优惠" min-width="150"><template #default="{ row }">{{ couponBenefit(row) }}</template></el-table-column>
          <el-table-column label="归属" min-width="130"><template #default="{ row }">{{ row.merchantId > 0 ? '商户券' : '平台券' }}</template></el-table-column>
          <el-table-column label="有效天数" width="90"><template #default="{ row }">{{ row.validDays }} 天</template></el-table-column>
          <el-table-column label="启用" width="80"><template #default="{ row }"><el-switch :model-value="row.isEnabled" :disabled="!editable" @change="toggleTemplate(row)" /></template></el-table-column>
          <el-table-column label="操作" width="110" fixed="right"><template #default="{ row }"><el-button v-if="editable" link type="primary" @click="openTemplate(row)">编辑</el-button></template></el-table-column>
        </el-table>
        <el-pagination class="pager" v-model:current-page="templateQuery.page" :page-size="templateQuery.pageSize" :total="templateTotal" layout="total, prev, pager, next" @current-change="loadTemplates" />
      </el-tab-pane>

      <!-- ============ 券活动 ============ -->
      <el-tab-pane label="券活动" name="couponActivities">
        <div class="toolbar">
          <el-select v-if="canChoosePlatform" v-model="platformId" filterable style="width:220px" placeholder="选择平台" @change="loadCouponActivities">
            <el-option v-for="item in platforms" :key="item.id" :value="item.id" :label="item.platformName" />
          </el-select>
          <el-input v-model="couponActivityQuery.keyword" placeholder="券活动名称" clearable style="width:200px" @keyup.enter="loadCouponActivities" />
          <el-button type="primary" @click="loadCouponActivities">查询</el-button>
          <el-button v-if="editable" type="success" @click="openCouponActivity()">新建券活动</el-button>
        </div>
        <el-table :data="couponActivities" border>
          <el-table-column prop="name" label="券活动" min-width="150" />
          <el-table-column prop="templateName" label="券模板" min-width="140" />
          <el-table-column label="门槛/优惠" min-width="140"><template #default="{ row }">{{ couponBenefit(row) }}</template></el-table-column>
          <el-table-column label="范围" min-width="110"><template #default="{ row }">{{ scopeText(row.scopeType, row.merchantId) }}</template></el-table-column>
          <el-table-column label="发行/已发" width="110"><template #default="{ row }">{{ row.issuedCount }} / {{ row.totalStock }}</template></el-table-column>
          <el-table-column label="限领" width="80"><template #default="{ row }">{{ row.perUserLimit }} 张</template></el-table-column>
          <el-table-column label="可领取" width="80"><template #default="{ row }">{{ row.isClaimable ? '是' : '否' }}</template></el-table-column>
          <el-table-column label="启用" width="80"><template #default="{ row }"><el-switch :model-value="row.isEnabled" :disabled="!editable" @change="toggleCouponActivity(row)" /></template></el-table-column>
          <el-table-column label="操作" width="110" fixed="right"><template #default="{ row }"><el-button v-if="editable" link type="primary" @click="openCouponActivity(row)">编辑</el-button></template></el-table-column>
        </el-table>
        <el-pagination class="pager" v-model:current-page="couponActivityQuery.page" :page-size="couponActivityQuery.pageSize" :total="couponActivityTotal" layout="total, prev, pager, next" @current-change="loadCouponActivities" />
      </el-tab-pane>

      <!-- ============ 效果报表 ============ -->
      <el-tab-pane label="效果报表" name="reports">
        <div class="toolbar">
          <el-date-picker v-model="reportRange" type="daterange" range-separator="~" start-placeholder="开始" end-placeholder="结束" value-format="YYYY-MM-DD" style="width:260px" />
          <el-button type="primary" @click="loadReports">查询</el-button>
        </div>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-card shadow="never">
              <template #header><div class="report-head"><span>活动效果（订单数 / 折扣总额 / 赠券）</span><b>合计 {{ Number(activityReport.totalDiscount || 0).toFixed(2) }} 元</b></div></template>
              <el-table :data="activityReport.summary || []" border @row-click="row => drillActivity(row)">
                <el-table-column prop="activityName" label="活动" min-width="140" />
                <el-table-column label="类型" width="80"><template #default="{ row }">{{ activityTypeText[row.activityType] }}</template></el-table-column>
                <el-table-column prop="orderCount" label="订单数" width="90" />
                <el-table-column label="折扣总额" width="110"><template #default="{ row }">¥{{ Number(row.discountAmount).toFixed(2) }}</template></el-table-column>
                <el-table-column prop="giftCouponCount" label="赠券" width="80" />
              </el-table>
            </el-card>
          </el-col>
          <el-col :span="12">
            <el-card shadow="never">
              <template #header><div class="report-head"><span>券效果（核销笔数 / 抵扣总额）</span><b>合计 {{ Number(couponReport.totalDiscount || 0).toFixed(2) }} 元</b></div></template>
              <el-table :data="couponReport.summary || []" border @row-click="row => drillCoupon(row)">
                <el-table-column prop="couponActivityName" label="券活动" min-width="160" />
                <el-table-column prop="couponCount" label="核销笔数" width="100" />
                <el-table-column label="抵扣总额" width="120"><template #default="{ row }">¥{{ Number(row.discountAmount).toFixed(2) }}</template></el-table-column>
              </el-table>
            </el-card>
          </el-col>
        </el-row>
        <p class="muted drill-tip">点击汇总行可下钻订单明细</p>
      </el-tab-pane>

      <!-- ============ 营销配置 ============ -->
      <el-tab-pane label="营销配置" name="config">
        <el-form label-width="150px" style="max-width:520px">
          <el-form-item v-if="canChoosePlatform" label="平台">
            <el-select v-model="platformId" filterable style="width:100%" @change="loadConfig">
              <el-option v-for="item in platforms" :key="item.id" :value="item.id" :label="item.platformName" />
            </el-select>
          </el-form-item>
          <el-form-item label="优惠计算优先级">
            <el-radio-group v-model="discountPriority">
              <el-radio :value="1">活动优先（先取最优活动，无活动可用券）</el-radio>
              <el-radio :value="2">券优先（先取最优券，无券可用活动）</el-radio>
            </el-radio-group>
          </el-form-item>
          <el-form-item>
            <el-button v-if="editable" type="primary" @click="saveConfig">保存配置</el-button>
          </el-form-item>
          <el-alert type="info" :closable="false" title="券与活动互斥：同一商品最终只享受一种优惠，按此处的优先级选择。" />
        </el-form>
      </el-tab-pane>
    </el-tabs>

    <!-- ============ 活动弹窗 ============ -->
    <el-dialog v-model="activityDialog" :title="activityForm.id ? '编辑活动' : '新建活动'" width="680px" @closed="activityFormRef?.clearValidate()">
      <el-form ref="activityFormRef" :model="activityForm" :rules="activityRules" label-width="110px">
        <el-form-item label="活动名称" prop="name"><el-input v-model="activityForm.name" maxlength="64" show-word-limit /></el-form-item>
        <el-form-item label="归属" prop="merchantId">
          <el-select v-model="activityForm.merchantId" style="width:100%">
            <el-option :value="0" label="平台活动（全平台生效）" />
            <el-option v-for="item in merchants" :key="item.id" :value="item.id" :label="`商户活动：${item.merchantName}`" />
          </el-select>
        </el-form-item>
        <el-form-item label="活动类型" prop="activityType">
          <el-radio-group v-model="activityForm.activityType">
            <el-radio :value="1">满减</el-radio><el-radio :value="2">满折</el-radio><el-radio :value="3">满赠</el-radio>
          </el-radio-group>
        </el-form-item>
        <el-form-item label="门槛金额" prop="threshold"><el-input-number v-model="activityForm.threshold" :min="0" :precision="2" style="width:200px" /></el-form-item>
        <el-form-item v-if="activityForm.activityType === 1" label="减免金额" prop="discountValue"><el-input-number v-model="activityForm.discountValue" :min="0.01" :precision="2" style="width:200px" /></el-form-item>
        <el-form-item v-if="activityForm.activityType === 2" label="折扣率" prop="discountValue">
          <el-input-number v-model="activityForm.discountValue" :min="0.01" :max="0.99" :step="0.05" :precision="2" style="width:200px" />
          <span class="muted" style="margin-left:8px">0.85 = 8.5 折</span>
        </el-form-item>
        <el-form-item v-if="activityForm.activityType === 3" label="赠品券活动" prop="giftCouponActivityId">
          <el-select v-model="activityForm.giftCouponActivityId" filterable style="width:100%" placeholder="选择要发放的券活动">
            <el-option v-for="item in couponActivityOptions" :key="item.id" :value="item.id" :label="item.name" />
          </el-select>
        </el-form-item>
        <el-form-item label="参与范围" prop="scopeType">
          <el-radio-group v-model="activityForm.scopeType">
            <el-radio :value="1">{{ activityForm.merchantId > 0 ? '本商户全部商品' : '全平台' }}</el-radio>
            <el-radio v-if="activityForm.merchantId === 0" :value="2">指定商户</el-radio>
            <el-radio :value="3">指定商品</el-radio>
          </el-radio-group>
        </el-form-item>
        <el-form-item v-if="activityForm.scopeType === 2" label="指定商户" prop="targetMerchantIds">
          <el-select v-model="activityForm.targetMerchantIds" multiple filterable style="width:100%">
            <el-option v-for="item in merchants" :key="item.id" :value="item.id" :label="item.merchantName" />
          </el-select>
        </el-form-item>
        <el-form-item v-if="activityForm.scopeType === 3" label="指定商品" prop="targetSkuIds">
          <el-select v-model="activityForm.targetSkuIds" multiple filterable style="width:100%" placeholder="选择商品（SKU）">
            <el-option v-for="item in productOptions" :key="item.skuId" :value="item.skuId" :label="`${item.name}（${item.skuCode}）`" />
          </el-select>
        </el-form-item>
        <el-form-item label="活动时间" prop="range">
          <el-date-picker v-model="activityForm.range" type="datetimerange" range-separator="~" style="width:100%" value-format="YYYY-MM-DDTHH:mm:ss" />
        </el-form-item>
        <el-form-item label="活动说明"><el-input v-model="activityForm.description" type="textarea" maxlength="255" /></el-form-item>
        <el-form-item label="启用"><el-switch v-model="activityForm.isEnabled" /></el-form-item>
      </el-form>
      <template #footer><el-button @click="activityDialog = false">取消</el-button><el-button type="primary" @click="saveActivity">保存</el-button></template>
    </el-dialog>

    <!-- ============ 券模板弹窗 ============ -->
    <el-dialog v-model="templateDialog" :title="templateForm.id ? '编辑券模板' : '新建券模板'" width="560px" @closed="templateFormRef?.clearValidate()">
      <el-form ref="templateFormRef" :model="templateForm" :rules="templateRules" label-width="110px">
        <el-form-item label="模板名称" prop="name"><el-input v-model="templateForm.name" maxlength="64" show-word-limit /></el-form-item>
        <el-form-item label="归属" prop="merchantId">
          <el-select v-model="templateForm.merchantId" style="width:100%">
            <el-option :value="0" label="平台券模板" />
            <el-option v-for="item in merchants" :key="item.id" :value="item.id" :label="`商户券：${item.merchantName}`" />
          </el-select>
        </el-form-item>
        <el-form-item label="券类型" prop="couponType">
          <el-radio-group v-model="templateForm.couponType">
            <el-radio :value="1">满减</el-radio><el-radio :value="2">满折</el-radio><el-radio :value="3">0元减</el-radio>
          </el-radio-group>
        </el-form-item>
        <el-form-item v-if="templateForm.couponType !== 3" label="门槛金额" prop="threshold"><el-input-number v-model="templateForm.threshold" :min="0" :precision="2" style="width:200px" /></el-form-item>
        <el-form-item v-if="templateForm.couponType !== 2" label="优惠金额" prop="discountValue"><el-input-number v-model="templateForm.discountValue" :min="0.01" :precision="2" style="width:200px" /></el-form-item>
        <el-form-item v-else label="折扣率" prop="discountValue">
          <el-input-number v-model="templateForm.discountValue" :min="0.01" :max="0.99" :step="0.05" :precision="2" style="width:200px" />
          <span class="muted" style="margin-left:8px">0.85 = 8.5 折</span>
        </el-form-item>
        <el-form-item label="有效天数" prop="validDays"><el-input-number v-model="templateForm.validDays" :min="1" :max="365" style="width:200px" /></el-form-item>
        <el-form-item label="使用说明"><el-input v-model="templateForm.description" type="textarea" maxlength="255" /></el-form-item>
        <el-form-item label="启用"><el-switch v-model="templateForm.isEnabled" /></el-form-item>
      </el-form>
      <template #footer><el-button @click="templateDialog = false">取消</el-button><el-button type="primary" @click="saveTemplate">保存</el-button></template>
    </el-dialog>

    <!-- ============ 券活动弹窗 ============ -->
    <el-dialog v-model="couponActivityDialog" :title="couponActivityForm.id ? '编辑券活动' : '新建券活动'" width="620px" @closed="couponActivityFormRef?.clearValidate()">
      <el-form ref="couponActivityFormRef" :model="couponActivityForm" :rules="couponActivityRules" label-width="110px">
        <el-form-item label="券活动名称" prop="name"><el-input v-model="couponActivityForm.name" maxlength="64" show-word-limit /></el-form-item>
        <el-form-item label="券模板" prop="couponTemplateId">
          <el-select v-model="couponActivityForm.couponTemplateId" filterable style="width:100%" placeholder="选择券模板">
            <el-option v-for="item in templateOptions" :key="item.id" :value="item.id" :label="`${item.name}（${couponBenefit(item)}）`" />
          </el-select>
        </el-form-item>
        <el-form-item label="参与范围" prop="scopeType">
          <el-radio-group v-model="couponActivityForm.scopeType">
            <el-radio :value="1">全部范围</el-radio>
            <el-radio v-if="couponActivityForm.merchantId === 0" :value="2">指定商户</el-radio>
            <el-radio :value="3">指定商品</el-radio>
          </el-radio-group>
        </el-form-item>
        <el-form-item v-if="couponActivityForm.scopeType === 2" label="指定商户" prop="targetMerchantIds">
          <el-select v-model="couponActivityForm.targetMerchantIds" multiple filterable style="width:100%">
            <el-option v-for="item in merchants" :key="item.id" :value="item.id" :label="item.merchantName" />
          </el-select>
        </el-form-item>
        <el-form-item v-if="couponActivityForm.scopeType === 3" label="指定商品" prop="targetSkuIds">
          <el-select v-model="couponActivityForm.targetSkuIds" multiple filterable style="width:100%">
            <el-option v-for="item in productOptions" :key="item.skuId" :value="item.skuId" :label="`${item.name}（${item.skuCode}）`" />
          </el-select>
        </el-form-item>
        <el-form-item label="发行总量" prop="totalStock"><el-input-number v-model="couponActivityForm.totalStock" :min="1" :max="1000000" style="width:200px" /></el-form-item>
        <el-form-item label="每人限领" prop="perUserLimit"><el-input-number v-model="couponActivityForm.perUserLimit" :min="1" :max="100" style="width:200px" /></el-form-item>
        <el-form-item label="领券中心"><el-switch v-model="couponActivityForm.isClaimable" /></el-form-item>
        <el-form-item label="活动时间" prop="range">
          <el-date-picker v-model="couponActivityForm.range" type="datetimerange" range-separator="~" style="width:100%" value-format="YYYY-MM-DDTHH:mm:ss" />
        </el-form-item>
        <el-form-item label="启用"><el-switch v-model="couponActivityForm.isEnabled" /></el-form-item>
      </el-form>
      <template #footer><el-button @click="couponActivityDialog = false">取消</el-button><el-button type="primary" @click="saveCouponActivity">保存</el-button></template>
    </el-dialog>

    <!-- ============ 报表下钻 ============ -->
    <el-dialog v-model="drillDialog" :title="drillTitle" width="860px">
      <el-table :data="drillRecords" border>
        <el-table-column prop="orderNo" label="订单号" min-width="190" />
        <el-table-column prop="activityName" label="活动/券" min-width="140"><template #default="{ row }">{{ row.activityName || row.couponActivityName }}</template></el-table-column>
        <el-table-column label="折扣" width="110"><template #default="{ row }">¥{{ Number(row.discountAmount).toFixed(2) }}</template></el-table-column>
        <el-table-column prop="createdAt" label="时间" width="180" />
      </el-table>
      <el-divider content-position="left">商品明细</el-divider>
      <el-table :data="drillItems" border max-height="260">
        <el-table-column prop="orderNo" label="订单号" min-width="190" />
        <el-table-column prop="productName" label="商品" min-width="140" />
        <el-table-column prop="quantity" label="数量" width="70" />
        <el-table-column label="行金额" width="100"><template #default="{ row }">¥{{ Number(row.itemAmount).toFixed(2) }}</template></el-table-column>
        <el-table-column label="行折扣" width="100"><template #default="{ row }">¥{{ Number(row.discountAmount).toFixed(2) }}</template></el-table-column>
      </el-table>
    </el-dialog>
  </el-card>
</template>

<script setup>
import { ElMessage } from 'element-plus'
import { computed, onMounted, reactive, ref } from 'vue'
import request from '@/api/request'
import { hasPermission as allow } from '@/utils/permission'
import { trimForm } from '@/utils/validators'

const tab = ref('activities')
const editable = computed(() => allow('marketing:create'))
const user = JSON.parse(localStorage.getItem('admin_user') || 'null')
const canChoosePlatform = computed(() => (user?.permissions || []).includes('*'))
const platforms = ref([]); const merchants = ref([]); const productOptions = ref([])
const templateOptions = ref([]); const couponActivityOptions = ref([])
const platformId = ref(user?.platformId && Number(user.platformId) > 0 ? user.platformId : '')

const activityTypeText = { 1: '满减', 2: '满折', 3: '满赠' }
const couponTypeText = { 1: '满减', 2: '满折', 3: '0元减' }
const scopeText = (scopeType, merchantId) => scopeType === 2 ? '指定商户' : scopeType === 3 ? '指定商品' : (merchantId > 0 ? '本商户全部商品' : '全平台')
const activityBenefit = row => row.activityType === 1 ? `满${row.threshold}减${row.discountValue}` : row.activityType === 2 ? `满${row.threshold}打${(row.discountValue * 10).toFixed(1)}折` : `满${row.threshold}赠券`
const couponBenefit = row => row.couponType === 1 ? `满${row.threshold}减${row.discountValue}` : row.couponType === 2 ? `满${row.threshold}打${(row.discountValue * 10).toFixed(1)}折` : `0元减${row.discountValue}`

const activities = ref([]); const activityTotal = ref(0)
const activityQuery = reactive({ keyword: '', activityType: null, page: 1, pageSize: 10 })
const templates = ref([]); const templateTotal = ref(0)
const templateQuery = reactive({ keyword: '', page: 1, pageSize: 10 })
const couponActivities = ref([]); const couponActivityTotal = ref(0)
const couponActivityQuery = reactive({ keyword: '', page: 1, pageSize: 10 })
const reportRange = ref([])
const activityReport = ref({}); const couponReport = ref({})
const discountPriority = ref(2)

const baseParams = () => (platformId.value ? { platformId: platformId.value } : {})

const loadReferences = async () => {
  const [platformData, merchantData, productData] = await Promise.all([
    request.get('/platforms/List', { params: { page: 1, pageSize: 100 } }).catch(() => ({ items: [] })),
    request.get('/merchants/List', { params: { page: 1, pageSize: 100 } }).catch(() => ({ items: [] })),
    request.get('/products/List', { params: { page: 1, pageSize: 100 } }).catch(() => ({ items: [] }))
  ])
  platforms.value = platformData.items || []
  merchants.value = merchantData.items || []
  productOptions.value = (productData.items || []).flatMap(product => (product.skus || []).map(sku => ({
    skuId: sku.id, skuCode: sku.skuCode, name: product.name, merchantId: product.merchantId
  })))
  if (!platformId.value && platforms.value.length) platformId.value = platforms.value[0].id
}

const loadActivities = async () => {
  const data = await request.get('/marketing/ActivityList', { params: { ...activityQuery, ...baseParams() } })
  activities.value = data.items || []; activityTotal.value = Number(data.total || 0)
}
const loadTemplates = async () => {
  const data = await request.get('/marketing/CouponTemplateList', { params: { ...templateQuery, ...baseParams() } })
  templates.value = data.items || []; templateTotal.value = Number(data.total || 0)
}
const loadCouponActivities = async () => {
  const data = await request.get('/marketing/CouponActivityList', { params: { ...couponActivityQuery, ...baseParams() } })
  couponActivities.value = data.items || []; couponActivityTotal.value = Number(data.total || 0)
  couponActivityOptions.value = data.items || []
}
const loadOptionLists = async () => {
  const [templateData, couponData] = await Promise.all([
    request.get('/marketing/CouponTemplateList', { params: { page: 1, pageSize: 100, ...baseParams() } }).catch(() => ({ items: [] })),
    request.get('/marketing/CouponActivityList', { params: { page: 1, pageSize: 100, ...baseParams() } }).catch(() => ({ items: [] }))
  ])
  templateOptions.value = templateData.items || []
  couponActivityOptions.value = couponData.items || []
}
const loadReports = async () => {
  const params = { page: 1, pageSize: 50, from: reportRange.value?.[0], to: reportRange.value?.[1] }
  const [activityData, couponData] = await Promise.all([
    request.get('/marketing/ActivityReport', { params }),
    request.get('/marketing/CouponReport', { params })
  ])
  activityReport.value = activityData; couponReport.value = couponData
}
const loadConfig = async () => {
  if (!platformId.value) return
  const data = await request.get('/marketing/Config', { params: { platformId: platformId.value } })
  discountPriority.value = Number(data.discountPriority || 2)
}
const saveConfig = async () => {
  await request.post('/marketing/SaveConfig', { platformId: platformId.value, discountPriority: discountPriority.value })
  ElMessage.success('配置已保存')
}

// ---------- 活动弹窗 ----------
const activityDialog = ref(false); const activityFormRef = ref(null)
const emptyActivity = () => ({ id: 0, name: '', merchantId: 0, activityType: 1, threshold: 0, discountValue: 0, giftCouponActivityId: '', scopeType: 1, targetMerchantIds: [], targetSkuIds: [], range: [], description: '', isEnabled: true })
const activityForm = reactive(emptyActivity())
const activityRules = {
  name: [{ required: true, message: '请输入活动名称', trigger: 'blur' }],
  activityType: [{ required: true, message: '请选择活动类型', trigger: 'change' }],
  scopeType: [{ required: true, message: '请选择参与范围', trigger: 'change' }]
}
const openActivity = async row => {
  Object.assign(activityForm, emptyActivity())
  await loadOptionLists()
  if (row) {
    const detail = await request.get('/marketing/ActivityDetail', { params: { id: row.id } })
    const item = detail.activity
    Object.assign(activityForm, {
      id: item.id, name: item.name, merchantId: item.merchantId, activityType: item.activityType,
      threshold: Number(item.threshold), discountValue: Number(item.discountValue),
      giftCouponActivityId: item.giftCouponActivityId && Number(item.giftCouponActivityId) > 0 ? item.giftCouponActivityId : '',
      scopeType: item.scopeType, description: item.description, isEnabled: item.isEnabled,
      range: [item.startAt?.slice(0, 19), item.endAt ? item.endAt.slice(0, 19) : null].filter(Boolean)
    })
    const targets = detail.targets || []
    activityForm.targetMerchantIds = targets.filter(t => Number(t.targetType) === 1).map(t => t.targetId)
    activityForm.targetSkuIds = targets.filter(t => Number(t.targetType) === 2).map(t => t.targetId)
  }
  activityDialog.value = true
}
const saveActivity = async () => {
  const valid = await activityFormRef.value?.validate().catch(() => false)
  const error = validateActivityForm()
  if (!valid || error) return ElMessage.warning(error || '请按红色提示修正输入')
  trimForm(activityForm)
  await request.post('/marketing/SaveActivity', {
    id: activityForm.id, platformId: platformId.value, merchantId: activityForm.merchantId,
    name: activityForm.name, description: activityForm.description, activityType: activityForm.activityType,
    threshold: activityForm.threshold,
    discountValue: activityForm.activityType === 3 ? 0 : activityForm.discountValue,
    giftCouponActivityId: activityForm.activityType === 3 ? activityForm.giftCouponActivityId : 0,
    scopeType: activityForm.scopeType, targetMerchantIds: activityForm.targetMerchantIds,
    targetProducts: activityForm.targetSkuIds.map(skuId => ({ skuId, merchantId: productOptions.value.find(item => item.skuId === skuId)?.merchantId || 0 })),
    startAt: activityForm.range?.[0] || new Date().toISOString().slice(0, 19),
    endAt: activityForm.range?.[1] || null, isEnabled: activityForm.isEnabled
  })
  ElMessage.success('活动已保存'); activityDialog.value = false; loadActivities()
}
const validateActivityForm = () => {
  if (activityForm.activityType === 1 && !(Number(activityForm.discountValue) > 0)) return '满减活动减免金额必须大于0'
  if (activityForm.activityType === 2 && !(Number(activityForm.discountValue) > 0 && Number(activityForm.discountValue) < 1)) return '满折活动折扣率必须在0.01-0.99之间'
  if (activityForm.activityType === 3 && !activityForm.giftCouponActivityId) return '满赠活动必须选择赠品券活动'
  if (activityForm.scopeType === 2 && !activityForm.targetMerchantIds.length) return '请选择参与商户'
  if (activityForm.scopeType === 3 && !activityForm.targetSkuIds.length) return '请选择参与商品'
  return ''
}
const toggleActivity = async row => { await request.post('/marketing/SetActivityEnabled', { id: row.id, isEnabled: !row.isEnabled }); loadActivities() }

// ---------- 券模板弹窗 ----------
const templateDialog = ref(false); const templateFormRef = ref(null)
const emptyTemplate = () => ({ id: 0, name: '', merchantId: 0, couponType: 1, threshold: 0, discountValue: 0, validDays: 7, description: '', isEnabled: true })
const templateForm = reactive(emptyTemplate())
const templateRules = { name: [{ required: true, message: '请输入模板名称', trigger: 'blur' }] }
const openTemplate = row => {
  Object.assign(templateForm, emptyTemplate())
  if (row) Object.assign(templateForm, {
    id: row.id, name: row.name, merchantId: row.merchantId, couponType: row.couponType,
    threshold: Number(row.threshold), discountValue: Number(row.discountValue), validDays: row.validDays,
    description: row.description, isEnabled: row.isEnabled
  })
  templateDialog.value = true
}
const saveTemplate = async () => {
  const valid = await templateFormRef.value?.validate().catch(() => false)
  if (!valid) return
  if (templateForm.couponType !== 2 && !(Number(templateForm.discountValue) > 0)) return ElMessage.warning('优惠金额必须大于0')
  if (templateForm.couponType === 2 && !(Number(templateForm.discountValue) > 0 && Number(templateForm.discountValue) < 1)) return ElMessage.warning('折扣率必须在0.01-0.99之间')
  trimForm(templateForm)
  await request.post('/marketing/SaveCouponTemplate', {
    id: templateForm.id, platformId: platformId.value, merchantId: templateForm.merchantId,
    name: templateForm.name, description: templateForm.description, couponType: templateForm.couponType,
    threshold: templateForm.couponType === 3 ? 0 : templateForm.threshold,
    discountValue: templateForm.discountValue, validDays: templateForm.validDays, isEnabled: templateForm.isEnabled
  })
  ElMessage.success('券模板已保存'); templateDialog.value = false; loadTemplates()
}
const toggleTemplate = async row => { await request.post('/marketing/SetCouponTemplateEnabled', { id: row.id, isEnabled: !row.isEnabled }); loadTemplates() }

// ---------- 券活动弹窗 ----------
const couponActivityDialog = ref(false); const couponActivityFormRef = ref(null)
const emptyCouponActivity = () => ({ id: 0, name: '', merchantId: 0, couponTemplateId: '', scopeType: 1, targetMerchantIds: [], targetSkuIds: [], totalStock: 1000, perUserLimit: 1, isClaimable: true, range: [], isEnabled: true })
const couponActivityForm = reactive(emptyCouponActivity())
const couponActivityRules = {
  name: [{ required: true, message: '请输入券活动名称', trigger: 'blur' }],
  couponTemplateId: [{ required: true, message: '请选择券模板', trigger: 'change' }]
}
const openCouponActivity = async row => {
  Object.assign(couponActivityForm, emptyCouponActivity())
  await loadOptionLists()
  if (row) {
    const detail = await request.get('/marketing/CouponActivityDetail', { params: { id: row.id } })
    const item = detail.activity
    Object.assign(couponActivityForm, {
      id: item.id, name: item.name, merchantId: item.merchantId, couponTemplateId: item.couponTemplateId,
      scopeType: item.scopeType, totalStock: item.totalStock, perUserLimit: item.perUserLimit,
      isClaimable: item.isClaimable, isEnabled: item.isEnabled,
      range: [item.startAt?.slice(0, 19), item.endAt ? item.endAt.slice(0, 19) : null].filter(Boolean)
    })
    const targets = detail.targets || []
    couponActivityForm.targetMerchantIds = targets.filter(t => Number(t.targetType) === 1).map(t => t.targetId)
    couponActivityForm.targetSkuIds = targets.filter(t => Number(t.targetType) === 2).map(t => t.targetId)
  }
  couponActivityDialog.value = true
}
const saveCouponActivity = async () => {
  const valid = await couponActivityFormRef.value?.validate().catch(() => false)
  if (!valid) return
  if (couponActivityForm.scopeType === 2 && !couponActivityForm.targetMerchantIds.length) return ElMessage.warning('请选择参与商户')
  if (couponActivityForm.scopeType === 3 && !couponActivityForm.targetSkuIds.length) return ElMessage.warning('请选择参与商品')
  trimForm(couponActivityForm)
  await request.post('/marketing/SaveCouponActivity', {
    id: couponActivityForm.id, platformId: platformId.value, merchantId: couponActivityForm.merchantId,
    name: couponActivityForm.name, couponTemplateId: couponActivityForm.couponTemplateId,
    scopeType: couponActivityForm.scopeType, targetMerchantIds: couponActivityForm.targetMerchantIds,
    targetProducts: couponActivityForm.targetSkuIds.map(skuId => ({ skuId, merchantId: productOptions.value.find(item => item.skuId === skuId)?.merchantId || 0 })),
    totalStock: couponActivityForm.totalStock, perUserLimit: couponActivityForm.perUserLimit,
    isClaimable: couponActivityForm.isClaimable,
    startAt: couponActivityForm.range?.[0] || new Date().toISOString().slice(0, 19),
    endAt: couponActivityForm.range?.[1] || null, isEnabled: couponActivityForm.isEnabled
  })
  ElMessage.success('券活动已保存'); couponActivityDialog.value = false; loadCouponActivities()
}
const toggleCouponActivity = async row => { await request.post('/marketing/SetCouponActivityEnabled', { id: row.id, isEnabled: !row.isEnabled }); loadCouponActivities() }

// ---------- 报表下钻 ----------
const drillDialog = ref(false); const drillTitle = ref(''); const drillRecords = ref([]); const drillItems = ref([])
const drillActivity = async row => {
  const data = await request.get('/marketing/ActivityReport', { params: { activityId: row.activityId, page: 1, pageSize: 50, from: reportRange.value?.[0], to: reportRange.value?.[1] } })
  drillTitle.value = `活动：${row.activityName}`; drillRecords.value = data.records || []; drillItems.value = data.recordItems || []; drillDialog.value = true
}
const drillCoupon = async row => {
  const data = await request.get('/marketing/CouponReport', { params: { couponActivityId: row.couponActivityId, page: 1, pageSize: 50, from: reportRange.value?.[0], to: reportRange.value?.[1] } })
  drillTitle.value = `券活动：${row.couponActivityName}`; drillRecords.value = data.records || []; drillItems.value = data.recordItems || []; drillDialog.value = true
}

onMounted(async () => {
  await loadReferences()
  await Promise.all([loadActivities(), loadTemplates(), loadCouponActivities(), loadReports(), loadConfig()])
})
</script>

<style scoped>
.muted { color: var(--apple-text-3, #86868b); font-size: 12px; }
.report-head { display: flex; justify-content: space-between; align-items: center; }
.drill-tip { margin: 12px 0 0; }
</style>
