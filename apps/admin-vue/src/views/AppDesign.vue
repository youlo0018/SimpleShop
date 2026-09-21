<template>
  <el-card class="page-card">
    <template #header>
      <div class="header-row">
        <div class="title-group">
          <span>小程序装修</span>
          <el-tag v-if="isPublished" type="success" size="small">已发布 v{{ version }}</el-tag>
          <el-tag v-else type="info" size="small">未发布（草稿）</el-tag>
        </div>
        <div class="toolbar">
          <el-select v-model="platformId" filterable style="width:220px" @change="load">
            <el-option v-for="platform in platforms" :key="platform.id" :value="platform.id" :label="platform.platformName" />
          </el-select>
          <el-button :loading="saving" @click="save(false)">保存草稿</el-button>
          <el-button type="primary" :loading="saving" @click="save(true)">保存并发布</el-button>
        </div>
      </div>
    </template>

    <div v-if="dragState.active" class="drag-ghost" :style="{ left: dragState.x + 14 + 'px', top: dragState.y + 14 + 'px' }">
      <span>{{ ghostInfo.icon }}</span><span>{{ ghostInfo.title }}</span>
    </div>

    <div class="editor">
      <!-- 左：组件库 / 我的页 / 主题 -->
      <aside class="panel left-panel">
        <el-tabs v-model="activeTab">
          <el-tab-pane label="首页装修" name="home">
            <div class="hint">拖拽组件到手机预览中，或点击直接添加到末尾；预览中可拖动排序。</div>
            <div class="section-head">
              <span>首页轮播图（头图）</span>
              <el-button size="small" @click="addBanner">添加</el-button>
            </div>
            <div class="banner-list">
              <div
                v-for="(banner, index) in design.home.banners"
                :key="`bn-${index}`"
                :class="['banner-item', isSelected('banner', index) && 'on']"
                @click="select('banner', index)"
              >
                <img class="banner-thumb" :src="assetUrl(banner.image)" alt="" />
                <div class="banner-info">
                  <div class="banner-title">{{ banner.title || '未命名轮播图' }}</div>
                  <div class="banner-link">{{ linkTypeText(banner.linkType) }}</div>
                </div>
                <span class="mini-actions" @mousedown.stop>
                  <el-button link size="small" :disabled="!index" @click.stop="moveBanner(index, -1)">↑</el-button>
                  <el-button link size="small" :disabled="index === design.home.banners.length - 1" @click.stop="moveBanner(index, 1)">↓</el-button>
                  <el-button link size="small" type="danger" @click.stop="design.home.banners.splice(index, 1)">删</el-button>
                </span>
              </div>
              <div v-if="!design.home.banners.length" class="banner-empty">暂无轮播图，点击「添加」或拖拽图片到右侧预览</div>
            </div>

            <div class="palette">
              <div
                v-for="item in palette"
                :key="item.type"
                class="palette-item"
                @mousedown.prevent="startDrag($event, { kind: 'palette', type: item.type })"
                @click="appendModule(item.type)"
              >
                <span class="palette-icon">{{ item.icon }}</span>
                <div class="palette-text">
                  <div class="palette-name">{{ item.label }}</div>
                  <div class="palette-desc">{{ item.desc }}</div>
                </div>
              </div>
            </div>
          </el-tab-pane>

          <el-tab-pane label="我的页装修" name="profile">
            <div class="hint">「我的页」由会员卡 + 权益行 + 我的账户 + 我的服务组成；下面可增删与排序（预览中也可拖动）。</div>
            <div class="section-head">
              <span>会员权益行（4 个圆形入口）</span>
              <el-button size="small" @click="addProfileItem('benefits')">添加</el-button>
            </div>
            <div class="mini-list">
              <div
                v-for="(item, index) in design.profile.benefits"
                :key="`b-${index}`"
                :class="['mini-item', isSelected('benefit', index) && 'on']"
                :data-profile-list="'benefits'"
                :data-item-index="index"
                @mousedown.prevent="startDrag($event, { kind: 'benefit', index })"
                @click="select('benefit', index)"
              >
                <span class="mini-icon">{{ iconText(item.icon) }}</span>
                <span class="mini-title">{{ item.title || '未命名' }}</span>
                <span class="mini-actions">
                  <el-button link size="small" :disabled="!index" @click.stop="moveProfileItem('benefits', index, -1)">↑</el-button>
                  <el-button link size="small" :disabled="index === design.profile.benefits.length - 1" @click.stop="moveProfileItem('benefits', index, 1)">↓</el-button>
                  <el-button link size="small" type="danger" @click.stop="design.profile.benefits.splice(index, 1)">删</el-button>
                </span>
              </div>
            </div>

            <div class="section-head">
              <span>我的服务（每行 5 个宫格）</span>
              <el-button size="small" @click="addProfileItem('services')">添加</el-button>
            </div>
            <div class="mini-list">
              <div
                v-for="(item, index) in design.profile.services"
                :key="`s-${index}`"
                :class="['mini-item', isSelected('service', index) && 'on']"
                :data-profile-list="'services'"
                :data-item-index="index"
                @mousedown.prevent="startDrag($event, { kind: 'service', index })"
                @click="select('service', index)"
              >
                <span class="mini-icon">{{ iconText(item.icon) }}</span>
                <span class="mini-title">{{ item.title || '未命名' }}</span>
                <span class="mini-actions">
                  <el-button link size="small" :disabled="!index" @click.stop="moveProfileItem('services', index, -1)">↑</el-button>
                  <el-button link size="small" :disabled="index === design.profile.services.length - 1" @click.stop="moveProfileItem('services', index, 1)">↓</el-button>
                  <el-button link size="small" type="danger" @click.stop="design.profile.services.splice(index, 1)">删</el-button>
                </span>
              </div>
            </div>
          </el-tab-pane>

          <el-tab-pane label="主题与标签" name="theme">
            <el-form label-width="86px" class="theme-form">
              <el-form-item label="商城名称"><el-input v-model="design.home.appName" maxlength="20" show-word-limit /></el-form-item>
              <el-form-item label="副标题"><el-input v-model="design.home.slogan" maxlength="40" show-word-limit /></el-form-item>
              <el-form-item label="公告"><el-input v-model="design.home.notice" type="textarea" :rows="2" maxlength="200" show-word-limit /></el-form-item>
              <el-form-item label="主色"><el-color-picker v-model="design.theme.primary" /></el-form-item>
              <el-form-item label="标签选中色"><el-color-picker v-model="design.theme.tabColor" /></el-form-item>
              <el-form-item label="背景色"><el-color-picker v-model="design.theme.background" /></el-form-item>
              <el-form-item label="底部标签">
                <div class="tab-inputs">
                  <el-input v-model="design.tabs.home" maxlength="8" placeholder="首页" />
                  <el-input v-model="design.tabs.category" maxlength="8" placeholder="分类" />
                  <el-input v-model="design.tabs.cart" maxlength="8" placeholder="购物车" />
                  <el-input v-model="design.tabs.profile" maxlength="8" placeholder="我的" />
                </div>
              </el-form-item>
            </el-form>
          </el-tab-pane>
        </el-tabs>

      </aside>

      <!-- 中：手机预览（所见即所得） -->
      <main class="canvas">
        <div class="phone">
          <div class="phone-notch" />
          <div class="phone-screen" :class="{ 'drag-in': dragState.active && !dragState.outside, 'drag-out': dragState.outside }" :style="{ background: design.theme.background }">
            <div v-if="dragState.outside" class="drag-out-hint">松手删除该{{ dragState.kind === 'module' ? '模块' : '入口' }}</div>
            <div class="status-bar" :style="{ color: '#1d1d1f' }">
              <span>9:41</span>
              <span class="status-icons">●●● ▮</span>
            </div>
            <div class="screen-scroll">
              <template v-if="activeTab === 'profile'">
                <div class="mock-profile-head">
                  <div class="mock-avatar">{{ (design.home.appName || 'M').slice(0, 1) }}</div>
                  <div><div class="mock-name">Hi，会员</div><div class="mock-sub">138****0000</div></div>
                </div>
                <div class="mock-tier" :style="{ background: tierGradient }">
                  <div class="mock-tier-name">普通会员</div>
                  <div class="mock-tier-bar"><i :style="{ width: '35%', background: '#fff' }" /></div>
                </div>
                <div class="mock-card">
                  <div class="mock-section-title">{{ design.home.appName || '本平台' }}普通会员，享受以下权益</div>
                  <div class="mock-benefits">
                    <div
                      v-for="(item, index) in design.profile.benefits"
                      :key="`pb-${index}`"
                      :class="['mock-benefit', isSelected('benefit', index) && 'on']"
                      @click="select('benefit', index)"
                    >
                      <span class="mock-benefit-icon">{{ iconText(item.icon) }}</span>
                      <span class="mock-benefit-label">{{ item.title || '权益' }}</span>
                    </div>
                  </div>
                </div>
                <div class="mock-card">
                  <div class="mock-section-title">我的账户</div>
                  <div class="mock-stats"><span>0<small>张</small><em>优惠券</em></span><span>0<em>我的收藏</em></span><span>0<em>订单</em></span></div>
                </div>
                <div class="mock-card">
                  <div class="mock-section-title">我的服务</div>
                  <div class="mock-services">
                    <div
                      v-for="(item, index) in design.profile.services"
                      :key="`ps-${index}`"
                      :class="['mock-service', isSelected('service', index) && 'on']"
                      @click="select('service', index)"
                    >
                      <span class="mock-service-icon">{{ iconText(item.icon) }}</span>
                      <span class="mock-service-label">{{ item.title || '服务' }}</span>
                    </div>
                  </div>
                </div>
              </template>

              <template v-else>
                <!-- 首页头图：轮播图优先，否则主题渐变 -->
                <div class="mock-hero" :style="heroStyle">
                  <img v-if="heroImage" class="mock-hero-image" :src="assetUrl(heroImage)" alt="" @click.stop="selectBanner" />
                  <div class="mock-hero-topbar">📍 {{ currentPlatformName }}</div>
                  <div v-if="!heroImage" class="mock-hero-text">
                    <div class="mock-hero-title">{{ design.home.appName || '商城' }}</div>
                    <div class="mock-hero-sub">{{ design.home.slogan || '本平台专属精选商城' }}</div>
                  </div>
                  <div v-if="heroImages.length > 1" class="mock-hero-dots">
                    <i v-for="(image, index) in heroImages" :key="index" :class="[index === 0 && 'on']" />
                  </div>
                  <div class="mock-search">🔍 搜索</div>
                </div>

                <!-- 快捷入口（小程序中固定在头图下方） -->
                <div v-if="quickNavItems.length" class="mock-card quick-card">
                  <div class="mock-quick">
                    <div
                      v-for="(item, index) in quickNavItems"
                      :key="`qn-${index}`"
                      class="mock-quick-item"
                      @click.stop="select('module', quickNavIndex)"
                    >
                      <span class="mock-quick-icon">{{ iconText(item.icon) }}</span>
                      <span class="mock-quick-label">{{ item.title || '入口' }}</span>
                    </div>
                  </div>
                </div>

                <!-- 会员问候卡（真实小程序登录后展示，这里做示意） -->
                <div class="mock-card mock-greeting">
                  <div class="mock-greeting-top"><b>Hi，会员</b><span class="mock-chip" :style="{ color: design.theme.primary, background: design.theme.primary + '14' }">普通会员</span></div>
                  <div class="mock-stats"><span>0<small>张</small><em>优惠券</em></span><span>0<em>我的收藏</em></span><span>0<em>订单</em></span></div>
                </div>

                <div v-if="design.home.notice" class="mock-notice">📢 {{ design.home.notice }}</div>

                <!-- 优惠专区（小程序按进行中活动自动渲染） -->
                <div class="mock-card">
                  <div class="mock-section-title">优惠专区</div>
                  <div class="mock-promos">
                    <div class="mock-promo" :style="{ background: design.theme.primary + '12' }"><b :style="{ color: design.theme.primary }">满100减10</b><span>进行中</span></div>
                    <div class="mock-promo" :style="{ background: design.theme.primary + '12' }"><b :style="{ color: design.theme.primary }">领券中心</b><span>可领券</span></div>
                  </div>
                </div>

                <!-- 可拖拽排序的首页模块 -->
                <div class="module-dropzone" data-module-end>
                  <template v-for="(module, index) in design.home.modules" :key="moduleKey(module, index)">
                    <div v-if="module.type !== 'quickNav' && module.type !== 'banners'" class="module-wrap">
                      <div v-if="dragState.active && !dragState.outside && dragState.overIndex === index" class="drop-gap" />
                      <div
                        :class="['mock-module', isSelected('module', index) && 'on']"
                        :data-module-index="index"
                        @mousedown.prevent="startDrag($event, { kind: 'module', index })"
                        @click.stop="select('module', index)"
                      >
                        <div class="module-toolbar">
                          <span class="module-name">{{ moduleTitle(module) }}</span>
                          <span class="module-buttons" @mousedown.stop>
                            <el-button link size="small" :disabled="!index" @click.stop="moveModule(index, -1)">↑</el-button>
                            <el-button link size="small" :disabled="index === design.home.modules.length - 1" @click.stop="moveModule(index, 1)">↓</el-button>
                            <el-button link size="small" @click.stop="duplicateModule(index)">复制</el-button>
                            <el-button link size="small" type="danger" @click.stop="design.home.modules.splice(index, 1)">删</el-button>
                          </span>
                        </div>

                        <template v-if="module.type === 'hero'">
                          <div class="render-hero" :style="module.backgroundImage ? { backgroundImage: `url(${module.backgroundImage})` } : { background: heroGradient }">
                            <b>{{ module.title || design.home.appName }}</b>
                            <span>{{ module.subtitle || design.home.slogan }}</span>
                          </div>
                        </template>
                        <template v-else-if="module.type === 'notice'">
                          <div class="render-notice">📢 {{ module.text || '公告内容' }}</div>
                        </template>
                        <template v-else-if="module.type === 'categories'">
                          <div class="render-title">{{ module.title || '精选分类' }}</div>
                          <div class="render-chips">
                            <span v-for="category in previewCategories.slice(0, Number(module.limit || 8))" :key="category.id" :style="{ background: design.theme.primary + '12', color: design.theme.primary }">{{ category.name }}</span>
                            <span v-if="!previewCategories.length" class="muted">分类加载中…</span>
                          </div>
                        </template>
                        <template v-else-if="module.type === 'products'">
                          <div class="render-title">{{ module.title || '为你推荐' }}</div>
                          <div v-if="module.layout === 'list'" class="render-products list">
                            <div v-for="item in previewProducts(module)" :key="item.id" class="render-product row">
                              <img :src="item.image" alt="" /><div><b>{{ item.name }}</b><em>¥{{ item.price }}</em></div>
                            </div>
                          </div>
                          <div v-else :class="['render-products', module.layout === 'grid3' ? 'grid3' : 'grid']">
                            <div v-for="item in previewProducts(module)" :key="item.id" class="render-product">
                              <img :src="item.image" alt="" /><b>{{ item.name }}</b><em>¥{{ item.price }}</em>
                            </div>
                          </div>
                        </template>
                      </div>
                    </div>
                  </template>
                  <div v-if="dragState.active && !dragState.outside && dragState.overIndex === design.home.modules.length" class="drop-gap" />
                  <div class="dropzone-hint" :class="dragState.active && 'on'">拖拽组件到这里添加模块</div>
                </div>
              </template>
            </div>

            <div class="tabbar">
              <span v-for="tab in tabItems" :key="tab.key" :style="{ color: tab.active ? design.theme.tabColor : '#8e8e93' }">
                <i>{{ tab.icon }}</i>{{ design.tabs[tab.key] || tab.label }}
              </span>
            </div>
          </div>
        </div>
        <div class="canvas-tip">预览为小程序实际渲染示意；拖动模块可排序，点击模块/权益/服务在右侧编辑属性。</div>
      </main>

      <!-- 右：属性面板 -->
      <aside class="panel right-panel">
        <template v-if="selection.kind === 'module' && selectedModule">
          <div class="panel-title">{{ moduleTitle(selectedModule) }}设置</div>
          <el-form label-width="72px" class="prop-form">
            <el-form-item label="模块标题"><el-input v-model="selectedModule.title" /></el-form-item>

            <template v-if="selectedModule.type === 'hero'">
              <el-form-item label="副标题"><el-input v-model="selectedModule.subtitle" /></el-form-item>
              <el-form-item label="背景图"><ImageInput v-model="selectedModule.backgroundImage" /></el-form-item>
            </template>

            <template v-if="selectedModule.type === 'notice'">
              <el-form-item label="公告内容"><el-input v-model="selectedModule.text" type="textarea" :rows="3" /></el-form-item>
            </template>

            <template v-if="selectedModule.type === 'banners'">
              <div class="prop-hint">首页轮播图会显示在首页顶部（大图 + 指示点）。</div>
              <div v-for="(banner, index) in selectedModule.items" :key="index" class="item-card">
                <div class="item-card-head"><b>图 {{ index + 1 }}</b>
                  <span>
                    <el-button link size="small" :disabled="!index" @click="moveItem(selectedModule.items, index, -1)">↑</el-button>
                    <el-button link size="small" :disabled="index === selectedModule.items.length - 1" @click="moveItem(selectedModule.items, index, 1)">↓</el-button>
                    <el-button link size="small" type="danger" @click="selectedModule.items.splice(index, 1)">删</el-button>
                  </span>
                </div>
                <ImageInput v-model="banner.image" />
                <el-input v-model="banner.title" placeholder="标题（可选）" class="mt8" />
                <LinkPicker v-model:linkType="banner.linkType" v-model:linkValue="banner.linkValue" :categories="previewCategories" />
              </div>
              <el-button @click="selectedModule.items.push({ image: '', title: '', linkType: 'products', linkValue: '' })">添加图片</el-button>
            </template>

            <template v-if="selectedModule.type === 'quickNav'">
              <div class="prop-hint">快捷入口固定展示在首页头图下方（每行 4 个）。</div>
              <div v-for="(item, index) in selectedModule.items" :key="index" class="item-card">
                <div class="item-card-head"><b>入口 {{ index + 1 }}</b>
                  <span>
                    <el-button link size="small" :disabled="!index" @click="moveItem(selectedModule.items, index, -1)">↑</el-button>
                    <el-button link size="small" :disabled="index === selectedModule.items.length - 1" @click="moveItem(selectedModule.items, index, 1)">↓</el-button>
                    <el-button link size="small" type="danger" @click="selectedModule.items.splice(index, 1)">删</el-button>
                  </span>
                </div>
                <div class="line"><IconInput v-model="item.icon" /><el-input v-model="item.title" placeholder="名称" maxlength="6" /></div>
                <LinkPicker v-model:linkType="item.linkType" v-model:linkValue="item.linkValue" :categories="previewCategories" />
              </div>
              <el-button @click="selectedModule.items.push({ icon: '🎁', title: '新品', linkType: 'products', linkValue: '' })">添加入口</el-button>
            </template>

            <template v-if="selectedModule.type === 'categories'">
              <el-form-item label="显示数量"><el-slider v-model="selectedModule.limit" :min="1" :max="20" show-input /></el-form-item>
            </template>

            <template v-if="selectedModule.type === 'products'">
              <el-form-item label="布局">
                <el-radio-group v-model="selectedModule.layout">
                  <el-radio-button value="grid">两列</el-radio-button>
                  <el-radio-button value="grid3">三列</el-radio-button>
                  <el-radio-button value="list">横滑</el-radio-button>
                </el-radio-group>
              </el-form-item>
              <el-form-item label="商品分类">
                <el-select v-model="selectedModule.categoryId" clearable filterable placeholder="全部分类" style="width:100%">
                  <el-option v-for="category in previewCategories" :key="category.id" :value="category.id" :label="category.name" />
                </el-select>
              </el-form-item>
              <el-form-item label="所属商户">
                <el-select v-model="selectedModule.merchantId" clearable filterable placeholder="全部商户" style="width:100%">
                  <el-option v-for="merchant in merchants" :key="merchant.id" :value="merchant.id" :label="merchant.merchantName" />
                </el-select>
              </el-form-item>
              <el-form-item label="显示数量"><el-slider v-model="selectedModule.limit" :min="1" :max="30" show-input /></el-form-item>
            </template>
          </el-form>
        </template>

        <template v-else-if="selection.kind === 'banner' && design.home.banners[selection.index]">
          <div class="panel-title">轮播图设置</div>
          <el-form label-width="72px" class="prop-form">
            <el-form-item label="图片"><ImageInput v-model="design.home.banners[selection.index].image" /></el-form-item>
            <el-form-item label="标题"><el-input v-model="design.home.banners[selection.index].title" placeholder="可选" /></el-form-item>
            <el-form-item label="跳转">
              <LinkPicker v-model:linkType="design.home.banners[selection.index].linkType" v-model:linkValue="design.home.banners[selection.index].linkValue" :categories="previewCategories" />
            </el-form-item>
          </el-form>
          <div class="prop-hint">轮播图显示在首页顶部；多张图会自动轮播并展示 1/N 指示。</div>
        </template>

        <template v-else-if="selection.kind === 'benefit'">
          <div class="panel-title">会员权益设置</div>
          <el-form label-width="72px" class="prop-form">
            <el-form-item label="图标"><IconInput v-model="design.profile.benefits[selection.index].icon" /></el-form-item>
            <el-form-item label="名称"><el-input v-model="design.profile.benefits[selection.index].title" maxlength="6" /></el-form-item>
            <el-form-item label="跳转">
              <LinkPicker v-model:linkType="design.profile.benefits[selection.index].linkType" v-model:linkValue="design.profile.benefits[selection.index].linkValue" :categories="previewCategories" />
            </el-form-item>
          </el-form>
        </template>

        <template v-else-if="selection.kind === 'service'">
          <div class="panel-title">我的服务设置</div>
          <el-form label-width="72px" class="prop-form">
            <el-form-item label="图标"><IconInput v-model="design.profile.services[selection.index].icon" /></el-form-item>
            <el-form-item label="名称"><el-input v-model="design.profile.services[selection.index].title" maxlength="8" /></el-form-item>
            <el-form-item label="跳转">
              <LinkPicker v-model:linkType="design.profile.services[selection.index].linkType" v-model:linkValue="design.profile.services[selection.index].linkValue" :categories="previewCategories" />
            </el-form-item>
          </el-form>
        </template>

        <div v-else class="panel-empty">
          <div class="panel-empty-icon">👈</div>
          <div>点击左侧组件添加模块</div>
          <div>点击预览中的模块 / 权益 / 服务编辑属性</div>
          <div class="panel-empty-tip">拖动预览中的模块或列表项可调整顺序</div>
        </div>
      </aside>
    </div>
  </el-card>
</template>

<script setup>
import { computed, defineComponent, h, onMounted, reactive, ref, watch } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import request from '@/api/request'
import { isHexColor } from '@/utils/validators'

// 后台域下无法直接加载小程序静态资源与网关相对路径：统一解析为可访问的绝对地址。
const USER_H5_BASE = import.meta.env.VITE_USER_H5_BASE || 'http://127.0.0.1:5174'
const GATEWAY_BASE = (import.meta.env.VITE_API_BASE || 'http://127.0.0.1:5008/gateway').replace(/\/gateway$/, '')
const UPLOAD_URL = `${import.meta.env.VITE_API_BASE || 'http://127.0.0.1:5008/gateway'}/files/Upload`
const uploadHeaders = () => ({ Authorization: `Bearer ${localStorage.getItem('admin_token') || ''}` })
const assetUrl = path => {
  const value = String(path || '')
  if (!value) return ''
  if (value.startsWith('http')) return value
  if (value.startsWith('/static/')) return USER_H5_BASE + value
  if (value.startsWith('/gateway/')) return GATEWAY_BASE + value
  return value
}

// ---------- 内联小组件：图片输入（支持上传）/ 图标输入 / 跳转选择 ----------
const ImageInput = defineComponent({
  props: { modelValue: { type: String, default: '' } },
  emits: ['update:modelValue'],
  setup(props, { emit }) {
    const inputRef = ref(null)
    const pick = () => inputRef.value?.click()
    const onChange = async event => {
      const file = event.target.files?.[0]
      event.target.value = ''
      if (!file) return
      const body = new FormData()
      body.append('file', file)
      const response = await fetch(UPLOAD_URL, { method: 'POST', headers: uploadHeaders(), body }).then(r => r.json()).catch(() => null)
      if (Number(response?.code) === 200 && response.data?.url) emit('update:modelValue', response.data.url)
      else ElMessage.error(response?.message || '上传失败')
    }
    return () => h('div', { class: 'upload-box', onClick: pick }, [
      props.modelValue
        ? h('img', { src: assetUrl(props.modelValue), class: 'upload-image' })
        : h('div', { class: 'upload-empty' }, [h('span', { class: 'upload-plus' }, '＋'), h('span', null, '点击上传')]),
      h('div', { class: 'upload-mask' }, [h('span', { class: 'upload-mask-icon' }, '⇪'), h('span', null, props.modelValue ? '更换图片' : '上传图片')]),
      h('input', { ref: inputRef, type: 'file', accept: 'image/*', class: 'hidden-input', onChange })
    ])
  }
})

const IconInput = defineComponent({
  props: { modelValue: { type: String, default: '' } },
  emits: ['update:modelValue'],
  setup(props, { emit }) {
    const inputRef = ref(null)
    const pick = () => inputRef.value?.click()
    const onChange = async event => {
      const file = event.target.files?.[0]
      event.target.value = ''
      if (!file) return
      const body = new FormData()
      body.append('file', file)
      const response = await fetch(UPLOAD_URL, { method: 'POST', headers: uploadHeaders(), body }).then(r => r.json()).catch(() => null)
      if (Number(response?.code) === 200 && response.data?.url) emit('update:modelValue', response.data.url)
      else ElMessage.error(response?.message || '上传失败')
    }
    const isImage = computed(() => String(props.modelValue || '').startsWith('/') || String(props.modelValue || '').startsWith('http'))
    return () => h('div', { class: 'icon-upload' }, [
      h('div', { class: 'upload-box small', onClick: pick }, [
        isImage.value
          ? h('img', { src: assetUrl(props.modelValue), class: 'upload-image contain' })
          : h('div', { class: 'upload-empty' }, [h('span', { class: 'upload-plus' }, props.modelValue || '＋')]),
        h('div', { class: 'upload-mask' }, [h('span', { class: 'upload-mask-icon' }, '⇪'), h('span', null, '上传')]),
        h('input', { ref: inputRef, type: 'file', accept: 'image/*', class: 'hidden-input', onChange })
      ]),
      h('span', { class: 'icon-hint' }, isImage.value ? '已上传图标' : 'emoji 或上传图标')
    ])
  }
})

const LinkPicker = defineComponent({
  props: { linkType: { type: String, default: 'products' }, linkValue: { type: String, default: '' }, categories: { type: Array, default: () => [] } },
  emits: ['update:linkType', 'update:linkValue'],
  setup(props, { emit }) {
    const linkTypes = [
      { value: 'products', label: '商品列表' }, { value: 'category', label: '分类页' }, { value: 'cart', label: '购物车' },
      { value: 'orders', label: '我的订单' }, { value: 'address', label: '收货地址' }, { value: 'coupon-center', label: '领券中心' },
      { value: 'coupons', label: '我的券包' }, { value: 'favorites', label: '我的收藏' }, { value: 'refresh', label: '刷新资料' }, { value: 'service', label: '联系客服' }
    ]
    return () => h('div', { class: 'link-picker' }, [
      h('select', { value: props.linkType, onChange: e => emit('update:linkType', e.target.value) },
        linkTypes.map(item => h('option', { value: item.value, selected: item.value === props.linkType }, item.label))),
      props.linkType === 'category'
        ? h('select', { value: props.linkValue, onChange: e => emit('update:linkValue', e.target.value) },
            [h('option', { value: '' }, '选择分类'), ...props.categories.map(c => h('option', { value: c.id, selected: String(c.id) === String(props.linkValue) }, c.name))])
        : h('input', { value: props.linkValue, placeholder: '参数（可选）', onInput: e => emit('update:linkValue', e.target.value) })
    ])
  }
})

// ---------- 状态 ----------
const platforms = ref([]); const platformId = ref(''); const saving = ref(false)
const version = ref(0); const isPublished = ref(false)
const activeTab = ref('home')
const selection = reactive({ kind: '', index: -1 })
const categories = ref([]); const merchants = ref([]); const productCache = ref({})
const currentPlatformName = ref('')

const design = ref(emptyDesign())

function emptyDesign() {
  return {
    theme: { primary: '#00c1a2', background: '#f5f5f5', tabColor: '#00c1a2' },
    home: {
      appName: '', slogan: '', notice: '',
      banners: [
        { image: '/static/banners/banner-1.png', title: '夏季洗护日用优惠', linkType: 'products', linkValue: '' },
        { image: '/static/banners/banner-2.png', title: '会员日狂欢', linkType: 'coupon-center', linkValue: '' },
        { image: '/static/banners/banner-3.png', title: '新品尝鲜', linkType: 'products', linkValue: '' }
      ],
      modules: []
    },
    profile: {
      benefits: [
        { icon: '/static/line-color/points.png', title: '积分回馈', linkType: 'coupons', linkValue: '' },
        { icon: '/static/line-color/benefit.png', title: '专属活动', linkType: 'coupon-center', linkValue: '' },
        { icon: '/static/line-color/star.png', title: '我的收藏', linkType: 'favorites', linkValue: '' },
        { icon: '/static/line-color/card.png', title: '更多权益', linkType: 'service', linkValue: '' }
      ],
      services: [
        { icon: '/static/line/order.png', title: '我的订单', linkType: 'orders', linkValue: '' },
        { icon: '/static/line/cart.png', title: '购物车', linkType: 'cart', linkValue: '' },
        { icon: '/static/line/record.png', title: '消费记录', linkType: 'orders', linkValue: '' },
        { icon: '/static/line/gift.png', title: '我的活动', linkType: 'coupon-center', linkValue: '' },
        { icon: '/static/line/service.png', title: '客服帮助', linkType: 'service', linkValue: '' },
        { icon: '/static/line/review.png', title: '评价中心', linkType: 'service', linkValue: '' },
        { icon: '/static/line/points.png', title: '积分指南', linkType: 'coupons', linkValue: '' },
        { icon: '/static/line/pin.png', title: '收货地址', linkType: 'address', linkValue: '' },
        { icon: '/static/line/invoice.png', title: '发票信息', linkType: 'service', linkValue: '' },
        { icon: '/static/line/mall.png', title: '关于我们', linkType: 'service', linkValue: '' }
      ]
    },
    tabs: { home: '首页', category: '商城', cart: '购物车', profile: '我的' }
  }
}

const palette = [
  { type: 'hero', icon: '🎨', label: '品牌头', desc: '主题渐变/背景图 + 标题' },
  { type: 'quickNav', icon: '⚡', label: '快捷入口', desc: '头图下方 4 列宫格' },
  { type: 'categories', icon: '🧭', label: '分类导航', desc: '横向分类 chips' },
  { type: 'products', icon: '🛍️', label: '商品推荐', desc: '两列/三列/横滑' },
  { type: 'notice', icon: '📢', label: '公告', desc: '一行滚动公告' }
]

const makeModule = type => ({
  hero: { type, title: '', subtitle: '', backgroundImage: '' },
  notice: { type, title: '', text: '' },
  banners: { type, title: '', items: [] },
  quickNav: { type, title: '快捷入口', items: [] },
  categories: { type, title: '精选分类', limit: 8 },
  products: { type, key: `section-${Date.now()}`, title: '为你推荐', layout: 'grid', categoryId: '', merchantId: '', limit: 10 }
}[type])

const moduleTitle = module => ({ hero: '品牌头', notice: '公告', banners: '首页轮播图', quickNav: '快捷入口', categories: '分类导航', products: '商品推荐' }[module?.type] || '模块')
const moduleKey = (module, index) => `${module.type}-${module.key || index}`
// 图标示意：emoji 原样展示；小程序静态图标路径按文件名映射为相近 emoji，保证预览可读。
const ICON_EMOJI = { order: '📦', cart: '🛒', record: '🧾', gift: '🎁', service: '💬', heart: '⭐', card: '🎫', pin: '📍', invoice: '🧾', info: 'ℹ️', points: '✨', benefit: '🎉', star: '⭐', user: '👤', home: '🏠', category: '🧭' }
const iconText = icon => {
  const value = String(icon || '')
  if (!value) return '▦'
  if (!value.startsWith('/')) return value
  const name = value.split('/').pop().replace(/\.(png|jpg|jpeg|webp|gif)$/i, '')
  return ICON_EMOJI[name] || '▦'
}
const isSelected = (kind, index) => selection.kind === kind && selection.index === index
const select = (kind, index) => { selection.kind = kind; selection.index = index }
const selectedModule = computed(() => (selection.kind === 'module' ? design.value.home.modules[selection.index] : null))

const quickNavIndex = computed(() => design.value.home.modules.findIndex(module => module.type === 'quickNav'))
const quickNavItems = computed(() => (quickNavIndex.value >= 0 ? design.value.home.modules[quickNavIndex.value].items || [] : []))
const heroImages = computed(() => {
  const banners = design.value.home.banners || []
  if (banners.length) return banners.map(item => item.image).filter(Boolean)
  const module = design.value.home.modules.find(item => item.type === 'banners')
  return (module?.items || []).map(item => item.image).filter(Boolean)
})
const heroImage = computed(() => heroImages.value[0] || '')
const heroGradient = computed(() => `linear-gradient(135deg, ${design.value.theme.primary} 0%, ${design.value.theme.primary}D9 52%, ${design.value.theme.primary}A6 100%)`)
const heroStyle = computed(() => (heroImage.value ? { background: '#e8e8ed' } : { background: heroGradient.value }))
const tierGradient = computed(() => `linear-gradient(120deg, #57c7c2 0%, ${design.value.theme.primary}B3 48%, #8f7ae5 100%)`)
const tabItems = computed(() => [
  { key: 'home', label: '首页', icon: '🏠', active: true },
  { key: 'category', label: '分类', icon: '🧭', active: false },
  { key: 'cart', label: '购物车', icon: '🛒', active: false },
  { key: 'profile', label: '我的', icon: '👤', active: false }
])
const previewCategories = computed(() => categories.value)

// 商品预览：按模块配置取真实商品（带缓存，避免重复请求）。
const previewProducts = module => {
  const key = `${module.categoryId || 0}|${module.merchantId || 0}|${module.limit || 10}`
  return productCache.value[key] || []
}
const loadPreviewProducts = async () => {
  const cache = { ...productCache.value }
  for (const module of design.value.home.modules.filter(item => item.type === 'products')) {
    const key = `${module.categoryId || 0}|${module.merchantId || 0}|${module.limit || 10}`
    if (cache[key]) continue
    const data = await request.get('/products/List', {
      params: { categoryId: module.categoryId || 0, merchantId: module.merchantId || 0, status: 1, page: 1, pageSize: module.limit || 10 }
    }).catch(() => null)
    cache[key] = (data?.items || []).map(item => {
      const sku = (item.skus || []).filter(s => s.isActive !== false).sort((a, b) => Number(a.price) - Number(b.price))[0]
      return { id: item.id, name: item.name, image: item.mainImage, price: sku ? Number(sku.price).toFixed(2) : '--' }
    })
  }
  productCache.value = cache
}

// ---------- 指针拖拽（跟随幽灵 + 插入占位 + 拖出删除） ----------
const dragState = reactive({ active: false, kind: '', type: '', index: -1, x: 0, y: 0, overIndex: -1, overList: '', outside: false })

const ghostInfo = computed(() => {
  if (!dragState.active) return { icon: '', title: '' }
  if (dragState.kind === 'palette') return { icon: palette.find(item => item.type === dragState.type)?.icon || '🧩', title: moduleTitle({ type: dragState.type }) }
  if (dragState.kind === 'module') return { icon: '🧩', title: moduleTitle(design.value.home.modules[dragState.index]) }
  const list = dragState.kind === 'benefit' ? design.value.profile.benefits : design.value.profile.services
  return { icon: iconText(list[dragState.index]?.icon), title: list[dragState.index]?.title || '未命名' }
})

// 开始拖拽：调色板（放入）、画布模块（排序/拖出删除）、我的页条目（排序/拖出删除）。
const startDrag = (event, payload) => {
  if (event.button !== 0) return
  Object.assign(dragState, {
    active: true, kind: payload.kind, type: payload.type || '', index: payload.index ?? -1,
    x: event.clientX, y: event.clientY, overIndex: -1, overList: '', outside: false
  })
  window.addEventListener('mousemove', onDragMove)
  window.addEventListener('mouseup', onDragEnd)
  document.body.style.userSelect = 'none'
}

const onDragMove = event => {
  if (!dragState.active) return
  dragState.x = event.clientX; dragState.y = event.clientY
  const element = document.elementFromPoint(event.clientX, event.clientY)
  const phone = document.querySelector('.phone-screen')
  const insidePhone = Boolean(phone && element && phone.contains(element))

  if (dragState.kind === 'palette' || dragState.kind === 'module') {
    dragState.outside = dragState.kind === 'module' && !insidePhone
    const target = element?.closest?.('[data-module-index], [data-module-end]')
    if (target && insidePhone) {
      const end = target.hasAttribute('data-module-end')
      const index = end ? design.value.home.modules.length : Number(target.dataset.moduleIndex)
      const rect = target.getBoundingClientRect()
      dragState.overIndex = (!end && event.clientY > rect.top + rect.height / 2) ? index + 1 : index
    } else if (insidePhone) {
      dragState.overIndex = design.value.home.modules.length
    } else {
      dragState.overIndex = -1
    }
    return
  }

  // 我的页条目：仅允许在同一列表内排序，拖出预览删除。
  dragState.outside = !insidePhone
  const listTarget = element?.closest?.('[data-profile-list]')
  if (listTarget && insidePhone) {
    dragState.overList = listTarget.dataset.profileList
    const item = element.closest('[data-item-index]')
    if (item) {
      const index = Number(item.dataset.itemIndex)
      const rect = item.getBoundingClientRect()
      dragState.overIndex = event.clientY > rect.top + rect.height / 2 ? index + 1 : index
    } else {
      dragState.overIndex = design.value.profile[listTarget.dataset.profileList].length
    }
  } else {
    dragState.overList = ''; dragState.overIndex = -1
  }
}

const onDragEnd = () => {
  window.removeEventListener('mousemove', onDragMove)
  window.removeEventListener('mouseup', onDragEnd)
  document.body.style.userSelect = ''
  const { kind, type, index, overIndex, overList, outside } = dragState
  dragState.active = false
  dragState.overIndex = -1; dragState.overList = ''; dragState.outside = false

  const modules = design.value.home.modules
  if (kind === 'palette') {
    if (overIndex < 0) return
    modules.splice(overIndex, 0, makeModule(type))
    select('module', overIndex)
    ElMessage.success(`已添加${moduleTitle(modules[overIndex])}`)
    return
  }
  if (kind === 'module') {
    if (outside) {
      modules.splice(index, 1)
      if (selection.kind === 'module') { selection.kind = ''; selection.index = -1 }
      ElMessage.success('已移除模块')
      return
    }
    if (overIndex < 0) return
    const target = index < overIndex ? overIndex - 1 : overIndex
    if (target === index) return
    const [moved] = modules.splice(index, 1)
    modules.splice(target, 0, moved)
    select('module', target)
    return
  }
  if (kind === 'benefit' || kind === 'service') {
    const key = kind === 'benefit' ? 'benefits' : 'services'
    const items = design.value.profile[key]
    if (outside) {
      items.splice(index, 1)
      if (selection.kind === kind) { selection.kind = ''; selection.index = -1 }
      ElMessage.success('已移除')
      return
    }
    if (overList !== key || overIndex < 0) return
    const target = index < overIndex ? overIndex - 1 : overIndex
    if (target === index) return
    const [moved] = items.splice(index, 1)
    items.splice(target, 0, moved)
    select(kind, target)
  }
}

const linkTypeText = value => ({
  products: '商品列表', category: '分类页', cart: '购物车', orders: '我的订单', address: '收货地址',
  'coupon-center': '领券中心', coupons: '我的券包', favorites: '我的收藏', refresh: '刷新资料', service: '联系客服'
}[value] || '未设置')
const addBanner = () => {
  design.value.home.banners.push({ image: '', title: '', linkType: 'products', linkValue: '' })
  select('banner', design.value.home.banners.length - 1)
}
const moveBanner = (index, offset) => {
  const list = design.value.home.banners
  const target = index + offset
  if (target < 0 || target >= list.length) return
  ;[list[index], list[target]] = [list[target], list[index]]
  select('banner', target)
}
const selectBanner = () => {
  if (design.value.home.banners.length) select('banner', 0)
  else if (quickNavIndex.value >= 0) select('module', quickNavIndex.value)
}
const appendModule = type => {
  design.value.home.modules.push(makeModule(type))
  selection.kind = 'module'; selection.index = design.value.home.modules.length - 1
}
const moveModule = (index, offset) => {
  const modules = design.value.home.modules
  const target = index + offset
  if (target < 0 || target >= modules.length) return
  ;[modules[index], modules[target]] = [modules[target], modules[index]]
  selection.index = target
}
const duplicateModule = index => {
  const copy = JSON.parse(JSON.stringify(design.value.home.modules[index]))
  if (copy.type === 'products') copy.key = `section-${Date.now()}`
  design.value.home.modules.splice(index + 1, 0, copy)
  selection.index = index + 1
}
const moveItem = (items, index, offset) => {
  const target = index + offset
  if (target < 0 || target >= items.length) return
  ;[items[index], items[target]] = [items[target], items[index]]
}
const moveProfileItem = (list, index, offset) => {
  moveItem(design.value.profile[list], index, offset)
  select(list === 'benefits' ? 'benefit' : 'service', index + offset)
}
const addProfileItem = list => {
  if (list === 'benefits') {
    design.value.profile.benefits.push({ icon: '🎁', title: '新权益', linkType: 'coupons', linkValue: '' })
    select('benefit', design.value.profile.benefits.length - 1)
  } else {
    design.value.profile.services.push({ icon: '⭐', title: '新服务', linkType: 'orders', linkValue: '' })
    select('service', design.value.profile.services.length - 1)
  }
  activeTab.value = 'profile'
}

// ---------- 加载 / 保存 ----------
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
  design.value.profile = { ...emptyDesign().profile, ...(data.design?.profile || {}) }
  design.value.tabs = { ...emptyDesign().tabs, ...(data.design?.tabs || {}) }
  design.value.home.banners = data.design?.home?.banners || []
  design.value.home.modules = data.design?.home?.modules || [makeModule('quickNav'), makeModule('categories'), makeModule('products')]
  if (!design.value.profile.services.length) design.value.profile.services = emptyDesign().profile.services
  if (!design.value.profile.benefits.length) design.value.profile.benefits = emptyDesign().profile.benefits
  version.value = data.version || 0
  isPublished.value = Boolean(data.isPublished)
  currentPlatformName.value = data.platform?.platformName || ''
  selection.kind = ''; selection.index = -1
  productCache.value = {}
  loadPreviewProducts()
}

// 与后端/小程序一致的强校验：必填/长度/颜色/链接参数/模块数量。
const validateDesign = () => {
  const home = design.value.home || {}
  const tabs = design.value.tabs || {}
  const theme = design.value.theme || {}
  if (!String(home.appName || '').trim()) return '请在「主题与标签」填写商城名称'
  if (String(home.appName).length > 20) return '商城名称不能超过20个字符'
  if (String(home.slogan || '').length > 40) return '副标题不能超过40个字符'
  if (String(home.notice || '').length > 200) return '公告不能超过200个字符'
  if (![theme.primary, theme.background, theme.tabColor].every(isHexColor)) return '主题颜色必须是 #RRGGBB 格式'
  if (['home', 'category', 'cart', 'profile'].some(key => !String(tabs[key] || '').trim() || String(tabs[key]).length > 8))
    return '底部标签文案必填且不超过8个字符'
  const banners = home.banners || []
  if (banners.some(item => !String(item.image || '').trim())) return '首页轮播图图片地址不能为空'
  if (banners.some(item => ['category', 'products'].includes(item.linkType) && !String(item.linkValue || '').trim())) return '轮播图选择分类/商品列表时必须填写参数'
  const services = design.value.profile?.services || []
  if (services.some(item => !String(item.title || '').trim())) return '我的服务名称不能为空'
  if (services.some(item => String(item.title).length > 8)) return '我的服务名称不能超过8个字'
  if (services.some(item => String(item.icon || '').length > 64)) return '我的服务图标请使用图标路径或 1 个 emoji'
  if (services.some(item => ['category', 'products'].includes(item.linkType) && !String(item.linkValue || '').trim())) return '我的服务跳分类/商品时必须填写参数'
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
    ElMessage.success(publish ? '已发布' : '已保存')
    load()
  } finally { saving.value = false }
}

// 预览数据：分类树 + 商户列表（选择器与预览用）。
const loadPreviewData = async () => {
  const [tree, merchantData] = await Promise.all([
    request.get('/products/GetCategoryTree').catch(() => []),
    request.get('/merchants/List', { params: { page: 1, pageSize: 100 } }).catch(() => ({ items: [] }))
  ])
  const flat = []
  const walk = items => (items || []).forEach(item => { if (item.isActive !== false) { flat.push({ id: item.id, name: item.name }); walk(item.children) } })
  walk(tree)
  categories.value = flat
  merchants.value = merchantData?.items || []
}

let previewTimer = null
watch(() => design.value.home.modules, () => {
  clearTimeout(previewTimer)
  previewTimer = setTimeout(loadPreviewProducts, 400)
}, { deep: true })

onMounted(async () => { await Promise.all([loadPlatforms(), loadPreviewData()]); load() })
</script>

<style scoped>
.header-row { display: flex; align-items: center; justify-content: space-between; gap: 12px; }
.title-group { display: flex; align-items: center; gap: 10px; font-weight: 600; }
.toolbar { display: flex; align-items: center; gap: 10px; }
.editor { display: grid; grid-template-columns: 300px 1fr 340px; gap: 16px; align-items: start; }
.panel { background: #fff; border: 1px solid var(--el-border-color-lighter); border-radius: 14px; padding: 14px; }
.hint { color: #86868b; font-size: 12px; line-height: 1.6; margin-bottom: 12px; }
.banner-list { display: grid; gap: 8px; margin-bottom: 16px; }
.banner-item { display: flex; align-items: center; gap: 10px; padding: 8px; border: 1px solid var(--el-border-color-lighter); border-radius: 10px; cursor: pointer; background: #fafafc; }
.banner-item.on { border-color: var(--el-color-primary); background: var(--el-color-primary-light-9); }
.banner-thumb { width: 64px; height: 42px; border-radius: 6px; object-fit: cover; background: #f2f2f4; flex-shrink: 0; }
.banner-info { flex: 1; min-width: 0; }
.banner-title { font-size: 12px; font-weight: 600; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.banner-link { font-size: 11px; color: #a1a1a6; }
.banner-empty { color: #a1a1a6; font-size: 12px; padding: 10px; border: 1px dashed var(--el-border-color); border-radius: 10px; text-align: center; }
.palette { display: grid; gap: 10px; }
.palette-item { display: flex; align-items: center; gap: 10px; padding: 10px 12px; border: 1px dashed var(--el-border-color); border-radius: 12px; cursor: grab; transition: all .15s ease; background: #fafafc; }
.palette-item:hover { border-color: var(--el-color-primary); background: var(--el-color-primary-light-9); transform: translateY(-1px); }
.palette-item:active { cursor: grabbing; }
.palette-icon { font-size: 20px; }
.palette-name { font-size: 13px; font-weight: 600; }
.palette-desc { font-size: 11px; color: #a1a1a6; }
.section-head { display: flex; align-items: center; justify-content: space-between; margin: 14px 0 8px; font-size: 13px; font-weight: 600; }
.mini-list { display: grid; gap: 8px; }
.mini-item { display: flex; align-items: center; gap: 8px; padding: 8px 10px; border: 1px solid var(--el-border-color-lighter); border-radius: 10px; cursor: grab; background: #fafafc; }
.mini-item.on { border-color: var(--el-color-primary); background: var(--el-color-primary-light-9); }
.mini-icon { width: 22px; text-align: center; }
.mini-title { flex: 1; font-size: 12px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.mini-actions { display: flex; align-items: center; }
.theme-form { margin-top: 8px; }
.tab-inputs { display: grid; grid-template-columns: 1fr 1fr; gap: 8px; width: 100%; }
.mt8 { margin-top: 8px; } .mt12 { margin-top: 12px; }

/* 手机预览 */
.canvas { display: grid; justify-items: center; gap: 10px; }
.phone { width: 390px; height: 780px; background: #111; border-radius: 40px; padding: 12px; box-shadow: 0 24px 60px rgba(0, 0, 0, .22); position: relative; }
.phone-notch { position: absolute; top: 14px; left: 50%; transform: translateX(-50%); width: 120px; height: 22px; background: #111; border-radius: 0 0 14px 14px; z-index: 3; }
.phone-screen { width: 100%; height: 100%; border-radius: 30px; overflow: hidden; display: flex; flex-direction: column; position: relative; }
.status-bar { display: flex; justify-content: space-between; padding: 10px 22px 6px; font-size: 12px; font-weight: 600; }
.screen-scroll { flex: 1; overflow-y: auto; padding-bottom: 12px; }
.mock-hero { position: relative; height: 220px; display: grid; place-items: center; }
.mock-hero-image { position: absolute; inset: 0; width: 100%; height: 100%; object-fit: cover; }
.mock-hero-topbar { position: absolute; top: 12px; left: 14px; background: rgba(0,0,0,.28); color: #fff; font-size: 11px; padding: 3px 10px; border-radius: 999px; }
.mock-hero-text { text-align: center; color: #fff; position: relative; }
.mock-hero-title { font-size: 24px; font-weight: 800; }
.mock-hero-sub { font-size: 12px; opacity: .85; margin-top: 6px; }
.mock-hero-dots { position: absolute; bottom: 34px; display: flex; gap: 4px; }
.mock-hero-dots i { width: 6px; height: 6px; border-radius: 50%; background: rgba(255,255,255,.5); }
.mock-hero-dots i.on { background: #fff; }
.mock-search { position: absolute; bottom: -16px; left: 16px; right: 16px; height: 34px; background: rgba(255,255,255,.96); border-radius: 999px; display: flex; align-items: center; padding: 0 14px; color: #86868b; font-size: 12px; box-shadow: 0 6px 16px rgba(0,0,0,.12); }
.mock-card { background: #fff; border-radius: 14px; margin: 12px 12px 0; padding: 12px; box-shadow: 0 4px 14px rgba(0,0,0,.05); }
.quick-card { margin-top: 26px; }
.mock-quick { display: grid; grid-template-columns: repeat(4, 1fr); gap: 10px 6px; }
.mock-quick-item { display: grid; justify-items: center; gap: 4px; cursor: pointer; }
.mock-quick-icon { width: 38px; height: 38px; display: grid; place-items: center; background: #f5f5f7; border-radius: 12px; font-size: 18px; }
.mock-quick-label { font-size: 10px; color: #6e6e73; }
.mock-greeting-top { display: flex; align-items: center; justify-content: space-between; font-size: 14px; }
.mock-chip { font-size: 10px; padding: 2px 8px; border-radius: 999px; }
.mock-stats { display: flex; justify-content: space-around; margin-top: 10px; text-align: center; color: #1d1d1f; }
.mock-stats span { font-size: 15px; font-weight: 700; display: grid; }
.mock-stats small { font-size: 9px; font-weight: 500; }
.mock-stats em { font-size: 10px; color: #86868b; font-style: normal; font-weight: 400; margin-top: 2px; }
.mock-notice { margin: 12px 12px 0; background: #fff; border-radius: 12px; padding: 9px 12px; font-size: 11px; color: #6e6e73; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.mock-section-title { font-size: 13px; font-weight: 700; margin-bottom: 8px; }
.mock-promos { display: flex; gap: 8px; }
.mock-promo { flex: 1; border-radius: 10px; padding: 8px 10px; display: grid; gap: 2px; font-size: 11px; color: #6e6e73; }
.mock-promo b { font-size: 13px; }
.module-dropzone { min-height: 60px; padding-bottom: 8px; }
.module-wrap { position: relative; }
.drop-gap { height: 10px; margin: 8px 12px; border-radius: 6px; background: repeating-linear-gradient(90deg, var(--el-color-primary) 0 10px, transparent 10px 20px); opacity: .55; animation: gap-in .15s ease; }
@keyframes gap-in { from { transform: scaleY(.3); opacity: 0; } to { transform: scaleY(1); opacity: .55; } }
.phone-screen.drag-in { outline: 2px dashed var(--el-color-primary); outline-offset: -4px; }
.phone-screen.drag-out { outline: 2px dashed #ff3b30; outline-offset: -4px; }
.drag-out-hint { position: absolute; left: 14px; right: 14px; bottom: 74px; z-index: 6; background: rgba(255, 59, 48, .94); color: #fff; font-size: 12px; text-align: center; padding: 8px; border-radius: 10px; }
.drag-ghost { position: fixed; z-index: 9999; pointer-events: none; display: flex; align-items: center; gap: 8px; padding: 8px 14px; background: rgba(255, 255, 255, .97); border: 1px solid var(--el-color-primary); border-radius: 10px; box-shadow: 0 12px 32px rgba(0, 0, 0, .18); font-size: 12px; font-weight: 600; }
.mock-module { margin: 12px 12px 0; background: #fff; border-radius: 14px; padding: 10px 12px 12px; box-shadow: 0 4px 14px rgba(0,0,0,.05); border: 2px solid transparent; cursor: grab; position: relative; }
.mock-module:hover { border-color: var(--el-color-primary-light-5); }
.mock-module.on { border-color: var(--el-color-primary); }
.module-toolbar { display: flex; align-items: center; justify-content: space-between; margin-bottom: 6px; }
.module-name { font-size: 11px; font-weight: 700; color: #6e6e73; }
.module-buttons { display: flex; align-items: center; }
.dropzone-hint { margin: 12px; border: 1px dashed var(--el-border-color); border-radius: 12px; padding: 14px; text-align: center; color: #a1a1a6; font-size: 12px; }
.dropzone-hint.on { border-color: var(--el-color-primary); color: var(--el-color-primary); background: var(--el-color-primary-light-9); }
.render-hero { height: 110px; border-radius: 10px; display: grid; place-items: center; color: #fff; background-size: cover; background-position: center; }
.render-hero b { font-size: 16px; } .render-hero span { font-size: 11px; opacity: .85; }
.render-notice { font-size: 12px; color: #6e6e73; }
.render-title { font-size: 13px; font-weight: 700; margin-bottom: 8px; }
.render-chips { display: flex; flex-wrap: wrap; gap: 6px; }
.render-chips span { font-size: 11px; padding: 4px 10px; border-radius: 999px; }
.muted { color: #a1a1a6; font-size: 11px; }
.render-products.grid { display: grid; grid-template-columns: 1fr 1fr; gap: 8px; }
.render-products.grid3 { display: grid; grid-template-columns: 1fr 1fr 1fr; gap: 6px; }
.render-products.list { display: flex; gap: 8px; overflow-x: auto; }
.render-product { display: grid; gap: 3px; }
.render-product img { width: 100%; height: 74px; object-fit: cover; border-radius: 8px; background: #f5f5f7; }
.render-product b { font-size: 11px; font-weight: 500; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.render-product em { font-size: 11px; color: #ff3b30; font-style: normal; font-weight: 700; }
.render-product.row { min-width: 150px; }
.render-product.row img { height: 70px; }

/* 我的页预览 */
.mock-profile-head { display: flex; align-items: center; gap: 12px; padding: 20px 16px 12px; }
.mock-avatar { width: 54px; height: 54px; border-radius: 50%; display: grid; place-items: center; color: #fff; font-weight: 800; background: linear-gradient(135deg, #0a84ff, #5e5ce6); }
.mock-name { font-size: 16px; font-weight: 700; } .mock-sub { font-size: 11px; color: #86868b; }
.mock-tier { margin: 0 12px; border-radius: 14px; padding: 14px; color: #fff; }
.mock-tier-name { font-size: 14px; font-weight: 700; }
.mock-tier-bar { margin-top: 10px; height: 5px; background: rgba(255,255,255,.35); border-radius: 999px; overflow: hidden; }
.mock-tier-bar i { display: block; height: 100%; border-radius: 999px; }
.mock-benefits { display: grid; grid-template-columns: repeat(4, 1fr); gap: 8px; }
.mock-benefit { display: grid; justify-items: center; gap: 4px; padding: 6px 2px; border-radius: 10px; border: 1px solid transparent; cursor: pointer; }
.mock-benefit.on, .mock-service.on { border-color: var(--el-color-primary); background: var(--el-color-primary-light-9); }
.mock-benefit-icon { font-size: 20px; } .mock-benefit-label { font-size: 10px; color: #6e6e73; }
.mock-services { display: grid; grid-template-columns: repeat(5, 1fr); gap: 10px 4px; }
.mock-service { display: grid; justify-items: center; gap: 4px; padding: 6px 2px; border-radius: 10px; border: 1px solid transparent; cursor: pointer; }
.mock-service-icon { font-size: 18px; } .mock-service-label { font-size: 10px; color: #6e6e73; }
.tabbar { display: flex; border-top: 1px solid rgba(60,60,67,.08); background: #fff; padding: 8px 0 12px; }
.tabbar span { flex: 1; display: grid; justify-items: center; gap: 2px; font-size: 10px; }
.tabbar i { font-style: normal; font-size: 16px; }
.canvas-tip { color: #a1a1a6; font-size: 12px; }

/* 属性面板 */
.panel-title { font-weight: 700; margin-bottom: 12px; }
.prop-hint { color: #86868b; font-size: 12px; margin-bottom: 10px; }
.item-card { border: 1px solid var(--el-border-color-lighter); border-radius: 12px; padding: 10px; margin-bottom: 10px; background: #fafafc; }
.item-card-head { display: flex; align-items: center; justify-content: space-between; margin-bottom: 8px; font-size: 12px; }
.line { display: flex; align-items: center; gap: 8px; margin-bottom: 8px; }
.panel-empty { display: grid; gap: 8px; justify-items: center; color: #a1a1a6; font-size: 12px; padding: 60px 10px; text-align: center; }
.panel-empty-icon { font-size: 28px; }
.panel-empty-tip { color: #c7c7cc; }
/* 固定尺寸上传框 + 悬浮玻璃层（与商品编辑一致；子组件内部元素需 :deep 穿透 scoped） */
:deep(.upload-box) { position: relative; width: 120px; height: 120px; border: 1px dashed var(--el-border-color); border-radius: 12px; overflow: hidden; cursor: pointer; background: #fafafc; display: grid; place-items: center; flex-shrink: 0; }
:deep(.upload-box.small) { width: 120px; height: 120px; }
:deep(.upload-image) { width: 100%; height: 100%; object-fit: cover; display: block; }
:deep(.upload-image.contain) { object-fit: contain; padding: 8px; box-sizing: border-box; }
:deep(.upload-empty) { display: grid; justify-items: center; gap: 2px; color: #a1a1a6; font-size: 11px; }
:deep(.upload-plus) { font-size: 20px; line-height: 1; }
:deep(.upload-mask) { position: absolute; inset: 0; display: grid; place-items: center; gap: 2px; align-content: center; color: #fff; font-size: 11px; background: rgba(0, 0, 0, .42); backdrop-filter: blur(6px); opacity: 0; transition: opacity .18s ease; }
:deep(.upload-box:hover .upload-mask) { opacity: 1; }
:deep(.upload-mask-icon) { font-size: 18px; line-height: 1; }
:deep(.hidden-input) { display: none; }
:deep(.icon-upload) { display: flex; align-items: center; gap: 10px; }
:deep(.icon-hint) { font-size: 11px; color: #a1a1a6; }
:deep(.link-picker) { display: flex; gap: 8px; width: 100%; }
:deep(.link-picker select), :deep(.link-picker input) { flex: 1; height: 32px; border: 1px solid var(--el-border-color); border-radius: 8px; padding: 0 8px; font-size: 12px; background: #fff; min-width: 0; }
</style>
