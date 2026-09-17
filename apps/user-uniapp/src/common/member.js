/**
 * 会员等级（展示用）：按累计实付消费在前端计算，用于会员卡进度与权益文案。
 * 说明：会员体系尚未在后端建模（无独立成长值/积分表），这里只做展示，不参与价格与折扣计算。
 */
export const MEMBER_TIERS = [
  { name: '普通会员', threshold: 0 },
  { name: '心享会员', threshold: 3000 },
  { name: '钻石会员', threshold: 10000 }
]

/** 根据累计消费返回当前等级、下一等级与升级进度（percent 0-100）。 */
export const memberTierOf = spent => {
  const value = Number(spent || 0)
  const current = [...MEMBER_TIERS].reverse().find(tier => value >= tier.threshold) || MEMBER_TIERS[0]
  const next = MEMBER_TIERS.find(tier => tier.threshold > value) || null
  return {
    name: current.name,
    spent: value,
    next: next?.name || '',
    remain: next ? Math.max(0, next.threshold - value) : 0,
    percent: next ? Math.min(100, Math.round((value / next.threshold) * 100)) : 100
  }
}
