export const PHONE_PATTERN = /^1[3-9]\d{9}$/
export const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/
export const HEX_COLOR_PATTERN = /^#[0-9a-fA-F]{6}$/
export const PLATFORM_CODE_PATTERN = /^[a-zA-Z][a-zA-Z0-9_-]{2,31}$/
export const ROLE_CODE_PATTERN = /^[a-z][a-z0-9-]{2,79}$/
export const PERMISSION_CODE_PATTERN = /^[a-z][a-z0-9:-]{2,79}$/

export const isPhone = (value) => PHONE_PATTERN.test(String(value || '').trim())
export const isEmail = (value) => EMAIL_PATTERN.test(String(value || '').trim())
export const isHexColor = (value) => HEX_COLOR_PATTERN.test(String(value || '').trim())

export const isStrongPassword = (value) => {
  const text = String(value || '')
  return text.length >= 8 && /[A-Za-z]/.test(text) && /\d/.test(text)
}

// 与后端 FluentValidation 对齐的可选格式规则（留空放行，填了必须合法）。
export const optionalPattern = (pattern, message) => ({
  validator: (rule, value, callback) =>
    !value || pattern.test(String(value).trim()) ? callback() : callback(new Error(message)),
  trigger: 'blur'
})

export const requiredRule = (message, trigger = 'blur') => ({ required: true, message, trigger })

export const lengthRule = (min, max, label) => ({
  validator: (rule, value, callback) => {
    const text = String(value || '').trim()
    if (!text) return callback(new Error(`请填写${label}`))
    if (text.length < min || text.length > max) return callback(new Error(`${label}必须为${min}-${max}个字符`))
    callback()
  },
  trigger: 'blur'
})

export const maxLengthRule = (max, label) => ({
  validator: (rule, value, callback) => {
    const text = String(value || '').trim()
    return !text || text.length <= max ? callback() : callback(new Error(`${label}不能超过${max}个字符`))
  },
  trigger: 'blur'
})

// 金额：>0 且最多两位小数（与后端金额校验一致）。
export const amountRule = (message = '金额必须大于0', max = null) => ({
  validator: (rule, value, callback) => {
    const amount = Number(value)
    if (!Number.isFinite(amount) || amount <= 0) return callback(new Error(message))
    if (Math.round(amount * 100) !== amount * 100) return callback(new Error('金额最多保留两位小数'))
    if (max !== null && amount > Number(max)) return callback(new Error(`金额不能超过 ${max}`))
    callback()
  },
  trigger: 'blur'
})

export const integerRule = (min, max, label) => ({
  validator: (rule, value, callback) => {
    const number = Number(value)
    if (!Number.isInteger(number)) return callback(new Error(`${label}必须为整数`))
    if (number < min || number > max) return callback(new Error(`${label}必须为${min}-${max}`))
    callback()
  },
  trigger: 'blur'
})

// 提交前统一 trim 字符串字段，避免纯空格通过 required 校验。
export const trimForm = (form) => {
  Object.keys(form).forEach((key) => {
    if (typeof form[key] === 'string') form[key] = form[key].trim()
  })
  return form
}
