import { post } from '@/common/request'

/**
 * 到手价（京东/淘宝式）：商品列表与详情展示"原价 - 活动/最优券 = 到手价"。
 * 价格口径统一由后端营销引擎 `POST /marketing/FinalPrice` 计算，前端只做展示，避免自行拼算导致与结算不一致。
 */

/** 金额格式化：统一两位小数（与结算/订单口径一致）。 */
export const formatMoney = value => Number(value || 0).toFixed(2)

/** 可售 SKU：未下架且价格有效。 */
const activeSkus = item => (item?.skus || []).filter(sku => sku.isActive !== false && Number(sku.price) > 0)

/** 列表展示用 SKU：取价格最低的可售 SKU（与商品卡"¥起"口径一致）。 */
const cheapestSku = item => activeSkus(item).reduce((min, sku) => (!min || Number(sku.price) < Number(min.price) ? sku : min), null)

/**
 * 读取到手价展示信息（不发请求，读取 enrichFinalPrices 写入的字段）。
 * @param {object} item 商品（含 skus；sku 上可能带 finalPrice/discountAmount/discountLabel）。
 * @returns {{price:string, finalPrice:string, hasDiscount:boolean, discountAmount:number, discountLabel:string, sourceName:string}} 展示用价格信息。
 */
export const priceInfo = item => {
  const sku = cheapestSku(item)
  if (!sku) return { price: '--', finalPrice: '--', hasDiscount: false, discountAmount: 0, discountLabel: '', sourceName: '' }
  const price = Number(sku.price)
  const finalPrice = Number(sku.finalPrice ?? price)
  return {
    price: formatMoney(price),
    finalPrice: formatMoney(finalPrice),
    hasDiscount: finalPrice < price,
    discountAmount: Number(sku.discountAmount || 0),
    discountLabel: sku.discountLabel || '',
    sourceName: sku.dealSourceName || ''
  }
}

/** 到手价数值（排序用）：无优惠时回落原价。 */
export const finalPriceOf = item => {
  const sku = cheapestSku(item)
  return Number(sku?.finalPrice ?? sku?.price ?? 0)
}

/**
 * 批量填充到手价：整页商品聚合成一次请求（去重后按 50 个/批），失败静默回退原价展示。
 * @param {Array} items 商品列表（原地挂载 sku.finalPrice / discountAmount / discountLabel）。
 * @param {number|string} platformId 当前平台 ID。
 * @returns {Promise<Array>} 原列表。
 */
export const enrichFinalPrices = async (items, platformId) => {
  const list = (items || []).filter(Boolean)
  if (!list.length || !platformId) return list
  const requestItems = []
  const seen = new Set()
  for (const item of list) {
    for (const sku of activeSkus(item)) {
      const key = String(sku.id)
      if (seen.has(key)) continue
      seen.add(key)
      requestItems.push({
        skuId: sku.id,
        platformId: sku.platformId || platformId,
        merchantId: sku.merchantId || item.merchantId || 0,
        productName: item.name,
        price: Number(sku.price),
        quantity: 1
      })
    }
  }
  if (!requestItems.length) return list

  const results = []
  for (let index = 0; index < requestItems.length; index += 50) {
    const data = await post('/marketing/FinalPrice', { platformId, items: requestItems.slice(index, index + 50) }).catch(() => null)
    results.push(...(data?.items || []))
  }
  const bySku = new Map(results.map(row => [String(row.skuId), row]))
  for (const item of list) {
    for (const sku of item.skus || []) {
      const row = bySku.get(String(sku.id))
      if (!row) continue
      const activity = Number(row.activityDiscount || 0)
      const coupon = Number(row.couponDiscount || 0)
      sku.finalPrice = Number(row.finalPrice)
      sku.discountAmount = activity + coupon
      sku.dealSourceName = row.couponName || row.activityName || ''
      sku.discountLabel = coupon > 0 ? `券减¥${formatMoney(coupon)}`
        : activity > 0 ? `活动减¥${formatMoney(activity)}`
          : Number(row.giftCouponActivityId) > 0 ? '赠券' : ''
    }
  }
  return list
}
