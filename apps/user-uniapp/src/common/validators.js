export const PHONE_PATTERN = /^1[3-9]\d{9}$/
export const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/

export const isPhone = (value) => PHONE_PATTERN.test(String(value || '').trim())
export const isEmail = (value) => EMAIL_PATTERN.test(String(value || '').trim())
export const isStrongPassword = (value) => {
  const text = String(value || '')
  return text.length >= 8 && /[A-Za-z]/.test(text) && /\d/.test(text)
}

export const toast = (title) => {
  uni.showToast({ title, icon: 'none' })
  return false
}

// 与后端 Validator 对齐的提交前校验：返回 true 表示通过，否则已弹 toast。
export const validateLogin = form => {
  if (!String(form.userName || '').trim()) return toast('请输入用户名')
  if (String(form.userName).trim().length > 64) return toast('用户名不能超过64个字符')
  if (!form.password) return toast('请输入密码')
  return true
}

export const validateRegister = form => {
  const userName = String(form.userName || '').trim()
  if (userName.length < 3 || userName.length > 64) return toast('用户名必须为3-64个字符')
  if (!isStrongPassword(form.password)) return toast('密码至少8位且包含字母和数字')
  if (form.confirmPassword !== undefined && form.password !== form.confirmPassword) return toast('两次输入的密码不一致')
  if (form.email && !isEmail(form.email)) return toast('邮箱格式不正确')
  if (!isPhone(form.phone)) return toast('手机号格式不正确')
  return true
}

export const validateAddress = form => {
  if (!String(form.receiverName || '').trim()) return toast('请填写收货人')
  if (String(form.receiverName).trim().length > 32) return toast('收货人不能超过32个字符')
  if (!isPhone(form.receiverPhone)) return toast('手机号格式不正确')
  if (!String(form.province || '').trim() || !String(form.city || '').trim() || !String(form.district || '').trim()) return toast('请选择省市区')
  if (!String(form.detail || '').trim()) return toast('请填写详细地址')
  if (String(form.detail).trim().length > 255) return toast('详细地址不能超过255个字符')
  return true
}

export const validateQuantity = (value, max = 99) => {
  const quantity = Number(value)
  if (!Number.isInteger(quantity) || quantity < 1) return toast('数量必须为大于0的整数')
  if (quantity > max) return toast(`数量不能超过${max}`)
  return true
}

export const trimStrings = form => {
  Object.keys(form).forEach(key => {
    if (typeof form[key] === 'string') form[key] = form[key].trim()
  })
  return form
}
