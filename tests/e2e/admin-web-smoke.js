#!/usr/bin/env node
const assert = require('node:assert');

(async () => {
  const response = await fetch('http://127.0.0.1:8081/');
  const html = await response.text();
  assert.equal(response.status, 200);
  assert.match(html, /<title>SimpleShop 管理端<\/title>/);
  for (const id of ['orderForm', 'paymentForm', 'stockForm', 'productForm']) {
    assert(html.includes(`id="${id}"`), `管理端缺少 ${id}`);
  }
  for (const endpoint of ['orders/Create', 'payments/Create', 'payments/Confirm']) {
    assert(html.includes(endpoint), `管理端缺少接口 ${endpoint}`);
  }
  console.log('管理端冒烟测试通过：页面可访问，核心表单与网关接口已配置。');
})().catch(error => {
  console.error(error.message);
  process.exit(1);
});
