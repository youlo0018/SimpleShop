/**
 * 装修图标工具：icon 字段既支持线性图标图片路径（/static/line/*.png、https://...），
 * 也兼容历史 emoji 配置；页面据此决定渲染 <image> 还是文本。
 */
export const isImageIcon = icon => typeof icon === 'string' && (icon.startsWith('/static/') || icon.startsWith('http'))
