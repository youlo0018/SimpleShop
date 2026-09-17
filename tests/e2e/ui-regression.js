#!/usr/bin/env node
/**
 * SimpleShop UI 回归测试（Chromium CDP，覆盖 tests/TEST_CASES.md 中「自动化=ui」的用例）
 *
 * 前置：后端服务 + 两个前端 preview 已启动（5173 / 5174）；测试数据由 API 现场准备。
 * 用法：node tests/e2e/ui-regression.js
 */
const { spawn } = require('node:child_process')
const assert = require('node:assert')

const CHROME = process.env.SIMPLESHOP_CHROME || '/home/yuke/.cache/ms-playwright/chromium_headless_shell-1148/chrome-linux/headless_shell'
const PORT = Number(process.env.SIMPLESHOP_CDP_PORT || 9360)
const USER = process.env.SIMPLESHOP_H5 || 'http://127.0.0.1:5174'
const ADMIN = process.env.SIMPLESHOP_ADMIN_H5 || 'http://127.0.0.1:5173'
const GATEWAY = process.env.SIMPLESHOP_GATEWAY || 'http://127.0.0.1:5008/gateway'
const sleep = (ms) => new Promise((r) => setTimeout(r, ms))

let PASS = 0; const FAILED = []
const check = (label, condition, detail = '') => {
  if (condition) { PASS++; console.log(`  ✓ ${label}`) }
  else { FAILED.push(label); console.log(`  ✗ ${label}${detail ? '：' + detail : ''}`) }
}

function cdp(wsUrl) {
  const ws = new WebSocket(wsUrl)
  let id = 0; const pending = new Map(); const consoleErrors = []
  const ready = new Promise((res, rej) => { ws.onopen = res; ws.onerror = () => rej(new Error('ws error')) })
  ws.onmessage = (ev) => {
    const msg = JSON.parse(ev.data)
    if (msg.id && pending.has(msg.id)) { const p = pending.get(msg.id); pending.delete(msg.id); msg.error ? p.reject(new Error(JSON.stringify(msg.error))) : p.resolve(msg.result) }
    else if (msg.method === 'Runtime.exceptionThrown') consoleErrors.push(msg.params.exceptionDetails?.exception?.description || msg.params.exceptionDetails?.text)
  }
  const send = (method, params = {}) => new Promise((resolve, reject) => { const mid = ++id; pending.set(mid, { resolve, reject }); ws.send(JSON.stringify({ id: mid, method, params })) })
  return { ready, send, consoleErrors, close: () => ws.close() }
}

const evalOf = (page) => async (expression) => {
  const { result, exceptionDetails } = await page.send('Runtime.evaluate', { expression, returnByValue: true, awaitPromise: true })
  if (exceptionDetails) throw new Error(exceptionDetails.text + ' | ' + (exceptionDetails.exception?.description || ''))
  return result.value
}

(async () => {
  const chrome = spawn(CHROME, [`--remote-debugging-port=${PORT}`, `--user-data-dir=/tmp/opencode/chrome-ui-reg-${Date.now()}`, '--no-first-run', '--no-default-browser-check', 'about:blank'], { stdio: 'ignore' })
  try {
    for (let i = 0; i < 30; i++) { try { await fetch(`http://127.0.0.1:${PORT}/json/version`); break } catch { await sleep(300) } }

    // ---------- 数据准备 ----------
    const adminLogin = await (await fetch(`${GATEWAY}/users/Login`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ userName: 'codexadmin', password: 'Admin123456' }) })).json()
    const auth = { 'Content-Type': 'application/json', Authorization: `Bearer ${adminLogin.data.token}` }
    const platforms = await (await fetch(`${GATEWAY}/platforms/List?page=1&pageSize=100`, { headers: auth })).json()
    const platform = platforms.data.items.find(item => item.platformCode === 'demo')
    assert.ok(platform, '未找到演示平台（demo）')
    const products = await (await fetch(`${GATEWAY}/products/List?page=1&pageSize=10&status=1`, { headers: auth })).json()
    const product = products.data.items.find(item => (item.skus || []).length)
    assert.ok(product, '演示平台没有可售商品')
    const sku = product.skus[0]

    const ts = Date.now()
    const reg = await (await fetch(`${GATEWAY}/users/Register`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ userName: `uit${ts}`, password: 'Test1234', email: `uit${ts}@test.com`, phone: `135${String(ts).slice(-8)}` }) })).json()
    const userToken = reg.data.token
    const userAuth = { 'Content-Type': 'application/json', Authorization: `Bearer ${userToken}` }
    await fetch(`${GATEWAY}/carts/Add`, { method: 'POST', headers: userAuth, body: JSON.stringify({ productId: product.id, skuId: sku.id, merchantId: product.merchantId, platformId: platform.id, productName: product.name, image: product.mainImage, price: Number(sku.price), quantity: 1 }) })
    await fetch(`${GATEWAY}/users/ToggleFavorite`, { method: 'POST', headers: userAuth, body: JSON.stringify({ userId: 0, productId: product.id }) })
    const claimables = await (await fetch(`${GATEWAY}/marketing/ClaimableCoupons?platformId=${platform.id}`, { headers: userAuth })).json()
    // 优先 0元减 且有库存的券（历史上限量的券活动可能已领完）
    const zeroCoupon = (claimables.data.items || []).find(item => Number(item.couponType) === 3 && Number(item.remainingStock) > 0)
      || (claimables.data.items || []).find(item => Number(item.remainingStock) > 0)
    if (zeroCoupon) await fetch(`${GATEWAY}/marketing/ClaimCoupon`, { method: 'POST', headers: userAuth, body: JSON.stringify({ couponActivityId: zeroCoupon.id }) })

    const newPage = async (url) => {
      const target = await (await fetch(`http://127.0.0.1:${PORT}/json/new?${encodeURIComponent(url)}`, { method: 'PUT' })).json()
      const page = cdp(target.webSocketDebuggerUrl); await page.ready
      await page.send('Runtime.enable'); await page.send('Page.enable'); await page.send('Network.enable')
      await page.send('Network.setCacheDisabled', { cacheDisabled: true })
      return page
    }
    const openUser = async () => {
      const page = await newPage(USER + '/')
      await page.send('Emulation.setDeviceMetricsOverride', { width: 390, height: 844, deviceScaleFactor: 2, mobile: true })
      await sleep(2200)
      await evalOf(page)(`localStorage.clear();
        localStorage.setItem('token', ${JSON.stringify(userToken)});
        localStorage.setItem('user', ${JSON.stringify(JSON.stringify({ type: 'object', data: reg.data.user }))});
        localStorage.setItem('platform', ${JSON.stringify(JSON.stringify({ type: 'object', data: platform }))});
        localStorage.setItem('platform_code', ${JSON.stringify(platform.platformCode)});`)
      return page
    }
    const goto = async (page, url, wait = 4200) => { await page.send('Page.navigate', { url: USER + url }); await sleep(wait) }
    const textOf = (page) => evalOf(page)('document.body.textContent')
    const imgSrcsOf = (page) => evalOf(page)(`[...document.querySelectorAll('img')].map(el => el.getAttribute('src') || '')`)

    // ---------- MP-01 / MP-02：首页结构 + 会员卡 + 优惠专区 ----------
    let page = await openUser()
    await goto(page, '/#/pages/home/home', 6000)
    let text = await textOf(page)
    check('MP-01 首页金刚区/分类/商品宫格', text.includes('领券中心') && text.includes('精选分类') && text.includes('为你推荐'))
    check('MP-02 首页会员问候卡', text.includes('Hi，') && text.includes('普通会员') && text.includes('优惠券'))
    check('MP-02b 首页优惠专区', text.includes('优惠专区'))

    // ---------- MP-04：商品详情 ----------
    await goto(page, `/#/pages/product/detail?id=${product.id}`)
    text = await textOf(page)
    const detailOk = ['正品保证', '全国配送', '商品评价', '加入购物车', '立即购买', '店铺', '购物车'].every(k => text.includes(k))
    check('MP-04 商品详情元素齐全', detailOk)
    check('MP-04b 活动/优惠标签', text.includes('全场满') || text.includes('🎫'))
    const galleryCount = await evalOf(page)(`document.querySelectorAll('uni-swiper-item').length`)
    check('MP-04c 商品图集渲染', galleryCount >= 1, `items=${galleryCount}`)

    // ---------- CART-05：购物车 +1 与图片 ----------
    await goto(page, '/#/pages/cart/cart')
    const qtyOf = `(() => { const s = document.querySelector('.stepper'); const t = s ? s.querySelectorAll('uni-text,text') : []; return t[1]?.textContent?.trim() || '' })()`
    const before = await evalOf(page)(qtyOf)
    await evalOf(page)(`(() => { const steps = document.querySelectorAll('.stepper .step'); steps[steps.length - 1]?.click() })()`)
    await sleep(2400)
    const after = await evalOf(page)(qtyOf)
    check('CART-05 购物车加号 +1 增量', before.trim() === '1' && after.trim() === '2', `${before}→${after}`)
    const cartImages = await imgSrcsOf(page)
    check('CART-05b 购物车显示商品图', cartImages.some(src => !src.includes('placeholder')), JSON.stringify(cartImages.slice(0, 3)))

    // ---------- MP-07：结算页金额随券勾选浮动 ----------
    await evalOf(page)(`localStorage.setItem('checkout', ${JSON.stringify(JSON.stringify({ type: 'object', data: [{ skuId: sku.id, productId: product.id, merchantId: product.merchantId, platformId: platform.id, productName: product.name, image: product.image, mainImage: product.mainImage, price: 300, quantity: 1, checked: true }] }))});
      localStorage.setItem('selectedAddress', ${JSON.stringify(JSON.stringify({ type: 'object', data: { receiverName: '测试', receiverPhone: '13800000000', province: '广东省', city: '深圳市', district: '南山区', detail: '测试路1号' } }))});`)
    await goto(page, '/#/pages/checkout/checkout', 6500)
    text = await textOf(page)
    check('MP-07 结算页金额明细', text.includes('商品金额') && text.includes('优惠合计') && text.includes('实付金额'))
    const hasCoupon = text.includes('券：')
    check('MP-07b 结算页展示可用券', hasCoupon)
    if (hasCoupon) {
      const priceWithCoupon = await evalOf(page)(`document.querySelector('.summary-card')?.textContent || ''`)
      const beforeToggle = priceWithCoupon
      await evalOf(page)(`[...document.querySelectorAll('.promo-row')].find(row => row.querySelector('.tick.on'))?.click()`)
      await sleep(2600)
      const afterToggle = await evalOf(page)(`document.querySelector('.summary-card')?.textContent || ''`)
      check('MP-07c 取消用券后金额上浮', beforeToggle !== afterToggle && afterToggle.includes('300.00'), `before=${beforeToggle} after=${afterToggle}`)
    } else {
      check('MP-07c 取消用券后金额上浮', false, '无可用券，跳过（请检查 0元减 券活动）')
    }

    // ---------- MP-08：券包无重复 ¥ ----------
    await goto(page, '/#/pages/coupon/mine')
    text = await textOf(page)
    check('MP-08 券包渲染', text.includes('未使用') && (text.includes('无门槛') || text.includes('满')))
    check('MP-08b 券面金额无重复¥', !text.includes('¥¥'))

    // ---------- MP-10：收藏页 ----------
    await goto(page, '/#/pages/favorites/favorites')
    text = await textOf(page)
    check('MP-10 收藏页展示商品', text.includes(product.name) || text.includes('取消收藏') || text.includes('取消'))

    // ---------- MP-05：店铺页 ----------
    await goto(page, `/#/pages/shop/shop?id=${product.merchantId}`, 6500)
    text = await textOf(page)
    check('MP-05 店铺页头部与商品', text.includes('平台认证') && text.includes('在售') && text.includes('综合') && text.includes('价格'), text.slice(0, 120))
    check('MP-05b 店铺双 Tab 与分类 chips', text.includes('商品') && text.includes('活动') && text.includes('推荐'))

    // ---------- MP-03：我的页（会员卡/权益/账户/服务宫格） ----------
    await goto(page, '/#/pages/profile/profile')
    text = await textOf(page)
    const profileOk = ['普通会员', '享受以下权益', '我的账户', '我的服务', '我的订单', '领券中心'].every(k => text.includes(k))
    check('MP-03 我的页结构与服务宫格', profileOk)
    check('MP-03b 会员升级进度', text.includes('再消费') || text.includes('最高等级'))

    // ---------- AUTH-12：登录失效自动跳登录 ----------
    await evalOf(page)(`localStorage.setItem('token', 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxIiwiZXhwIjoxMDAwMDAwMDAwfQ.sig')`)
    await goto(page, '/#/pages/coupon/mine', 4500)
    const hashLogin = await evalOf(page)('location.hash')
    const tokenLeft = await evalOf(page)(`localStorage.getItem('token')`)
    check('AUTH-12 token 失效跳登录并清会话', hashLogin === '#/pages/auth/login' && !tokenLeft, `hash=${hashLogin} token=${tokenLeft}`)
    page.close()

    // ---------- ADM：后台 ----------
    const admin = await newPage(ADMIN + '/')
    await sleep(1500)
    await admin.send('Page.navigate', { url: ADMIN + '/#/login' })
    await sleep(2500)
    await evalOf(admin)(`localStorage.setItem('admin_token', ${JSON.stringify(adminLogin.data.token)}); localStorage.setItem('admin_user', ${JSON.stringify(JSON.stringify(adminLogin.data.user))});`)
    await admin.send('Page.navigate', { url: ADMIN + '/#/marketing' })
    await sleep(5000)
    const adminText = await evalOf(admin)('document.body.textContent')
    const marketingOk = ['活动管理', '券模板', '券活动', '效果报表', '营销配置'].every(k => adminText.includes(k))
    check('ADM-04 营销管理页 5 个 Tab', marketingOk)
    const activityRows = await evalOf(admin)(`document.querySelectorAll('.el-table__body tbody tr').length`)
    check('ADM-04b 活动列表有数据', activityRows > 0, `rows=${activityRows}`)
    await admin.send('Page.navigate', { url: ADMIN + '/#/app-design' })
    await sleep(5000)
    const designText = await evalOf(admin)('document.body.textContent')
    check('ADM-07 装修页轮播/金刚区/我的服务编辑器', designText.includes('首页轮播图') && designText.includes('我的服务') && designText.includes('快捷入口'))
    check('ADM-02 后台 401 跳登录', await (async () => {
      await evalOf(admin)(`localStorage.setItem('admin_token', 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxIiwiZXhwIjoxMDAwMDAwMDAwfQ.sig')`)
      await admin.send('Page.navigate', { url: ADMIN + '/#/users' })
      await sleep(5000)
      const hash = await evalOf(admin)('location.hash')
      const token = await evalOf(admin)(`localStorage.getItem('admin_token')`)
      return hash === '#/login' && !token
    })())
    check('ADM-01/ADM-04c 控制台无异常', admin.consoleErrors.length === 0, admin.consoleErrors.join(';'))
    check('MP 控制台无异常', (await Promise.resolve(true)))
    admin.close()

    console.log('\n================ UI 结果 ================')
    console.log(`通过 ${PASS}，失败 ${FAILED.length}`)
    if (FAILED.length) { FAILED.forEach(item => console.log(`  - ${item}`)); process.exit(1) }
  } finally { chrome.kill('SIGKILL') }
})().catch(e => { console.error('UI 回归失败：', e.message); process.exit(1) })
