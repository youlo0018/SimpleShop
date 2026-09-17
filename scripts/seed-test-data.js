#!/usr/bin/env node
/**
 * SimpleShop 演示数据填充脚本
 *
 * 数据来源：https://dummyjson.com/products（公开测试数据 API，商品标题/价格/图片/描述）。
 * 流程：演示平台 + 两个商户（含商户管理员）→ 三级分类 → 商品与 SKU（自动上架）
 *       → 券模板/券活动/平台与商户活动 → 演示用户领券 → 下单/支付/发货/签收/退款。
 *
 * 可重复执行：平台/商户/分类/活动按名称或编码复用，商品按 SKU 编码去重，订单每次追加。
 * 用法：
 *   node scripts/seed-test-data.js                 # 默认 30 个商品 + 12 笔订单
 *   node scripts/seed-test-data.js --skip-orders   # 只补商品与营销配置
 *   node scripts/seed-test-data.js --products=12 --orders=6
 */

const BASE = process.env.SIMPLESHOP_GATEWAY || 'http://127.0.0.1:5008/gateway'
const ADMIN_USER = process.env.SIMPLESHOP_ADMIN || 'codexadmin'
const ADMIN_PASS = process.env.SIMPLESHOP_ADMIN_PASS || 'Admin123456'
const args = Object.fromEntries(process.argv.slice(2).map(item => {
  const [key, value] = item.split('=')
  return [key.replace(/^--/, ''), value ?? true]
}))
const PRODUCT_LIMIT = Number(args.products || 30)
const ORDER_LIMIT = Number(args.orders || 12)
const SKIP_ORDERS = Boolean(args['skip-orders'])

const sleep = ms => new Promise(resolve => setTimeout(resolve, ms))
let adminToken = ''
let merchantToken = ''

const api = async (path, { method = 'GET', body, token, allowFail = false } = {}) => {
  const response = await fetch(`${BASE}${path}`, {
    method,
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {})
    },
    body: body === undefined ? undefined : JSON.stringify(body)
  })
  const payload = await response.json().catch(() => ({}))
  if (!allowFail && Number(payload?.code) !== 200) {
    throw new Error(`${method} ${path} 失败：${payload?.message || response.status}`)
  }
  return payload
}
const admin = (path, options = {}) => api(path, { ...options, token: adminToken })
const customer = (path, { token, ...options }) => api(path, { ...options, token })

const log = (...parts) => console.log('[seed]', ...parts)

// ---------- 商品数据（dummyjson，失败时回退内置数据集） ----------
const FALLBACK_PRODUCTS = [
  { id: 9001, title: '无线降噪耳机', price: 299, category: 'mobile-accessories', thumbnail: 'https://dummyimage.com/600x600/e8f0fe/1d1d1f&text=Headphone', images: [], description: '主动降噪，长续航。' },
  { id: 9002, title: '轻薄笔记本电脑', price: 4999, category: 'laptops', thumbnail: 'https://dummyimage.com/600x600/e8f0fe/1d1d1f&text=Laptop', images: [], description: '14 英寸轻薄本。' },
  { id: 9003, title: '智能手表', price: 899, category: 'smartphones', thumbnail: 'https://dummyimage.com/600x600/e8f0fe/1d1d1f&text=Watch', images: [], description: '健康监测。' },
  { id: 9004, title: '不粘炒锅', price: 159, category: 'kitchen-accessories', thumbnail: 'https://dummyimage.com/600x600/f1f3f5/1d1d1f&text=Pan', images: [], description: '麦饭石不粘。' },
  { id: 9005, title: '北欧布艺沙发', price: 1899, category: 'furniture', thumbnail: 'https://dummyimage.com/600x600/f1f3f5/1d1d1f&text=Sofa', images: [], description: '三人位。' },
  { id: 9006, title: '纯棉男士衬衫', price: 129, category: 'mens-shirts', thumbnail: 'https://dummyimage.com/600x600/fdf2f8/1d1d1f&text=Shirt', images: [], description: '免烫商务。' },
  { id: 9007, title: '女士针织连衣裙', price: 359, category: 'womens-dresses', thumbnail: 'https://dummyimage.com/600x600/fdf2f8/1d1d1f&text=Dress', images: [], description: '秋冬新款。' },
  { id: 9008, title: '保湿精华液', price: 219, category: 'beauty', thumbnail: 'https://dummyimage.com/600x600/f5f5f7/1d1d1f&text=Serum', images: [], description: '补水保湿。' }
]

const fetchProducts = async () => {
  try {
    const response = await fetch('https://dummyjson.com/products?limit=100&select=id,title,price,category,thumbnail,images,brand,description')
    if (!response.ok) throw new Error(`HTTP ${response.status}`)
    const data = await response.json()
    if (!Array.isArray(data.products) || !data.products.length) throw new Error('空数据')
    log(`从 dummyjson 拉取到 ${data.products.length} 条测试商品`)
    return data.products
  } catch (error) {
    log(`拉取在线测试数据失败（${error.message}），使用内置数据集`)
    return FALLBACK_PRODUCTS
  }
}

const CATEGORY_MAP = {
  laptops: '电脑办公', tablets: '手机数码', smartphones: '手机数码', 'mobile-accessories': '手机数码',
  'kitchen-accessories': '厨房用品', groceries: '厨房用品',
  furniture: '家居收纳', 'home-decoration': '家居收纳',
  'mens-shirts': '男装', 'mens-shoes': '男装', 'mens-watches': '男装',
  'womens-dresses': '女装美妆', 'womens-shoes': '女装美妆', 'womens-bags': '女装美妆', 'womens-jewellery': '女装美妆', 'womens-watches': '女装美妆',
  beauty: '女装美妆', fragrances: '女装美妆', 'skin-care': '女装美妆', sunglasses: '女装美妆',
  tops: '女装美妆', vehicle: '手机数码', motorcycle: '手机数码', 'sports-accessories': '男装'
}

const findPlatform = async () => {
  const data = await admin('/platforms/List?page=1&pageSize=100')
  const existing = (data.data.items || []).find(item => item.platformCode === 'demo')
  if (existing) return existing.id
  const created = await admin('/platforms/Create', {
    method: 'POST',
    body: { platformCode: 'demo', platformName: '演示商城', contactEmail: 'demo@simpleshop.test', defaultCommissionRate: 3 }
  })
  log('创建演示平台：演示商城')
  return created.data.id
}

const findMerchant = async (platformId, name, index) => {
  const data = await admin('/merchants/List?page=1&pageSize=100')
  const existing = (data.data.items || []).find(item => item.merchantName === name && String(item.platformId) === String(platformId))
  if (existing) return existing.id
  const created = await admin('/merchants/Create', {
    method: 'POST',
    body: {
      platformId, merchantName: name, contactName: `演示联系人${index}`, contactPhone: `1380000000${index}`,
      contactEmail: `merchant${index}@demo.test`, commissionRate: 3
    }
  })
  const id = created.data.id
  await admin('/merchants/Review', { method: 'POST', body: { id, status: 20 } })
  log(`创建并审核商户：${name}`)
  return id
}

const ensureMerchantAdmin = async (platformId, merchantId) => {
  const userName = 'demo-merchant'
  const existing = await admin(`/users/Users?keyword=${userName}&page=1&pageSize=5`).catch(() => ({ data: { items: [] } }))
  if (!(existing.data.items || []).some(item => item.userName === userName)) {
    await admin('/users/Create', {
      method: 'POST',
      body: {
        userName, password: 'Demo123456', email: 'demo-merchant@demo.test', phone: '13700000001',
        role: 'merchant-admin', platformId, merchantId
      }
    })
    log('创建商户管理员：demo-merchant')
  }
  const login = await api('/users/Login', { method: 'POST', body: { userName, password: 'Demo123456' }, allowFail: true })
  if (Number(login?.code) !== 200) throw new Error('商户管理员登录失败，请在权限中心检查 merchant-admin 角色绑定')
  merchantToken = login.data.token
}

const ensureCategories = async () => {
  const tree = await admin('/products/GetCategoryTree')
  const find = (items, name) => {
    for (const item of items || []) {
      if (item.name === name) return item
      const child = find(item.children, name)
      if (child) return child
    }
    return null
  }
  const result = {}
  for (const top of ['数码', '家居生活', '服饰美妆']) {
    let node = find(tree.data, top)
    if (!node) node = (await admin('/products/CreateCategory', { method: 'POST', body: { name: top, parentId: 0, sort: 0 } })).data
    result[top] = node
  }
  const children = { '手机数码': '数码', '电脑办公': '数码', '厨房用品': '家居生活', '家居收纳': '家居生活', '男装': '服饰美妆', '女装美妆': '服饰美妆' }
  for (const [name, parentName] of Object.entries(children)) {
    let node = find(tree.data, name)
    if (!node) {
      node = (await admin('/products/CreateCategory', { method: 'POST', body: { name, parentId: result[parentName].id, sort: 0 } })).data
      log(`创建分类：${parentName}/${name}`)
    }
    result[name] = node
  }
  return result
}

const existingSkuCodes = async () => {
  const codes = new Set()
  for (let page = 1; page <= 5; page++) {
    const data = await admin(`/products/List?page=${page}&pageSize=100`)
    const items = data.data.items || []
    items.forEach(product => (product.skus || []).forEach(sku => codes.add(sku.skuCode)))
    if (items.length < 100) break
  }
  return codes
}

const demoProductsFromList = async () => {
  const result = []
  for (let page = 1; page <= 5; page++) {
    const data = await admin(`/products/List?page=${page}&pageSize=100`)
    const items = data.data.items || []
    for (const product of items) {
      const sku = (product.skus || []).find(item => String(item.skuCode || '').startsWith('DEMO-'))
      if (!sku) continue
      result.push({
        id: product.id, name: product.name, price: Number(sku.price || product.minPrice || 0),
        merchantId: product.merchantId, skuId: sku.id
      })
    }
    if (items.length < 100) break
  }
  return result
}

const seedProducts = async (platformId, merchants, categories, source) => {
  const skuCodes = await existingSkuCodes()
  const created = []
  const picks = source.slice(0, PRODUCT_LIMIT)
  for (let index = 0; index < picks.length; index++) {
    const item = picks[index]
    const skuCode = `DEMO-${item.id}-1`
    if (skuCodes.has(skuCode)) continue
    const merchant = merchants[index % merchants.length]
    const categoryName = CATEGORY_MAP[item.category] || '家居收纳'
    const name = String(item.title || `演示商品${item.id}`).slice(0, 40)
    const price = Math.max(1, Math.round(Number(item.price || 9.9) * 100) / 100)
    const product = await admin('/products/CreateProduct', {
      method: 'POST',
      body: {
        platformId, merchantId: merchant,
        name, mainImage: item.thumbnail || '', categoryId: categories[categoryName].id, brandId: 0,
        description: String(item.description || name).slice(0, 255),
        skus: [
          { skuCode, price, originalPrice: Math.round(price * 1.3 * 100) / 100, stock: 60 + (index * 7) % 120, image: item.thumbnail || '', specName: '规格', specValue: '标准装' },
          { skuCode: `DEMO-${item.id}-2`, price: Math.round(price * 1.6 * 100) / 100, originalPrice: Math.round(price * 2.1 * 100) / 100, stock: 30 + (index * 5) % 60, image: (item.images || [])[0] || item.thumbnail || '', specName: '规格', specValue: '豪华装' }
        ]
      }
    })
    const productId = product.data
    await admin('/products/PublishProduct', { method: 'POST', body: { id: productId, approved: true } })
    created.push({ id: productId, name, price, merchantId: merchant, skuId: (await admin(`/products/AdminDetail?id=${productId}`)).data.skus[0].id })
    skuCodes.add(skuCode)
  }
  log(`创建并上架商品 ${created.length} 个`)
  // 合并已存在的演示商品，重复执行时可继续补订单/营销数据。
  const all = await demoProductsFromList()
  log(`演示商品合计可用：${all.length} 个`)
  return all
}

const findOrCreate = async (listPath, matches, createPath, body) => {
  const existing = (await admin(`${listPath}?page=1&pageSize=100`)).data.items || []
  const found = existing.find(matches)
  if (found) return found.id
  return (await admin(createPath, { method: 'POST', body })).data.id
}

const seedMarketing = async (platformId, merchantId, products) => {
  const templateIds = {}
  const templates = [
    { key: 'full100', name: '满100减10', couponType: 1, threshold: 100, discountValue: 10 },
    { key: 'full300', name: '满300减50', couponType: 1, threshold: 300, discountValue: 50 },
    { key: 'discount85', name: '满200打8.5折', couponType: 2, threshold: 200, discountValue: 0.85 },
    { key: 'zero5', name: '0元减5', couponType: 3, threshold: 0, discountValue: 5 }
  ]
  for (const template of templates) {
    templateIds[template.key] = await findOrCreate(
      '/marketing/CouponTemplateList',
      item => item.name === template.name,
      '/marketing/SaveCouponTemplate',
      { platformId, name: template.name, couponType: template.couponType, threshold: template.threshold, discountValue: template.discountValue, validDays: 30, description: '演示券' }
    )
  }
  const couponActivityIds = []
  for (const key of ['full100', 'full300', 'discount85', 'zero5']) {
    const name = `领券中心-${templates.find(item => item.key === key).name}`
    const id = await findOrCreate(
      '/marketing/CouponActivityList',
      item => item.name === name,
      '/marketing/SaveCouponActivity',
      { platformId, name, couponTemplateId: templateIds[key], scopeType: 1, totalStock: 500, perUserLimit: 5, isClaimable: true }
    )
    couponActivityIds.push(id)
  }
  const giftCouponActivity = await findOrCreate(
    '/marketing/CouponActivityList',
    item => item.name === '满赠回馈券（满50）',
    '/marketing/SaveCouponActivity',
    { platformId, name: '满赠回馈券（满50）', couponTemplateId: templateIds.zero5, scopeType: 1, totalStock: 1000, perUserLimit: 10, isClaimable: false }
  )

  const activities = [
    { name: '全平台满100减10', activityType: 1, threshold: 100, discountValue: 10, scopeType: 1, merchantId: 0 },
    { name: '演示旗舰店满99减9', activityType: 1, threshold: 99, discountValue: 9, scopeType: 2, merchantId: 0, targetMerchantIds: [merchantId] },
    { name: '指定商品满200减20', activityType: 1, threshold: 200, discountValue: 20, scopeType: 3, merchantId: 0, targetProducts: products.slice(0, 2).map(item => ({ skuId: item.skuId, merchantId: item.merchantId })) },
    { name: '全场满500打9折', activityType: 2, threshold: 500, discountValue: 0.9, scopeType: 1, merchantId: 0 },
    { name: '满50赠券', activityType: 3, threshold: 50, discountValue: 0, scopeType: 1, merchantId: 0, giftCouponActivityId: giftCouponActivity },
    { name: '商户满100减15', activityType: 1, threshold: 100, discountValue: 15, scopeType: 1, merchantId }
  ]
  for (const activity of activities) {
    await findOrCreate(
      '/marketing/ActivityList',
      item => item.name === activity.name,
      '/marketing/SaveActivity',
      { platformId, startAt: '2026-01-01T00:00:00', isEnabled: true, ...activity }
    )
  }
  log('营销配置就绪：4 张券模板 + 5 个券活动 + 6 个活动')
  return couponActivityIds
}

const ensureCustomers = async (count) => {
  const users = []
  for (let index = 1; index <= count; index++) {
    const userName = `demo_user_${String(index).padStart(2, '0')}`
    const password = 'Test123456'
    let login = await api('/users/Register', {
      method: 'POST',
      body: { userName, password, email: `${userName}@demo.test`, phone: `139000000${String(index).padStart(2, '0')}` },
      allowFail: true
    })
    if (Number(login?.code) !== 200) {
      login = await api('/users/Login', { method: 'POST', body: { userName, password } })
    }
    users.push({ token: login.data.token, id: login.data.user.id, userName })
  }
  log(`演示用户就绪：${users.length} 个`)
  return users
}

const claimCoupons = async (users, couponActivityIds) => {
  let claimed = 0
  for (const user of users) {
    for (const couponActivityId of couponActivityIds.slice(0, 2)) {
      const result = await api('/marketing/ClaimCoupon', { method: 'POST', body: { couponActivityId }, token: user.token, allowFail: true })
      if (Number(result?.code) === 200) claimed++
    }
  }
  log(`用户领券 ${claimed} 张（重复领取会跳过）`)
}

const seedOrders = async (platformId, products, users) => {
  if (!products.length || !users.length) return { orders: 0, paid: 0, shipped: 0, refunded: 0 }
  const stats = { orders: 0, paid: 0, shipped: 0, refunded: 0 }
  const runTag = Date.now()
  for (let index = 0; index < ORDER_LIMIT; index++) {
    const user = users[index % users.length]
    const first = products[(index * 3) % products.length]
    const second = products[(index * 5 + 1) % products.length]
    const lines = [{ product: first, quantity: 1 + (index % 2) }, ...(index % 3 === 0 && second.id !== first.id ? [{ product: second, quantity: 1 }] : [])]
    const items = lines.map(line => ({
      skuId: line.product.skuId, platformId, merchantId: line.product.merchantId,
      productName: line.product.name, price: line.product.price, quantity: line.quantity
    }))
    const order = await customer('/orders/Create', {
      method: 'POST', token: user.token,
      body: {
        idempotencyKey: `demo-${runTag}-${index}`,
        platformId, customerNo: `U${user.id}`, customerName: user.userName,
        receiverName: '演示收货人', receiverPhone: '13800000000', receiverAddress: '演示省演示市演示区演示路 1 号',
        selectedUserCouponIds: index % 2 === 0 ? null : [],
        items, stockItems: lines.map(line => ({ skuId: line.product.skuId, quantity: line.quantity }))
      }
    }).catch(error => ({ code: 500, message: error.message }))
    if (order.data?.success !== true) { log(`订单 ${index + 1} 跳过：${order.data?.message || order.message}`); continue }
    stats.orders++
    const orderNo = order.data.orderNo
    const orderId = order.data.id
    if (index % 4 === 3) continue // 留一笔待支付订单

    const payItems = lines.map(line => ({ skuId: line.product.skuId, quantity: line.quantity }))
    const payment = await customer('/payments/Create', {
      method: 'POST', token: user.token,
      body: { bizNo: orderNo, platformId, merchantId: lines[0].product.merchantId, userId: user.id, amount: order.data.paymentPrice, items: payItems }
    }).catch(() => null)
    if (!payment || Number(payment.code) !== 200) { log(`订单 ${index + 1} 支付单创建失败`); continue }
    await customer('/payments/Confirm', { method: 'POST', token: user.token, body: { bizNo: orderNo, items: payItems } }).catch(() => null)
    stats.paid++
    await sleep(900) // 等支付成功消费者把订单置为已支付

    if (index % 3 === 0) {
      const detail = await customer(`/orders/Detail?id=${orderId}&customerId=${user.id}`, { token: user.token })
      const shipItems = (detail.data.items || []).map(item => ({ orderItemId: item.id, skuId: item.skuId, quantity: item.quantity }))
      const shipment = await api('/orders/Shipment', {
        method: 'POST', token: adminToken,
        body: { orderId, platformId, merchantId: lines[0].product.merchantId, logisticsCompany: '顺丰速运', trackingNo: `SF${runTag}${index}`, items: shipItems }
      }).catch(() => null)
      if (shipment && Number(shipment.code) === 200) {
        stats.shipped++
        await customer('/orders/Receive', { method: 'POST', token: user.token, body: { shipmentId: shipment.data.shipmentId } }).catch(() => null)
      }
    }
  }
  return stats
}

const seedRefund = async (users) => {
  // 找到一笔已支付订单并走一遍退款申请 → 审批，产生退款报表数据。
  for (const user of users) {
    const list = await customer('/orders/List?page=1&pageSize=20', { token: user.token })
    const order = (list.data.items || []).find(item => Number(item.orderStatus) >= 20 && Number(item.orderStatus) !== 60)
    if (!order) continue
    const detail = await customer(`/orders/Detail?id=${order.id}&customerId=${user.id}`, { token: user.token })
    const items = (detail.data.items || []).map(item => ({ skuId: item.skuId, quantity: Number(item.quantity) }))
    const result = await customer('/payments/Refund', {
      method: 'POST', token: user.token,
      body: { bizNo: order.orderNo, amount: Number(order.paymentPrice), reason: '演示退款：不想要了', items }
    }).catch(() => null)
    if (!result || Number(result.code) !== 200) continue
    const refunds = await admin(`/payments/Refunds?keyword=${order.orderNo}&page=1&pageSize=5`)
    const refund = (refunds.data.items || [])[0]
    if (refund && Number(refund.status) === 10) {
      await admin('/payments/ApproveRefund', { method: 'POST', body: { id: refund.id } })
      log(`演示退款审批通过：${order.orderNo}`)
      return 1
    }
  }
  return 0
}

const summary = async () => {
  const products = await admin('/products/List?page=1&pageSize=1')
  const users = await admin('/users/Users?page=1&pageSize=1')
  const orders = await admin('/orders/List?page=1&pageSize=1')
  const payments = await admin('/payments/Payments?page=1&pageSize=1')
  const refunds = await admin('/payments/Refunds?page=1&pageSize=1')
  const activityReport = await admin('/marketing/ActivityReport?page=1&pageSize=5')
  const couponReport = await admin('/marketing/CouponReport?page=1&pageSize=5')
  console.log('\n===== 数据概览 =====')
  console.log(`商品：${products.data.total} 个    用户：${users.data.total} 个    订单：${orders.data.total} 笔`)
  console.log(`支付单：${payments.data.total} 笔    退款单：${refunds.data.total} 笔`)
  console.log(`活动参与：${activityReport.data.totalOrders} 单 / 折扣 ${Number(activityReport.data.totalDiscount).toFixed(2)} 元    用券：${couponReport.data.totalCoupons} 笔 / 抵扣 ${Number(couponReport.data.totalDiscount).toFixed(2)} 元`)
}

(async () => {
  log(`网关：${BASE}`)
  const login = await api('/users/Login', { method: 'POST', body: { userName: ADMIN_USER, password: ADMIN_PASS } })
  adminToken = login.data.token

  const platformId = await findPlatform()
  const merchants = [
    await findMerchant(platformId, '演示旗舰店', 1),
    await findMerchant(platformId, '品质优选店', 2)
  ]
  await ensureMerchantAdmin(platformId, merchants[0])
  const categories = await ensureCategories()
  const source = await fetchProducts()
  const products = await seedProducts(platformId, merchants, categories, source)
  const couponActivityIds = await seedMarketing(platformId, merchants[0], products)
  const users = await ensureCustomers(8)
  await claimCoupons(users, couponActivityIds)

  if (SKIP_ORDERS) {
    log('已跳过订单填充（--skip-orders）')
  } else {
    await sleep(3000) // 等商品创建事件在库存服务初始化库存
    const stats = await seedOrders(platformId, products, users)
    log(`订单填充：新增 ${stats.orders} 笔（已支付 ${stats.paid} / 已发货 ${stats.shipped}）`)
    const refunded = await seedRefund(users)
    if (refunded) log('退款演示数据已生成')
  }
  await summary()
})().catch(error => {
  console.error('[seed] 失败：', error.message)
  process.exit(1)
})
