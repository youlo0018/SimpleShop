# SimpleShop 测试用例

> 版本：2026-09-17。覆盖 12 个后端服务 + 网关 + 管理后台 + 商城小程序（H5/微信小程序）。
> 用例编号：`模块-序号`；优先级：P0（主链路/资金/安全）、P1（核心功能/校验）、P2（体验/边界）。
> 「自动化」列：`api` = `tests/e2e/api-regression.sh`；`ui` = `tests/e2e/ui-regression.js`；`flow` = `tests/e2e/business-flow.sh` / `marketing-flow.sh`；`手工` = 需人工核对（样式/交互）。
> 执行方式见文末「执行与约定」。

## 1. 认证与登录态（AUTH）

| ID | 优先级 | 前置 | 步骤 | 期望结果 | 自动化 |
|----|--------|------|------|----------|--------|
| AUTH-01 | P0 | 存在账号 codexadmin/Admin123456 | POST `/gateway/users/Login` 提交正确账号密码 | HTTP 200，`data.token` 非空、`data.user.userName=codexadmin`；JWT 含 `tenant_type/permission` 声明 | api/flow |
| AUTH-02 | P0 | 同上 | POST 提交错误密码 | body.code=400，message 含"用户名或密码错误"，不返回 token | api |
| AUTH-03 | P1 | 无 | POST 提交不存在用户名 | body.code=400，不泄露账号是否存在 | api |
| AUTH-04 | P1 | 无 | POST 用户名超过 64 字符 | HTTP 400（FluentValidation），`errors.UserName` 有消息 | api |
| AUTH-05 | P1 | 无 | POST 密码为空 | HTTP 400，`errors.Password` 非空 | api |
| AUTH-06 | P0 | 已签发 token 的客户 | 访问需要后台权限的接口（如 `GET /gateway/users/Users`） | HTTP 403；不得放行 | api |
| AUTH-07 | P0 | 无 token | 访问受保护接口 | HTTP 401，body.message=请先登录 | api |
| AUTH-08 | P1 | 伪造/畸形 JWT（非 base64 头） | 携带 `Authorization: Bearer not-a-jwt` 访问受保护接口 | HTTP 401（不得 500） | api |
| AUTH-09 | P1 | 过期 JWT | 携带过期 token 访问受保护接口 | HTTP 401，body.message=请先登录 | api |
| AUTH-10 | P1 | 无 | 注册：用户名 3-64、密码≥8 含字母数字、手机号 `^1[3-9]\d{9}$`、邮箱格式 | 全部合法 → 200 并自动登录；任一非法 → HTTP 400 且字段级 errors | api |
| AUTH-11 | P1 | 已存在用户名 | 重复注册同一用户名 | HTTP 400，message 提示已存在 | api |
| AUTH-12 | P2 | 客户 token | 小程序登录态失效（token 被清/过期）访问券包 | UI 自动清会话并跳转 `/pages/auth/login` | ui |

## 2. 用户 / 地址 / 收藏（USER）

| ID | 优先级 | 前置 | 步骤 | 期望结果 | 自动化 |
|----|--------|------|------|----------|--------|
| USER-01 | P1 | 后台 token | `GET /gateway/users/Users?page=1&pageSize=10` | 200，`items/total/page/pageSize` 结构完整，total 与数据一致 | api/flow |
| USER-02 | P1 | 后台 token | `pageSize=1000` / `page=0` | HTTP 400（分页范围校验） | api |
| USER-03 | P1 | 后台 token | 创建用户：非法手机号 / 弱密码 / 非法邮箱 | 各返回 HTTP 400 且 errors 指向对应字段 | api |
| USER-04 | P1 | 后台 token | 创建合法用户（customer） | 200；该用户可登录且权限为普通客户 | flow |
| USER-05 | P1 | 后台 token | `UpdateStatus` 禁用刚创建用户 → 用该账号登录 | 登录被拒（IsEnabled=false） | 手工 |
| USER-06 | P1 | 客户 token | `GET /gateway/users/Profile` | 200，返回本人资料；不得返回他人 | api |
| USER-07 | P1 | 客户 token | 保存地址：省市区/收货人/详细地址合法 | 200；`GET /gateway/users/Addresses` 可见且 `userId` 为本人 | flow |
| USER-08 | P1 | 客户 token | 保存地址：手机号 123 / 详细地址超 255 | HTTP 400，字段级 errors | api |
| USER-09 | P1 | 两个客户 A/B | A 保存地址 → B 尝试编辑该地址 id | 404/无权；不得越权修改 | 手工 |
| USER-10 | P1 | 客户 token | 收藏商品 → 再次调用取消收藏 | 第一次 `favorited=true`，第二次 `favorited=false`；`GET /users/Favorites` 数量正确 | flow |
| USER-11 | P1 | 客户 token | `ToggleFavorite` 传 productId=0 | HTTP 400 | api |
| USER-12 | P2 | 客户 token | 地址设为默认后新增另一默认地址 | 始终只有一个默认地址（唯一默认） | 手工 |

## 3. 网关鉴权与多租户隔离（GW/TEN）

| ID | 优先级 | 前置 | 步骤 | 期望结果 | 自动化 |
|----|--------|------|------|----------|--------|
| GW-01 | P0 | 外部请求伪造 `X-Claim-UserId/PlatformId` | 带伪造头访问订单/用户接口（无 token） | 401；网关先清除伪造头，下游不信任外部注入 | api |
| GW-02 | P0 | 商户账号 A | 访问 `GET /gateway/orders/Detail` 查询其他商户订单 | 403（无权）或 404；不得返回数据 | api |
| GW-03 | P0 | 商户账号 A | `POST /gateway/products/Update` 修改商户 B 的商品 | 403；商品未被修改 | api |
| GW-04 | P0 | 平台 token | 创建商户后商户列表仅包含本平台数据（平台账号） | 列表中 `platformId` 均为本平台 | 手工 |
| GW-05 | P0 | 客户 token | `POST /gateway/orders/Cancel` 传 `overrideCustomerScope=true` 取消他人订单 | 越权被忽略（控制器强制回填租户），目标订单状态不变 | api |
| GW-06 | P1 | 无 token | 访问 `/gateway/marketing/ActiveActivities`（公开接口） | 200；仅返回展示字段 | api |
| GW-07 | P1 | 无 token | 访问 `/gateway/merchants/Shop?id=已入驻商户` | 200，仅店铺展示字段（无联系人/佣金） | api |
| GW-08 | P1 | 后台 token | `GET /gateway/marketing/ActivityList` | 200；未授权客户访问同接口 403 | api |
| GW-09 | P2 | 平台 token | 权限中心角色/权限列表 | 200；权限点含 `marketing:read/marketing:create` | 手工 |

## 4. 分类（CAT）

| ID | 优先级 | 前置 | 步骤 | 期望结果 | 自动化 |
|----|--------|------|------|----------|--------|
| CAT-01 | P1 | 后台 token | 创建一级分类 | 200，返回分类实体（含 id/name/parentId=0） | api |
| CAT-02 | P1 | 已有一/二/三级 | 在三级的父级下再创建（即第四级） | 400，message 提示不能超过 3 级 | api |
| CAT-03 | P1 | 后台 token | 分类名为空 | HTTP 400 | api |
| CAT-04 | P1 | 已有分类 | `GetCategoryTree` | 树形结构 ≤3 级，节点含 isActive | flow |
| CAT-05 | P2 | 分类下挂有商品 | 删除该分类 | 被拒绝（提示存在商品或子分类） | 手工 |
| CAT-06 | P1 | 后台 token | 禁用分类后小程序分类页 | 停用分类不展示（`isActive=false` 过滤） | 手工 |

## 5. 商品与 SKU（PRD）

| ID | 优先级 | 前置 | 步骤 | 期望结果 | 自动化 |
|----|--------|------|------|----------|--------|
| PRD-01 | P0 | 后台 token + 分类 | 创建商品：名称/主图/分类/SKU（编码、售价>0、库存>0） | 200，返回商品 id；`AdminDetail` 可见 SKU | flow/api |
| PRD-02 | P1 | 同上 | SKU 售价 ≤0 / 库存 ≤0 / 原价低于售价 | HTTP 400，errors 指向 SKU 字段 | api |
| PRD-03 | P1 | 同上 | 名称超过 40 字符 / 主图为空 | HTTP 400 | api |
| PRD-04 | P1 | 同上 | 两个 SKU 使用相同编码（忽略大小写） | HTTP 400，提示编码重复 | api |
| PRD-05 | P0 | 已创建商品 | `PublishProduct` 上架 → 小程序 `products/List?status=1` | 上架成功且出现在列表；下架后不出现 | flow |
| PRD-06 | P1 | 已上架商品 | 编辑商品：修改名称与 SKU 价格/库存 | 200；`AdminDetail` 反映新值 | api |
| PRD-07 | P1 | 已上架商品 | 编辑时传入非法 SKU（价格 0） | HTTP 400（不得静默跳过） | api |
| PRD-08 | P1 | 无 | 商品列表分页 `pageSize=1000` | HTTP 400 | api |
| PRD-09 | P2 | 后台 token | 商品详情传不存在的 id | 404 | api |

## 6. 图片上传（IMG）

| ID | 优先级 | 前置 | 步骤 | 期望结果 | 自动化 |
|----|--------|------|------|----------|--------|
| IMG-01 | P1 | 后台 token | 上传合法 PNG（真实文件头） | 200，返回 `/gateway/products/File/{id}`，可访问 | api |
| IMG-02 | P1 | 后台 token | 上传文本文件伪装 `.png` | 400（文件头魔数校验失败） | api |
| IMG-03 | P1 | 后台 token | 上传扩展名不允许（.svg） | 400 | api |
| IMG-04 | P2 | 后台 token | 上传超过 2MB 图片 | 400（大小限制） | 手工 |

## 7. 购物车（CART）

| ID | 优先级 | 前置 | 步骤 | 期望结果 | 自动化 |
|----|--------|------|------|----------|--------|
| CART-01 | P0 | 客户 token | 加购 SKU（数量 1，带图片） | 200；`carts/Get` 返回该条目，数量 1，image 保留 | api |
| CART-02 | P0 | 同上 | 再加购同一 SKU 数量 +1（增量语义） | 数量变为 2（不是 3/倍数） | api/ui |
| CART-03 | P1 | 同上 | 加购数量 0 / 价格 -1 / 数量累计 >99 | HTTP 400（对应字段 errors） | api |
| CART-04 | P1 | 客户 token | `carts/Remove` 移除条目 | 200；`carts/Get` 不再包含该 SKU | api |
| CART-05 | P1 | 客户 token | 加购后小程序购物车点击「+」一次 | 数量 1→2；图片显示商品图（非占位） | ui |
| CART-06 | P2 | 未登录 | 调用 `carts/Get` | 401（请先登录），前端跳登录 | ui |

## 8. 库存（INV）

| ID | 优先级 | 前置 | 步骤 | 期望结果 | 自动化 |
|----|--------|------|------|----------|--------|
| INV-01 | P0 | 已上架商品 | 下单（含 stockItems） | 库存锁定：`stock.locked` 增加，`stock_flow` 有 `lock` 流水 | flow |
| INV-02 | P0 | 支付成功 | 等待事件消费 | `locked` 减少、`quantity` 扣减，流水有 `deduct` 且 BizNo+SKU+动作幂等 | flow |
| INV-03 | P1 | 同一 BizNo 重复锁库存 | 重复调用 Lock | 幂等：不重复扣减，返回成功 | 手工 |
| INV-04 | P0 | 库存不足 | 下单数量 > 可售 | 下单失败「库存不足或锁定失败」，订单未创建，已占券回退 | flow |
| INV-05 | P1 | 超时关单 | 等待 Scheduled 关单 | 释放库存（`release` 流水），失败写补偿表 `pending_stock_release` 下轮重试 | 手工 |
| INV-06 | P1 | 退款审批通过且传 items | 观察库存 | 按退款 items 回补库存，流水幂等键为 refundNo | flow |
| INV-07 | P1 | 内部接口 | `LockStock` 传空 items / bizNo 为空 | HTTP 400（Validator） | api |

## 9. 订单（ORD）

| ID | 优先级 | 前置 | 步骤 | 期望结果 | 自动化 |
|----|--------|------|------|----------|--------|
| ORD-01 | P0 | 客户 token | 下单（合法地址/手机号/明细） | `success=true`，返回 orderNo/paymentPrice/paymentExpiredAt；订单状态 10（待支付） | flow |
| ORD-02 | P0 | 同上 | 用同一 idempotencyKey 再次下单 | 拒绝（请勿重复提交订单），不产生第二条订单 | api |
| ORD-03 | P1 | 客户 token | 下单手机号非法 / 数量 100 | HTTP 400 | api |
| ORD-04 | P1 | 客户 token | 下单价与商品无关（客户端传价格） | 订单总价按提交行计算；营销服务按行计算优惠（风险见 REVIEW） | flow |
| ORD-05 | P0 | 客户 token | 查询订单列表/详情 | 仅返回本人订单；后台按租户裁剪 | flow |
| ORD-06 | P0 | 商户 token | 对已支付订单发货（物流公司/单号/明细） | 200；订单状态 40；物流信息落库 | flow |
| ORD-07 | P0 | 客户 token | 确认收货（本人发货单） | 200；订单状态 50（已完成） | flow |
| ORD-08 | P1 | 发货明细为空 | 发货 | HTTP 400（请选择发货商品） | api |
| ORD-09 | P0 | 待支付订单 | 客户取消订单 | 200；状态 90；占用券回退（可再次使用） | api |
| ORD-10 | P1 | 已取消订单 | 再次取消 | 400「当前订单状态不可取消」 | api |
| ORD-11 | P1 | 非法订单 id | 查询详情 | 404/无权 | api |
| ORD-12 | P0 | 订单创建后未支付 | 等待支付超时（15 分钟，测试可用 Scheduled 手动触发） | 状态 91（已关闭），库存释放，发 `order.cancelled` | 手工 |
| ORD-13 | P1 | 后台/商户 token | 订单报表 `reports/Report` | 200；DAU/订单/GMV/趋势字段齐全 | api/flow |

## 10. 支付（PAY）

| ID | 优先级 | 前置 | 步骤 | 期望结果 | 自动化 |
|----|--------|------|------|----------|--------|
| PAY-01 | P0 | 待支付订单 | 创建支付单（bizNo=订单号） | 200；支付单状态 10 | flow |
| PAY-02 | P0 | 同上 | 同一 bizNo 重复创建 | 幂等：返回同一支付单，不重复落单 | api |
| PAY-03 | P0 | 支付单 | 确认支付（Confirm） | 200；状态 20；发 `payment.succeeded`（含 stockItems） | flow |
| PAY-04 | P1 | 已支付 | 再次 Confirm | 幂等补发事件，不重复扣款（状态仍 20） | api |
| PAY-05 | P1 | 客户 token | 支付他人支付单（bizNo） | 403/无权 | 手工 |
| PAY-06 | P1 | 无 | 创建支付：金额 ≤0 / items 为空 / 数量 0 | HTTP 400（Validator） | api |
| PAY-07 | P1 | 无 | 确认支付：bizNo 为空 / items 为空 | HTTP 400 | api |
| PAY-08 | P1 | 订单已支付 | `orders/Get` | orderStatus=20、isPayment=true、paymentAt 非空 | flow |

## 11. 退款（REF）

| ID | 优先级 | 前置 | 步骤 | 期望结果 | 自动化 |
|----|--------|------|------|----------|--------|
| REF-01 | P0 | 已支付订单 | 客户申请退款（金额=实付，items） | 200；退款单状态 10；不发事件 | flow |
| REF-02 | P1 | 同上 | 再次申请（累计 > 实付） | 400「累计退款不能超过实付金额」 | api |
| REF-03 | P1 | 同上 | 退款金额 ≤0 / 原因为空 | HTTP 400 | api |
| REF-04 | P0 | 退款单待审批 | 平台/商户同意 | 200；退款单 20；发 `payment.refunded`；订单 60（全额）/仅标记（部分）；库存回补 | flow |
| REF-05 | P1 | 待审批 | 拒绝并填原因（空原因用默认文案） | 200；退款单 90；订单与库存无变化 | api |
| REF-06 | P1 | 已处理退款单 | 再次审批 | 400「当前退款状态不可审批」 | api |
| REF-07 | P1 | 客户 A 的退款单 | 客户 B 审批/查看 | 403/无权 | 手工 |
| REF-08 | P2 | 部分退款 | 查看订单 | `isRefund=true` 且订单状态不变；全额时 isAllRefund=true | 手工 |

## 12. 营销-活动（ACT）

| ID | 优先级 | 前置 | 步骤 | 期望结果 | 自动化 |
|----|--------|------|------|----------|--------|
| ACT-01 | P1 | 平台 token | 创建满减活动（全平台，门槛/金额合法） | 200，返回活动 id；列表可见 | api/flow |
| ACT-02 | P1 | 平台 token | 创建满折活动（折扣率 0.9） | 200；列表优惠文案「满X打9.0折」 | ui |
| ACT-03 | P1 | 平台 token | 创建满赠活动（指定券活动） | 200；未选赠品券活动 → HTTP 400 | api |
| ACT-04 | P1 | 平台 token | 创建指定商户活动（scopeType=2 无商户） | HTTP 400（必须选择商户） | api |
| ACT-05 | P1 | 平台 token | 创建指定商品活动（scopeType=3 无商品） | HTTP 400（必须选择商品） | api |
| ACT-06 | P1 | 商户 token | 商户创建「指定商户」范围活动 | 400（商户活动不支持指定商户范围） | api |
| ACT-07 | P1 | 已有活动 | 启停活动 | 200；停用后不再参与结算（preview 无该活动） | api |
| ACT-08 | P1 | 活动门槛 100 | 商品行小计 99 / 100 | 99 不命中的；100 命中（边界） | api |
| ACT-09 | P1 | 后台 token | 活动列表分页 / 详情含范围 | 200；详情 targets 与创建一致 | ui |
| ACT-10 | P1 | 无 | 进行中活动公开接口（游客） | 200；只含有效期内且启用的活动 | api |

## 13. 营销-券模板 / 券活动 / 券包（CPN）

| ID | 优先级 | 前置 | 步骤 | 期望结果 | 自动化 |
|----|--------|------|------|----------|--------|
| CPN-01 | P1 | 平台 token | 创建券模板（满减/满折/0元减） | 200；列表类型与优惠文案正确 | api/ui |
| CPN-02 | P1 | 平台 token | 0元减模板门槛传 100 | 200 且门槛被强制为 0 | api |
| CPN-03 | P1 | 平台 token | 满折折扣率 1.5 / 0 | HTTP 400 | api |
| CPN-04 | P1 | 平台 token | 创建券活动（使用模板，发行量/限领） | 200；列表带模板名/优惠 | api/flow |
| CPN-05 | P1 | 平台 token | 券活动引用不存在/他人模板 | 400 | api |
| CPN-06 | P0 | 客户 token | 领券中心列表 | 200；含剩余库存与 canClaim 标记 | flow |
| CPN-07 | P0 | 客户 token | 领取优惠券 | 200；券包出现该券（未使用）；`IssuedCount+1` | flow |
| CPN-08 | P0 | 已达每人限领 | 再次领取 | 400「已达到每人限领数量」 | flow |
| CPN-09 | P1 | 发行量仅 1 张 | 两个用户先后领取 | 第二个 400「已被领完」；不超发 | api |
| CPN-10 | P1 | 有券客户 | 我的券包按状态过滤 | 200；未使用/已使用/已过期分组正确 | ui |
| CPN-11 | P2 | 过期券 | 券包查看 | 展示状态为已过期，且不可用于结算 | 手工 |
| CPN-12 | P1 | 未登录 | 领取优惠券 | 401/需登录（前端跳登录页） | ui |

## 14. 优惠计算引擎（CAL）

| ID | 优先级 | 前置 | 步骤 | 期望结果 | 自动化 |
|----|--------|------|------|----------|--------|
| CAL-01 | P0 | 有可用券 + 活动 | 结算预览（券优先配置） | 命中券折扣，活动折扣为 0；逐商品互斥 | flow |
| CAL-02 | P0 | 同上 | 平台配置切换为活动优先后再预览 | 命中活动折扣，券折扣为 0 | flow |
| CAL-03 | P0 | 商品行 120，券 0元减5 | 预览 | couponDiscount=5.00，实付 115.00 | api |
| CAL-04 | P1 | 券为 0元减 5、商品行 5 元 | 预览 | 券不命中（券后价必须 >0），折扣 0 | api |
| CAL-05 | P1 | 满减金额大于行金额 | 预览 | 单行最多抵扣到 0.01（payAmount=0.01），不出现 0/负数 | api |
| CAL-06 | P1 | 同商品行命中多个活动 | 预览 | 取折扣最大的活动（逐商品贪心） | api |
| CAL-07 | P1 | 用户勾选部分券 | 预览（selectedUserCouponIds 显式列表） | 只按勾选券计算 | flow/ui |
| CAL-08 | P1 | 勾选已使用/过期券 | 预览/下单 | 返回失败「选择的优惠券不可用或已过期」 | api |
| CAL-09 | P1 | 满赠活动（无其他折扣可用） | 预览 | 命中满赠（discount 0），订单支付成功后发券 | flow |
| CAL-10 | P1 | 满赠 + 可用于折扣的券 | 预览（活动优先） | 优先折扣（满赠只在无折扣时兜底），避免用户吃亏 | api |
| CAL-11 | P1 | 指定商品范围活动 | 预览不同 SKU | 范围内 SKU 命中，范围外不命中 | api |
| CAL-12 | P1 | 跨商户商品同单 | 预览 | 商户活动只作用于其商户商品 | 手工 |
| CAL-13 | P1 | 结算预览（游客/未登录） | 调用 SettlePreview | 未登录：仅活动；登录：含本人券 | api |

## 15. 营销-下单落账 / 赠券 / 回退 / 报表（MKT）

| ID | 优先级 | 前置 | 步骤 | 期望结果 | 自动化 |
|----|--------|------|------|----------|--------|
| MKT-01 | P0 | 客户有券 | 带 `selectedUserCouponIds` 下单 | 订单 `allDiscountPrice/couponDiscountPrice/paymentPrice` 正确；明细带营销快照 | flow |
| MKT-02 | P0 | 同上 | 下单后查看券 | 券状态=已使用，`UsedOrderNo`=订单号 | flow |
| MKT-03 | P1 | 下单失败（如库存不足） | 查看券 | 券回退为未使用 | api |
| MKT-04 | P1 | 取消订单 | 查看券 | 券回退为未使用（order.cancelled 消费） | api |
| MKT-05 | P1 | 使用券下单 | 查询用券记录 | `CouponRecord(+Item)` 一条（订单号幂等），报表汇总包含该单 | flow |
| MKT-06 | P0 | 满赠活动订单 | 支付成功 | 用户券包收到赠品券（来源=满赠）；活动记录 `GiftCouponCount=1` | flow |
| MKT-07 | P1 | 同一满赠订单 | 重复投递支付事件 | 赠券不重复发放（GiftCouponCount 幂等） | 手工 |
| MKT-08 | P1 | 参与活动订单 | 活动报表 | 汇总订单数/折扣总额/赠券数与记录明细一致 | flow |
| MKT-09 | P1 | 券订单 | 券报表 | 核销笔数/抵扣总额与订单折扣一致 | flow |
| MKT-10 | P1 | 租户隔离 | 商户只看本商户活动/券报表 | 不含其他商户数据 | 手工 |

## 16. 参数校验（VAL）

| ID | 优先级 | 前置 | 步骤 | 期望结果 | 自动化 |
|----|--------|------|------|----------|--------|
| VAL-01 | P1 | 各服务 | 非法邮箱（建号/注册） | HTTP 400，errors 指向 Email | api |
| VAL-02 | P1 | 同上 | 非法手机号（建号/地址/下单/商户） | HTTP 400，errors 指向手机号字段 | api |
| VAL-03 | P1 | 同上 | 弱密码（建号/注册） | HTTP 400 | api |
| VAL-04 | P1 | 同上 | 金额 ≤0 / 超两位小数（支付/退款/商品/购物车） | HTTP 400 或业务失败提示 | api |
| VAL-05 | P1 | 同上 | 数量 0/100（购物车/下单/支付/库存） | HTTP 400 | api |
| VAL-06 | P1 | 各列表接口 | page=0 / pageSize=1000 | HTTP 400 | api |
| VAL-07 | P1 | 权限中心 | 角色名称为空 / 命名长度超限 | HTTP 400 | api |
| VAL-08 | P1 | 商户/平台 | 平台编码格式非法 / 邮箱非法 / 佣金率 >100 | HTTP 400 | api |
| VAL-09 | P1 | 装修配置 | 保存非法 JSON / 空配置 / 超过 100KB | 400 且提示明确 | api |
| VAL-10 | P1 | 装修配置 | 我的服务名称超 8 字 / 图标非路径或超长 | 后台表单校验拦截 | ui |

## 17. 装修配置（DSN）

| ID | 优先级 | 前置 | 步骤 | 期望结果 | 自动化 |
|----|--------|------|------|----------|--------|
| DSN-01 | P1 | 后台 token | `GET /platform-configs/Admin`（已配置/未配置平台） | 200；未配置返回 DefaultDesign（含主题/首页/我的服务） | api |
| DSN-02 | P1 | 后台 token | 保存并发布装修（主题色/公告/首页模块/轮播/我的服务） | 200；`PublishVersion+1` | api |
| DSN-03 | P1 | 游客 | `GET /platform-configs/MiniApp?platformCode=` | 200；返回 design（banner/quickNav/profile.services 可读） | api |
| DSN-04 | P1 | 小程序 | 切换平台 | 主题色/TabBar 选中色/首页/我的服务随平台变化 | ui |
| DSN-05 | P1 | 后台装修页 | 编辑轮播图/金刚区/我的服务并保存 | 配置写入 JSON；校验拦截空图片/空名称 | ui |
| DSN-06 | P2 | 后台装修页 | 发布确认弹窗 | 取消不发布；确认后发布 | ui |
| DSN-07 | P1 | 小程序首页 | 无轮播配置 | 显示主题渐变 Hero（不空白） | ui |

## 18. 管理后台 UI（ADM）

| ID | 优先级 | 前置 | 步骤 | 期望结果 | 自动化 |
|----|--------|------|------|----------|--------|
| ADM-01 | P0 | 未登录 | 打开 `/` | 跳转登录页；登录后进入工作台 | ui |
| ADM-02 | P0 | token 过期 | 触发任意接口 | 提示登录已过期并跳登录页，清 localStorage | ui |
| ADM-03 | P1 | 已登录 | 用户弹窗选择「平台角色」 | 出现「所属平台」字段（tenantType 回填） | ui |
| ADM-04 | P1 | 已登录 | 营销管理页 5 个 Tab | 活动/券模板/券活动/报表/配置均渲染且有数据 | ui |
| ADM-05 | P1 | 已登录 | 活动编辑弹窗 | 归属/活动类型/参与范围正确回填；列表优惠与范围文案正确 | ui |
| ADM-06 | P1 | 已登录 | 订单详情 | 展示活动/券与逐商品折扣、优惠合计 | 手工 |
| ADM-07 | P1 | 已登录 | 小程序装修页 | 轮播图/金刚区/我的服务/权益 可编辑并保存 | ui |
| ADM-08 | P2 | 已登录 | 报表下钻 | 点击活动/券汇总行弹出订单与商品明细 | 手工 |

## 19. 商城小程序 UI（MP）

| ID | 优先级 | 前置 | 步骤 | 期望结果 | 自动化 |
|----|--------|------|------|----------|--------|
| MP-01 | P0 | 已选平台 | 首页 | 铺满头图/悬浮搜索/金刚区/会员问候/优惠专区/分类/商品宫格齐全 | ui |
| MP-02 | P1 | 已登录 | 首页会员卡 | 显示 Hi+问候、等级、券/收藏/订单统计、全部入口 | ui |
| MP-03 | P1 | 已登录 | 我的页 | 会员卡进度/权益行/我的账户/我的服务（5 列线性图标） | ui |
| MP-04 | P1 | 已登录 | 商品详情 | 图集指示点、价格/划线价/库存、活动标签、服务保障、配送、评价占位、吸底（店铺/购物车角标/加购/立购）、悬浮首页 | ui |
| MP-05 | P1 | 已登录 | 店铺页 | 店铺头/商品-活动 Tab/分类 chips/优惠专区/排序/商品网格/悬浮购物车分享 | ui |
| MP-06 | P1 | 有购物车条目 | 购物车 | 商品图显示、+1 增量、优惠条（已优惠+标签）、去结算可用 | ui |
| MP-07 | P1 | 有可用券 | 提交页 | 金额明细（商品金额/优惠合计/实付）、券勾选切换金额浮动、活动展示 | ui |
| MP-08 | P1 | 已登录 | 领券中心/我的券包 | 券卡金额/门槛/有效期展示正确；0元减无重复¥符号 | ui |
| MP-09 | P1 | 已登录 | 订单列表/详情 | 状态药丸、逐商品优惠与优惠合计展示 | 手工 |
| MP-10 | P1 | 已登录 | 收藏页 | 收藏商品展示、取消收藏生效 | ui |
| MP-11 | P2 | 未登录 | 首页会员卡 | 显示登录引导，点击跳登录 | 手工 |

## 20. 系统与基础设施（SYS）

| ID | 优先级 | 前置 | 步骤 | 期望结果 | 自动化 |
|----|--------|------|------|----------|--------|
| SYS-01 | P0 | 服务启动 | 访问各服务 `/health` | 全部 200（12 个 HTTP 服务 + 网关） | api |
| SYS-02 | P0 | 容器运行 | 检查 postgres/redis/consul/rabbitmq/agile_config | 均 Up | 手工 |
| SYS-03 | P1 | 服务启动 | Consul 服务目录 | 10 个业务服务已注册且健康 | api |
| SYS-04 | P1 | 服务停止后重启 | 服务能重新注册 Consul 并恢复路由 | 网关路由 200 | 手工 |
| SYS-05 | P1 | 支付成功事件 | Order/Inventory/Marketing 三个消费者 | 各自队列消费成功；营销赠券、库存扣减、订单已支付 | flow |

## 执行与约定

```bash
# 0) 基础设施与后端（首次或容器停止后）
docker start postgres redis consul rabbitmq agile_config
dotnet build SimpleShop.slnx
bash scripts/run-dev-services.sh     # 日志 logs/runtime/

# 1) 前端 preview（项目路径含 #，必须 build + preview）
cd apps/admin-vue   && npm run build && npx vite preview --host 0.0.0.0 --port 5173 --outDir dist --strictPort
cd apps/user-uniapp && npm run build:h5 && npx vite preview --host 0.0.0.0 --port 5174 --outDir dist/build/h5 --strictPort

# 2) 测试
bash tests/e2e/api-regression.sh     # API 回归（本文件 api 列）
node tests/e2e/ui-regression.js      # UI 回归（本文件 ui 列，需前后端已启动）
bash tests/e2e/business-flow.sh      # 主链路（下单→支付→发货→签收）
bash tests/e2e/marketing-flow.sh     # 营销链路（券/活动/优先级/赠券/报表）
```

**约定**
1. 用例失败即视为回归缺陷：先修复再提交；修复后必须重跑对应脚本。
2. 涉及资金/库存/券的用例（P0）必须验证「副作用」而非只看 HTTP 200（订单金额、库存流水、券状态、记录表）。
3. 新增接口必须同步补：Validator 用例（负数/边界）+ 本文档对应模块条目 + 自动化脚本断言。
4. UI 用例以「元素与文案存在性 + 关键交互结果」为准，样式细节（像素级）以人工核对截图。

## 最近执行结果

| 日期 | 脚本 | 结果 |
|------|------|------|
| 2026-09-17 | `api-regression.sh` | 通过 98 / 失败 0 |
| 2026-09-17 | `ui-regression.js` | 通过 25 / 失败 0 |
| 2026-09-17 | `marketing-flow.sh` | 通过 26 / 失败 0 |
| 2026-09-17 | `business-flow.sh` | 主链路通过 |

> 例行执行：每次改动后至少跑 `api-regression.sh` + `business-flow.sh`；改前端再跑 `ui-regression.js`；改营销再跑 `marketing-flow.sh`。
