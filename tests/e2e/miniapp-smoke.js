#!/usr/bin/env node
const assert = require('node:assert');
const fs = require('node:fs');
const path = require('node:path');

// 用 Node 模拟 wx.request，只验证小程序请求封装与真实网关契约。
global.wx = {
  request(options) {
    fetch(options.url, {
      method: options.method || 'GET',
      headers: options.header,
      body: options.method === 'GET' ? undefined : JSON.stringify(options.data)
    }).then(async response => options.success({
      statusCode: response.status,
      data: await response.json()
    })).catch(options.fail);
  }
};

const app = JSON.parse(fs.readFileSync(path.join(__dirname, '../../apps/user-miniapp/app.json'), 'utf8'));
for (const page of app.pages) {
  for (const ext of ['js', 'json', 'wxml']) {
    const file = path.join(__dirname, '../../apps/user-miniapp', `${page}.${ext}`);
    assert(fs.existsSync(file), `缺少小程序文件：${page}.${ext}`);
  }
}

const api = require(path.join(__dirname, '../../apps/user-miniapp/utils/api'));
const config = require(path.join(__dirname, '../../apps/user-miniapp/utils/config'));
api.request(`products/GetProductDetail?id=${config.defaultProductId}`).then(data => {
  assert(data.product && data.skus.length > 0, '商品详情契约不完整');
  console.log('小程序冒烟测试通过：页面文件完整，商品详情可通过网关访问。');
}).catch(error => {
  console.error(error.message);
  process.exit(1);
});
