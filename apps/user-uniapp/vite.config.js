import fs from 'node:fs'
import path from 'node:path'
import { defineConfig } from 'vite'
import uni from '@dcloudio/vite-plugin-uni'

// uni-app 编译微信小程序时会把 App.vue 的全局样式写成 App.wxss、根组件配置写成 App.json（组件 JS 已改名为 App2.js），
// 但微信只加载 app.wxss，且大小写不敏感的文件系统上这些文件会与 app.wxss / app.json 互相覆盖。
// 因此在产物写入后归一化：全局样式合并进 app.wxss，组件配置改名 App2.json 与 App2.js 对应，并移除大小写冲突文件。
const normalizeMpWeixinOutput = () => ({
  name: 'normalize-mp-weixin-output',
  apply: 'build',
  writeBundle(options) {
    if (process.env.UNI_PLATFORM !== 'mp-weixin' || !options.dir) return
    const dir = options.dir
    const appWxss = path.join(dir, 'app.wxss')
    const componentWxss = path.join(dir, 'App.wxss')
    const componentJson = path.join(dir, 'App.json')
    if (fs.existsSync(componentWxss)) {
      const globalStyles = fs.readFileSync(componentWxss, 'utf8')
      if (fs.existsSync(appWxss)) fs.appendFileSync(appWxss, `\n${globalStyles}`)
      fs.rmSync(componentWxss)
    }
    if (fs.existsSync(componentJson)) fs.renameSync(componentJson, path.join(dir, 'App2.json'))
  }
})

export default defineConfig({ plugins: [uni(), normalizeMpWeixinOutput()] })
