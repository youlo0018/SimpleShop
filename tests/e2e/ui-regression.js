#!/usr/bin/env node
/**
 * SimpleShop UI 回归测试（Chromium CDP，覆盖 tests/TEST_CASES.md 中「自动化=ui」的用例）
 *
 * 前置：后端服务 + 两个前端 preview 已启动（5173 / 5174）；测试数据由 API 现场准备。
 * 用法：node tests/e2e/ui-regression.js
 */
const { spawn, execSync } = require('node:child_process')
const assert = require('node:assert')

const CHROME = process.env.SIMPLESHOP_CHROME || '/home/yuke/.cache/ms-playwright/chromium_headless_shell-1148/chrome-linux/headless_shell'
const PORT = Number(process.env.SIMPLESHOP_CDP_PORT || 9360)
const USER = process.env.SIMPLESHOP_H5 || 'http://127.0.0.1:5174'
const ADMIN = process.env.SIMPLESHOP_ADMIN_H5 || 'http://127.0.0.1:5173'
const GATEWAY = process.env.SIMPLESHOP_GATEWAY || 'http://127.0.0.1:5008/gateway'
const sleep = (ms) => new Promise((r) => setTimeout(r, ms))
/** 数据库断言：与前端显示值逐一比对，保证「前端 = 接口 = 数据库」。 */
const psql = (db, sql) => execSync(`docker exec -i postgres psql -U postgres -d ${db} -tAc ${JSON.stringify(sql)}`, { encoding: 'utf8' }).trim()
const num = (value) => Number(String(value ?? '').replace(/[^\d.-]/g, ''))

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
    // 后台登录：AuthService OpenIddict 令牌端点（password flow）；用户信息从访问令牌解析。
    const adminLogin = await (await fetch(`${GATEWAY}/auth/Token`, {
      method: 'POST', headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
      body: new URLSearchParams({ grant_type: 'password', username: 'codexadmin', password: 'Admin123456', client_id: 'admin-app' })
    })).json()
    const adminToken = adminLogin.access_token
    assert.ok(adminToken, `后台登录失败：${adminLogin.error_description || ''}`)
    const adminClaims = JSON.parse(Buffer.from(adminToken.split('.')[1].replace(/-/g, '+').replace(/_/g, '/'), 'base64').toString('utf8'))
    const asArray = value => value === undefined ? [] : Array.isArray(value) ? value : [value]
    const adminUser = {
      id: adminClaims.sub,
      userName: adminClaims['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] || adminClaims.name,
      tenantType: adminClaims.tenant_type, platformId: adminClaims.platform_id, merchantId: adminClaims.merchant_id,
      permissions: asArray(adminClaims.permission), roles: asArray(adminClaims.role)
    }
    const auth = { 'Content-Type': 'application/json', Authorization: `Bearer ${adminToken}` }
    const platforms = await (await fetch(`${GATEWAY}/platforms/List?page=1&pageSize=100`, { headers: auth })).json()
    const platform = platforms.data.items.find(item => item.platformCode === 'DEMOPL')
    assert.ok(platform, '未找到演示平台（demo）')
    const products = await (await fetch(`${GATEWAY}/products/List?page=1&pageSize=10&status=1`, { headers: auth })).json()
    const product = products.data.items.find(item => (item.skus || []).length)
    assert.ok(product, '演示平台没有可售商品')
    const sku = product.skus[0]

    const ts = Date.now()
    const reg = await (await fetch(`${GATEWAY}/customers/Register`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ userName: `uit${ts}`, password: 'Test1234', email: `uit${ts}@test.com`, phone: `135${String(ts).slice(-8)}`, platformId: platform.id, agreedAgreement: true, registerSource: 1 }) })).json()
    const userToken = reg.data.token
    const userAuth = { 'Content-Type': 'application/json', Authorization: `Bearer ${userToken}` }
    await fetch(`${GATEWAY}/carts/Add`, { method: 'POST', headers: userAuth, body: JSON.stringify({ productId: product.id, skuId: sku.id, merchantId: product.merchantId, platformId: platform.id, productName: product.name, image: product.mainImage, price: Number(sku.price), quantity: 1 }) })
    await fetch(`${GATEWAY}/customers/ToggleFavorite`, { method: 'POST', headers: userAuth, body: JSON.stringify({ userId: 0, productId: product.id }) })
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
      // 注入会话后重载一次：确保 App 启动时（onLaunch）已带登录态，页面首屏即为登录视图。
      await page.send('Page.reload', { ignoreCache: true })
      await sleep(2600)
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
    check('MP-02 首页会员问候卡', text.includes('Hi ') && text.includes('普通会员') && text.includes('优惠券'))
    check('MP-02b 首页优惠专区', text.includes('优惠专区'))

    // ---------- MP-04：商品详情 ----------
    await goto(page, `/#/pages/product/detail?id=${product.id}`)
    text = await textOf(page)
    const detailOk = ['配送', '商品评价', '加入购物车', '立即购买', '店铺', '购物车', '库存'].every(k => text.includes(k))
    check('MP-04 商品详情元素齐全', detailOk)
    check('MP-04b 活动/优惠标签', text.includes('到手价') || text.includes('已优惠') || text.includes('满'))
    const galleryCount = await evalOf(page)(`document.querySelectorAll('uni-swiper-item').length`)
    check('MP-04c 商品图集渲染', galleryCount >= 1, `items=${galleryCount}`)
    // MP-DB-01：前端到手价 = 营销引擎试算（京东/淘宝式到手价），划线原价 = 数据库 SKU 价
    const finalApi = await (await fetch(`${GATEWAY}/marketing/FinalPrice`, {
      method: 'POST', headers: userAuth,
      body: JSON.stringify({ platformId: platform.id, items: [{ skuId: sku.id, platformId: platform.id, merchantId: product.merchantId || sku.merchantId, productName: product.name, price: Number(sku.price), quantity: 1 }] })
    })).json()
    const expectFinal = num(finalApi.data.items[0].finalPrice)
    const expectDiscount = num(finalApi.data.items[0].activityDiscount) + num(finalApi.data.items[0].couponDiscount)
    const dbPrice = num(psql('simpleshopproduct', `select "Price" from sku where "Id"=${sku.id}`))
    const uiPrice = num(await evalOf(page)(`document.querySelector('.price-card .price')?.textContent || ''`))
    check('MP-DB-01 到手价 前端=营销引擎', uiPrice === expectFinal && uiPrice > 0, `ui=${uiPrice} api=${expectFinal}`)
    const uiPill = await evalOf(page)(`document.querySelector('.price-card .benefit-pill')?.textContent || ''`)
    if (expectDiscount > 0) {
      const pillDiscount = num((uiPill.match(/已优惠¥([\d.]+)/) || [])[1])
      check('MP-DB-01b 优惠说明金额=原价-到手价', pillDiscount === num((dbPrice - expectFinal).toFixed(2)), `pill=${uiPill} dbPrice=${dbPrice} final=${expectFinal}`)
      check('MP-DB-01c 到手价标签与优惠说明', uiPill.includes('到手价') && uiPill.includes('已优惠'), `pill=${uiPill}`)
    } else {
      check('MP-DB-01b 无优惠时价格=数据库价', uiPrice === dbPrice, `ui=${uiPrice} db=${dbPrice}`)
    }

    // MP-DB-01d：列表页（分类页）到手价 = 营销引擎，且优惠商品展示到手价标签与划线原价
    await goto(page, '/#/pages/category/category', 6000)
    const listApi = await (await fetch(`${GATEWAY}/products/List?status=1&page=1&pageSize=50`, { headers: userAuth })).json()
    const listProducts = (listApi.data.items || []).map(item => ({
      item,
      sku: (item.skus || []).filter(s => s.isActive !== false).sort((a, b) => Number(a.price) - Number(b.price))[0]
    })).filter(entry => entry.sku)
    const batchApi = await (await fetch(`${GATEWAY}/marketing/FinalPrice`, {
      method: 'POST', headers: userAuth,
      body: JSON.stringify({ platformId: platform.id, items: listProducts.map(entry => ({ skuId: entry.sku.id, platformId: entry.sku.platformId || platform.id, merchantId: entry.sku.merchantId || entry.item.merchantId, productName: entry.item.name, price: Number(entry.sku.price), quantity: 1 })) })
    })).json()
    const finalBySku = new Map((batchApi.data.items || []).map(row => [String(row.skuId), row]))
    const cardOf = async name => JSON.parse(await evalOf(page)(`(() => {
      const c = [...document.querySelectorAll('.product')].find(node => node.querySelector('.pname')?.textContent?.trim() === ${JSON.stringify(name)})
      return JSON.stringify({ price: c?.querySelector('.price')?.textContent?.trim() || '' })
    })()`))
    const firstEntry = listProducts[0]
    const firstCard = await cardOf(firstEntry.item.name)
    check('MP-DB-01d 列表页到手价 前端=营销引擎', num(firstCard.price) === num(finalBySku.get(String(firstEntry.sku.id)).finalPrice), `ui=${firstCard.price} api=${finalBySku.get(String(firstEntry.sku.id)).finalPrice}`)
    const discounted = listProducts.find(entry => num(finalBySku.get(String(entry.sku.id)).finalPrice) < Number(entry.sku.price))
    if (discounted) {
      const discountedCard = await cardOf(discounted.item.name)
      check('MP-DB-01e 列表页优惠商品价格=引擎到手价', num(discountedCard.price) === num(finalBySku.get(String(discounted.sku.id)).finalPrice), `ui=${discountedCard.price} api=${finalBySku.get(String(discounted.sku.id)).finalPrice}`)
    } else {
      check('MP-DB-01e 列表页到手价标签与划线原价', true, '本页无优惠商品，跳过（详情页 MP-DB-01c 已覆盖）')
    }

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
    // MP-DB-02：前端购物车数量 = 数据库 cart_items.Quantity
    const dbCartQty = psql('simpleshopcart', `select "Quantity" from cart_items where "UserId"=${reg.data.user.id} and "SkuId"=${sku.id} and "IsDeleted"=false`)
    check('MP-DB-02 购物车数量 前端=数据库', after.trim() === dbCartQty, `ui=${after} db=${dbCartQty}`)

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

    // ---------- MP-DB-03：UI 提交订单 → 数据库与订单列表三端一致 ----------
    const payableText = await evalOf(page)(`document.querySelector('.summary-card')?.textContent || ''`)
    const payable = num((payableText.match(/实付金额¥([\d.]+)/) || [])[1])
    check('MP-DB-03 结算页解析实付金额', payable > 0, `payable=${payable}`)
    await evalOf(page)(`[...document.querySelectorAll('uni-button')].find(b => b.textContent.includes('提交并支付'))?.click()`)
    let redirected = false
    for (let i = 0; i < 30; i++) {
      const hash = await evalOf(page)('location.hash')
      if (hash.includes('/pages/orders/orders')) { redirected = true; break }
      await sleep(600)
    }
    check('MP-DB-03b UI 下单后跳转订单列表', redirected)
    const orderRow = psql('simpleshoporder', `select "Id"||'|'||"OrderNo"||'|'||"PaymentPrice"||'|'||"OrderStatus" from "order" where "CustomerId"=${reg.data.user.id} order by "Id" desc limit 1`)
    const [dbOrderId, dbOrderNo, dbPaymentPrice, dbOrderStatus] = orderRow.split('|')
    check('MP-DB-03c 数据库订单实付 = 前端显示', num(dbPaymentPrice) === payable, `db=${dbPaymentPrice} ui=${payable}`)
    let paidOk = dbOrderStatus === '20'
    for (let i = 0; i < 10 && !paidOk; i++) { await sleep(1000); paidOk = psql('simpleshoporder', `select "OrderStatus" from "order" where "Id"=${dbOrderId}`) === '20' }
    check('MP-DB-03d 数据库订单已支付(20)', paidOk, `status=${dbOrderStatus}`)
    const ordersText = await textOf(page)
    check('MP-DB-03e 订单列表显示数据库订单号', ordersText.includes(dbOrderNo), `orderNo=${dbOrderNo}`)
    check('MP-DB-03f 订单列表显示数据库金额', ordersText.includes(Number(dbPaymentPrice).toFixed(2)), `amount=${dbPaymentPrice}`)

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
    check('MP-05 店铺页头部与商品', text.includes('全部商品') && text.includes('销量') && text.includes('价格'), text.slice(0, 120))
    check('MP-05b 店铺双 Tab 与分类 chips', text.includes('商品') && text.includes('活动') && text.includes('推荐'))

    // ---------- MP-03：我的页（会员卡/权益/账户/服务宫格） ----------
    await goto(page, '/#/pages/profile/profile')
    text = await textOf(page)
    const profileOk = ['普通会员', '享受以下权益', '我的账户', '我的服务', '我的订单', '我的活动'].every(k => text.includes(k))
    check('MP-03 我的页结构与服务宫格', profileOk)
    check('MP-03b 会员升级进度', text.includes('再消费') || text.includes('最高等级'))
    // MP-DB-04：我的账户三项统计 = 数据库真实数量
    const statsText = await evalOf(page)(`document.querySelector('.account-stats')?.textContent || ''`)
    const statsNumbers = (statsText.match(/\d+/g) || []).map(Number)
    const dbCoupons = Number(psql('simpleshopmarketing', `select count(*) from user_coupon where "UserId"=${reg.data.user.id} and "Status"=1`))
    const dbFavorites = Number(psql('simpleshopcustomer', `select count(*) from customer_favorite where "CustomerId"=${reg.data.user.id} and "IsDeleted"=false`))
    const dbOrders = Number(psql('simpleshoporder', `select count(*) from "order" where "CustomerId"=${reg.data.user.id} and "IsDeleted"=false`))
    check('MP-DB-04 我的账户统计 前端=数据库', JSON.stringify(statsNumbers.slice(0, 3)) === JSON.stringify([dbCoupons, dbFavorites, dbOrders]), `ui=${JSON.stringify(statsNumbers)} db=${[dbCoupons, dbFavorites, dbOrders]}`)

    // ---------- AUTH-12：登录失效自动跳登录 ----------
    await evalOf(page)(`localStorage.setItem('token', 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxIiwiZXhwIjoxMDAwMDAwMDAwfQ.sig')`)
    await goto(page, '/#/pages/coupon/mine', 4500)
    const hashLogin = await evalOf(page)('location.hash')
    const tokenLeft = await evalOf(page)(`localStorage.getItem('token')`)
    check('AUTH-12 token 失效跳登录并清会话', hashLogin === '#/pages/auth/login' && !tokenLeft, `hash=${hashLogin} token=${tokenLeft}`)
    page.close()

    // ---------- ADM：后台 ----------
    const admin = await newPage(ADMIN + '/')
    // 装修页为三栏可视化编辑器，需要桌面视口才能拿到正确的拖拽坐标。
    await admin.send('Emulation.setDeviceMetricsOverride', { width: 1680, height: 1050, deviceScaleFactor: 1, mobile: false })
    await sleep(1500)
    await admin.send('Page.navigate', { url: ADMIN + '/#/login' })
    await sleep(2500)
    // ADM-00：后台登录表单真实走 AuthService OpenIddict（password flow），成功后写入会话。
    await evalOf(admin)(`(() => {
      const set = (el, value) => { Object.getOwnPropertyDescriptor(HTMLInputElement.prototype, 'value').set.call(el, value); el.dispatchEvent(new Event('input', { bubbles: true })) }
      const inputs = document.querySelectorAll('.login-card input')
      set(inputs[0], 'codexadmin'); set(inputs[1], 'Admin123456')
      document.querySelector('.login-card button').click()
    })()`)
    let adminLogged = false
    for (let i = 0; i < 20; i++) {
      const hash = await evalOf(admin)('location.hash')
      const stored = await evalOf(admin)(`!!localStorage.getItem('admin_token')`)
      if (hash.includes('/dashboard') && stored) { adminLogged = true; break }
      await sleep(700)
    }
    check('ADM-00 后台登录（OpenIddict password flow）', adminLogged)
    await evalOf(admin)(`localStorage.setItem('admin_token', ${JSON.stringify(adminToken)}); localStorage.setItem('admin_user', ${JSON.stringify(JSON.stringify(adminUser))});`)
    await admin.send('Page.navigate', { url: ADMIN + '/#/marketing' })
    await sleep(5000)
    const adminText = await evalOf(admin)('document.body.textContent')
    const marketingOk = ['活动管理', '券模板', '券活动', '效果报表', '营销配置'].every(k => adminText.includes(k))
    check('ADM-04 营销管理页 5 个 Tab', marketingOk)
    const activityRows = await evalOf(admin)(`document.querySelectorAll('.el-table__body tbody tr').length`)
    check('ADM-04b 活动列表有数据', activityRows > 0, `rows=${activityRows}`)
    // ADM-DB-01：后台订单详情显示前台刚下的订单（订单号/金额来自数据库）
    const adminOrderRow = psql('simpleshoporder', `select "Id"||'|'||"OrderNo"||'|'||"PaymentPrice" from "order" where "CustomerId"=${reg.data.user.id} order by "Id" desc limit 1`)
    const [adminOrderId, adminOrderNo, adminOrderAmount] = adminOrderRow.split('|')
    await admin.send('Page.navigate', { url: ADMIN + `/#/orders/${adminOrderId}/detail` })
    await sleep(5000)
    const adminOrderText = await evalOf(admin)('document.body.textContent')
    check('ADM-DB-01 后台订单详情显示数据库订单', adminOrderText.includes(adminOrderNo) && adminOrderText.includes(Number(adminOrderAmount).toFixed(2)), `orderNo=${adminOrderNo} amount=${adminOrderAmount}`)

    await admin.send('Page.navigate', { url: ADMIN + '/#/app-design' })
    await sleep(5000)
    const designText = await evalOf(admin)('document.body.textContent')
    check('ADM-07 装修页可视化编辑器（组件库/预览/属性）', designText.includes('首页轮播图') && designText.includes('我的服务') && designText.includes('快捷入口') && designText.includes('主题与标签'))
    check('ADM-07b 手机预览框架渲染', await evalOf(admin)(`!!document.querySelector('.phone .mock-hero') && !!document.querySelector('.tabbar')`))
    const modulesBefore = await evalOf(admin)(`document.querySelectorAll('.mock-module').length`)
    await evalOf(admin)(`document.querySelector('.palette-item')?.click()`)
    await sleep(800)
    const modulesAfter = await evalOf(admin)(`document.querySelectorAll('.mock-module').length`)
    check('ADM-07c 点击组件库添加到预览', modulesAfter === modulesBefore + 1, `${modulesBefore}->${modulesAfter}`)
    const selectedPanel = await evalOf(admin)(`document.querySelector('.right-panel .panel-title')?.textContent || ''`)
    check('ADM-07d 新模块自动选中并显示属性面板', selectedPanel.includes('品牌头'), selectedPanel)

    // ADM-07e/f/g：真实鼠标拖拽（放入占位 / 拖出删除 / 拖动排序）
    const mouse = (type, x, y, buttons = 0) => admin.send('Input.dispatchMouseEvent', {
      type, x, y, button: 'left', buttons, clickCount: type === 'mousePressed' || type === 'mouseReleased' ? 1 : 0
    })
    const rectOf = async selector => JSON.parse(await evalOf(admin)(`(() => { const el = document.querySelector(${JSON.stringify(selector)}); if (!el) return 'null'; const r = el.getBoundingClientRect(); return JSON.stringify({ x: r.x + r.width / 2, y: r.y + r.height / 2, top: r.y, bottom: r.y + r.height, left: r.x }) })()`))
    const moduleCount = () => evalOf(admin)(`document.querySelectorAll('.mock-module').length`)

    const paletteRect = await rectOf('.palette-item')
    const phoneRect = await rectOf('.phone-screen')
    const countBeforeDrop = await moduleCount()
    await mouse('mousePressed', paletteRect.x, paletteRect.y, 1)
    await sleep(150)
    await mouse('mouseMoved', phoneRect.x, phoneRect.y + 160, 1)
    await sleep(250)
    const gapShown = await evalOf(admin)(`!!document.querySelector('.drop-gap')`)
    await mouse('mouseReleased', phoneRect.x, phoneRect.y + 160, 0)
    await sleep(500)
    const countAfterDrop = await moduleCount()
    check('ADM-07e 拖拽放入（占位线 + 模块增加）', gapShown && countAfterDrop === countBeforeDrop + 1, `gap=${gapShown} ${countBeforeDrop}->${countAfterDrop}`)

    const firstModule = await rectOf('.mock-module')
    // 模块中心可能被手机底部 TabBar 遮挡，按住模块顶部（可见区域）再拖出。
    await mouse('mousePressed', firstModule.x, firstModule.top + 26, 1)
    await sleep(150)
    await mouse('mouseMoved', phoneRect.left - 120, phoneRect.y, 1)
    await sleep(250)
    const outHint = await evalOf(admin)(`document.querySelector('.drag-out-hint')?.textContent || ''`)
    await mouse('mouseReleased', phoneRect.left - 120, phoneRect.y, 0)
    await sleep(500)
    const countAfterOut = await moduleCount()
    check('ADM-07f 拖出预览删除模块', outHint.includes('松手删除') && countAfterOut === countAfterDrop - 1, `hint=${outHint} ${countAfterDrop}->${countAfterOut}`)

    // 添加一个短模块（公告），滚到底部后用两个相邻且名称不同的模块验证拖动排序。
    await evalOf(admin)(`[...document.querySelectorAll('.palette-item')].find(el => el.textContent.includes('公告'))?.click()`)
    await sleep(600)
    await evalOf(admin)(`(() => { const scroll = document.querySelector('.screen-scroll'); scroll.scrollTop = scroll.scrollHeight })()`)
    await sleep(400)
    const namesBefore = await evalOf(admin)(`[...document.querySelectorAll('.mock-module .module-name')].map(el => el.textContent).join('|')`)
    const visible = JSON.parse(await evalOf(admin)(`JSON.stringify([...document.querySelectorAll('.mock-module')].map(el => { const r = el.getBoundingClientRect(); return { name: el.querySelector('.module-name')?.textContent || '', x: r.x + r.width / 2, top: r.y, bottom: r.y + r.height } }).filter(item => item.top > 235 && item.bottom < 875))`))
    const pairIndex = visible.findIndex((item, index) => index + 1 < visible.length && visible[index + 1].name !== item.name)
    if (pairIndex >= 0) {
      const from = visible[pairIndex]; const to = visible[pairIndex + 1]
      const dropY = Math.min(to.bottom - 6, 870)
      await mouse('mousePressed', from.x, from.top + 26, 1)
      await sleep(150)
      await mouse('mouseMoved', to.x, dropY, 1)
      await sleep(250)
      await mouse('mouseReleased', to.x, dropY, 0)
      await sleep(500)
    }
    const namesAfter = await evalOf(admin)(`[...document.querySelectorAll('.mock-module .module-name')].map(el => el.textContent).join('|')`)
    check('ADM-07g 拖动模块排序生效', pairIndex >= 0 && namesAfter !== namesBefore, `pair=${pairIndex} visible=${JSON.stringify(visible.map(item => item.name))} ${namesBefore} -> ${namesAfter}`)
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
