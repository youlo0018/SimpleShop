const currentUser = () => JSON.parse(localStorage.getItem('admin_user') || 'null')

/** 平台租户账号（platformId>0）：看不到全局平台管理，也不需要选择平台。 */
export const isPlatformScoped = () => Number(currentUser()?.platformId || 0) > 0

/** 商户租户账号：只能操作本商户，不需要选择商户。 */
export const isMerchantScoped = () => currentUser()?.tenantType === 'merchant'

/** 当前登录账号的平台 ID（全局超管为空；雪花 ID 必须保持字符串，Number 会丢精度）。 */
export const currentPlatformId = () => currentUser()?.platformId || ''

/** 当前登录账号的商户 ID（非商户账号为空）。 */
export const currentMerchantId = () => currentUser()?.merchantId || ''
