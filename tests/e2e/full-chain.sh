#!/usr/bin/env bash
# SimpleShop 业务链路端到端测试（详细严谨版）
#
# 特点：
#   1) 独立测试租户（平台+商户+商品），排除存量活动干扰，所有金额/状态可精确预测；
#   2) 每一步同时断言：HTTP/业务码 → 精确业务值 → 数据库字段（预测数据）；
#   3) 覆盖链路：注册登录 → 地址 → 加购 → 结算预览 → 下单 → 支付 → 库存扣减 → 发货 → 签收
#      → 退款 → 库存回补；取消订单 → 券回退 + 库存释放；满赠 → 券包发券；
#   4) 精确报错矩阵：字段级 400 的 HTTP 码/业务码/errors 字段与中文消息逐条断言。
#
# 用法：bash tests/e2e/full-chain.sh
# 前置：基础设施与全部后端服务已启动（scripts/run-dev-services.sh）。
set -uo pipefail

BASE="${SIMPLESHOP_GATEWAY:-http://127.0.0.1:5008/gateway}"
PSQL() { docker exec -i postgres psql -U postgres -d "$1" -tAc "$2" | tr -d '\r' | xargs; }
TS=$(date +%s)
PASS=0; FAIL=0; FAILED_LABELS=()

ok()  { PASS=$((PASS+1)); printf '  ✓ %s\n' "$1"; }
bad() { FAIL=$((FAIL+1)); FAILED_LABELS+=("$1"); printf '  ✗ %s（期望[%s] 实际[%s]）\n' "$1" "$2" "$3"; }
eq()  { local label="$1" got="$2" want="$3"; [[ "$got" == "$want" ]] && ok "$label" || bad "$label" "$want" "$got"; }

re()    { local m="$1" p="$2" t="${3:-}" b="${4:-}"; local a=(-s -m 15 -w '\n%{http_code}' -X "$m" "$BASE$p"); [[ -n "$t" ]] && a+=(-H "Authorization: Bearer $t"); [[ -n "$b" ]] && a+=(-H 'Content-Type: application/json' -d "$b"); curl "${a[@]}"; }
http_of() { re "$@" | tail -n1; }
body_of() { re "$@" | sed '$d'; }
code_of() { body_of "$@" | jq -r '.code'; }
# 后台账号登录：AuthService OpenIddict 令牌端点（password flow，公开客户端 admin-app）。
BACKEND_LOGIN() {
  curl -s -m 15 -X POST "$BASE/auth/Token" -H 'Content-Type: application/x-www-form-urlencoded' \
    --data-urlencode 'grant_type=password' --data-urlencode "username=$1" \
    --data-urlencode "password=$2" --data-urlencode 'client_id=admin-app' | jq -r '.access_token // empty'
}

echo "== 建立独立测试租户（隔离存量活动，保证预测值精确） =="
ADMIN=$(BACKEND_LOGIN codexadmin Admin123456)
PLATFORM_CODE=$(python3 -c "import random,string;print(''.join(random.choices(string.ascii_uppercase,k=6)))")
PLATFORM=$(body_of POST '/platforms/Create' "$ADMIN" "{\"platformCode\":\"$PLATFORM_CODE\",\"platformName\":\"链路测试平台$TS\",\"contactEmail\":\"chain$TS@test.com\",\"defaultCommissionRate\":3}" | jq -r '.data.id')
MERCHANT=$(body_of POST '/merchants/Create' "$ADMIN" "{\"platformId\":$PLATFORM,\"merchantName\":\"链路商户$TS\",\"contactName\":\"链路\",\"contactPhone\":\"13800000000\",\"contactEmail\":\"m$TS@test.com\",\"commissionRate\":3}" | jq -r '.data.id')
eq "商户编号=平台编码+雪花Id" "$(PSQL simpleshopmerchant "select \"MerchantNo\" from merchant where \"Id\"=$MERCHANT")" "${PLATFORM_CODE}$MERCHANT"
body_of POST '/merchants/Review' "$ADMIN" "{\"id\":$MERCHANT,\"status\":20}" >/dev/null
CATEGORY=$(body_of GET '/products/GetCategoryTree' "$ADMIN" | jq -r '.data[0].id')
PRODUCT=$(body_of POST '/products/CreateProduct' "$ADMIN" "{
  \"platformId\":$PLATFORM,\"merchantId\":$MERCHANT,\"name\":\"链路商品$TS\",\"mainImage\":\"/static/placeholder.png\",
  \"categoryId\":$CATEGORY,\"brandId\":0,\"description\":\"链路测试\",
  \"skus\":[{\"skuCode\":\"CHAIN-$TS-1\",\"price\":100,\"originalPrice\":130,\"stock\":10,\"image\":\"/static/placeholder.png\",\"specName\":\"规格\",\"specValue\":\"标准\"}]}" | jq -r '.data')
body_of POST '/products/PublishProduct' "$ADMIN" "{\"id\":$PRODUCT,\"approved\":true}" >/dev/null
SKU=$(body_of GET "/products/AdminDetail?id=$PRODUCT" "$ADMIN" | jq -r '.data.skus[0].id')
sleep 3   # 等 product.created 事件在库存服务建库存
echo "  平台=$PLATFORM 商户=$MERCHANT 商品=$PRODUCT SKU=$SKU"
eq "初始库存=10（数据库）" "$(PSQL simpleshopinventory "select \"AvailableQuantity\" from stock where \"SkuId\"=$SKU and \"IsDeleted\"=false")" "10"

echo
echo "== 链路 1：注册 → 登录 → 地址 → 加购（API + 数据库双层断言） =="
USER_NAME="chainuser$TS"; PHONE="139${TS: -8}"; EMAIL="chainuser$TS@test.com"
REG=$(body_of POST '/customers/Register' '' "{\"userName\":\"$USER_NAME\",\"password\":\"Chain1234\",\"email\":\"$EMAIL\",\"phone\":\"$PHONE\",\"platformId\":$PLATFORM,\"agreedAgreement\":true,\"registerSource\":1}")
eq "注册返回用户名" "$(jq -r '.data.user.userName' <<<"$REG")" "$USER_NAME"
TOKEN=$(jq -r '.data.token' <<<"$REG")
USER_ID=$(jq -r '.data.user.id' <<<"$REG")
eq "客户落库：customer 表（RegisterSource=1）" "$(PSQL simpleshopcustomer "select case when \"RegisterSource\"=1 then 'customer' else 'unknown' end from customer where \"Id\"=$USER_ID")" "customer"
eq "客户落库：邮箱一致" "$(PSQL simpleshopcustomer "select \"Email\" from customer where \"Id\"=$USER_ID")" "$EMAIL"
eq "密码非明文（长度64，SHA256）" "$(PSQL simpleshopcustomer "select length(pwd) from customer where \"Id\"=$USER_ID")" "64"
eq "密码不是明文" "$(PSQL simpleshopcustomer "select case when pwd='Chain1234' then 'plain' else 'hashed' end from customer where \"Id\"=$USER_ID")" "hashed"
eq "盐值非空" "$([[ -n "$(PSQL simpleshopcustomer "select \"Salt\" from customer where \"Id\"=$USER_ID")" ]] && echo yes)" "yes"
LOGIN=$(body_of POST '/customers/Login' '' "{\"userName\":\"$USER_NAME\",\"password\":\"Chain1234\",\"platformId\":$PLATFORM}")
eq "登录签发 token" "$([[ -n "$(jq -r '.data.token' <<<"$LOGIN")" && "$(jq -r '.data.token' <<<"$LOGIN")" != null ]] && echo yes)" "yes"
ADDR=$(body_of POST '/customers/SaveAddress' "$TOKEN" '{"receiverName":"链路收货人","receiverPhone":"13700000000","province":"广东省","city":"深圳市","district":"南山区","detail":"链路测试路1号","isDefault":true}')
eq "地址保存成功" "$(jq -r '.code' <<<"$ADDR")" "200"
eq "地址落库：收货人" "$(PSQL simpleshopcustomer "select \"ReceiverName\" from customer_address where \"CustomerId\"=$USER_ID order by \"Id\" desc limit 1")" "链路收货人"
eq "地址落库：默认标记" "$(PSQL simpleshopcustomer "select \"IsDefault\" from customer_address where \"CustomerId\"=$USER_ID order by \"Id\" desc limit 1")" "t"
ADD1=$(body_of POST '/carts/Add' "$TOKEN" "{\"productId\":$PRODUCT,\"skuId\":$SKU,\"merchantId\":$MERCHANT,\"platformId\":$PLATFORM,\"productName\":\"链路商品\",\"image\":\"/static/placeholder.png\",\"price\":100,\"quantity\":1}")
eq "加购成功" "$(jq -r '.code' <<<"$ADD1")" "200"
body_of POST '/carts/Add' "$TOKEN" "{\"productId\":$PRODUCT,\"skuId\":$SKU,\"merchantId\":$MERCHANT,\"platformId\":$PLATFORM,\"productName\":\"链路商品\",\"price\":100,\"quantity\":1}" >/dev/null
eq "购物车增量：接口返回2" "$(body_of GET '/carts/Get' "$TOKEN" | jq -r '.data.items[0].quantity')" "2"
eq "购物车落库：数量2" "$(PSQL simpleshopcart "select \"Quantity\" from cart_items where \"UserId\"=$USER_ID and \"SkuId\"=$SKU and \"IsDeleted\"=false")" "2"
eq "购物车落库：图片快照" "$(PSQL simpleshopcart "select \"Image\" from cart_items where \"UserId\"=$USER_ID and \"SkuId\"=$SKU and \"IsDeleted\"=false")" "/static/placeholder.png"

echo
echo "== 链路 2：结算预览（无优惠 → 满减活动 → 券优先/活动优先 → 0元减） =="
PREVIEW() { body_of POST '/marketing/SettlePreview' "$TOKEN" "{\"platformId\":$PLATFORM,\"selectedUserCouponIds\":$1,\"items\":[{\"skuId\":$SKU,\"platformId\":$PLATFORM,\"merchantId\":$MERCHANT,\"productName\":\"链路商品\",\"price\":100,\"quantity\":1}]}"; }
RES=$(PREVIEW 'null')
eq "无活动时实付=100" "$(jq -r '.data.items[0].payAmount' <<<"$RES")" "100"
ACT=$(body_of POST '/marketing/SaveActivity' "$ADMIN" "{\"platformId\":$PLATFORM,\"name\":\"链路满100减10\",\"activityType\":1,\"threshold\":100,\"discountValue\":10,\"scopeType\":1,\"startAt\":\"2026-01-01T00:00:00\",\"isEnabled\":true}" | jq -r '.data.id')
RES=$(PREVIEW 'null')
eq "满100减10命中（活动折扣）" "$(jq -r '.data.activityDiscount' <<<"$RES")" "10.00"
eq "活动后实付=90" "$(jq -r '.data.items[0].payAmount' <<<"$RES")" "90.00"
TPL=$(body_of POST '/marketing/SaveCouponTemplate' "$ADMIN" "{\"platformId\":$PLATFORM,\"name\":\"链路0元减5\",\"couponType\":3,\"threshold\":0,\"discountValue\":5,\"validDays\":30}" | jq -r '.data.id')
CACT=$(body_of POST '/marketing/SaveCouponActivity' "$ADMIN" "{\"platformId\":$PLATFORM,\"name\":\"链路领券\",\"couponTemplateId\":$TPL,\"scopeType\":1,\"totalStock\":100,\"perUserLimit\":2,\"isClaimable\":true}" | jq -r '.data.id')
CLAIM=$(body_of POST '/marketing/ClaimCoupon' "$TOKEN" "{\"couponActivityId\":$CACT}")
USER_COUPON=$(jq -r '.data.userCouponId' <<<"$CLAIM")
eq "领券成功" "$(jq -r '.code' <<<"$CLAIM")" "200"
eq "券落库：未使用(1)/来源领取(1)" "$(PSQL simpleshopmarketing "select \"Status\"||'/'||\"Source\" from user_coupon where \"Id\"=$USER_COUPON")" "1/1"
body_of POST '/marketing/SaveConfig' "$ADMIN" "{\"platformId\":$PLATFORM,\"discountPriority\":2}" >/dev/null
RES=$(PREVIEW "[$USER_COUPON]")
eq "券优先：命中券5元" "$(jq -r '.data.couponDiscount' <<<"$RES")" "5.00"
eq "券优先：不叠加活动" "$(jq -r '.data.activityDiscount' <<<"$RES")" "0"
body_of POST '/marketing/SaveConfig' "$ADMIN" "{\"platformId\":$PLATFORM,\"discountPriority\":1}" >/dev/null
RES=$(PREVIEW "[$USER_COUPON]")
eq "活动优先：命中活动10元" "$(jq -r '.data.activityDiscount' <<<"$RES")" "10.00"
eq "活动优先：不叠加券" "$(jq -r '.data.couponDiscount' <<<"$RES")" "0"
body_of POST '/marketing/SaveConfig' "$ADMIN" "{\"platformId\":$PLATFORM,\"discountPriority\":2}" >/dev/null
RES=$(PREVIEW "[$USER_COUPON]")
eq "0元减不越价（100元行命中5元）" "$(jq -r '.data.items[0].payAmount' <<<"$RES")" "95.00"
FP=$(body_of POST '/marketing/FinalPrice' "$TOKEN" "{\"platformId\":$PLATFORM,\"items\":[{\"skuId\":$SKU,\"platformId\":$PLATFORM,\"merchantId\":$MERCHANT,\"productName\":\"链路商品\",\"price\":100,\"quantity\":1}]}")
eq "到手价=活动/券更低价90（京东口径）" "$(jq -r '.data.items[0].finalPrice' <<<"$FP")" "90.00"
eq "到手价拆分：活动减10" "$(jq -r '.data.items[0].activityDiscount' <<<"$FP")" "10.00"
eq "到手价拆分：券减0（券5元劣于活动）" "$(jq -r '.data.items[0].couponDiscount' <<<"$FP")" "0"
eq "到手价命中活动名称" "$(jq -r '.data.items[0].activityName' <<<"$FP")" "链路满100减10"
FP_GUEST=$(body_of POST '/marketing/FinalPrice' '' "{\"platformId\":$PLATFORM,\"items\":[{\"skuId\":$SKU,\"platformId\":$PLATFORM,\"merchantId\":$MERCHANT,\"productName\":\"链路商品\",\"price\":100,\"quantity\":1}]}")
eq "游客到手价=活动价90（不含券）" "$(jq -r '.data.items[0].finalPrice' <<<"$FP_GUEST")" "90.00"
FPBODY="{\"platformId\":$PLATFORM,\"items\":[{\"skuId\":$SKU,\"platformId\":$PLATFORM,\"merchantId\":$MERCHANT,\"productName\":\"链路商品\",\"price\":100,\"quantity\":1}]}"
body_of POST '/marketing/SetActivityEnabled' "$ADMIN" "{\"id\":$ACT,\"isEnabled\":false}" >/dev/null
eq "停用活动落库（字段级更新生效）" "$(PSQL simpleshopmarketing "select \"IsEnabled\" from marketing_activity where \"Id\"=$ACT")" "f"
RES_OFF=$(body_of POST '/marketing/FinalPrice' "$TOKEN" "$FPBODY")
eq "停用后到手价回退为券后95（快照缓存失效）" "$(jq -r '.data.items[0].finalPrice' <<<"$RES_OFF")" "95.00"
body_of POST '/marketing/SetActivityEnabled' "$ADMIN" "{\"id\":$ACT,\"isEnabled\":true}" >/dev/null
eq "启用活动落库（字段级更新生效）" "$(PSQL simpleshopmarketing "select \"IsEnabled\" from marketing_activity where \"Id\"=$ACT")" "t"
RES_ON=$(body_of POST '/marketing/FinalPrice' "$TOKEN" "$FPBODY")
eq "重新启用后到手价恢复90（快照缓存失效）" "$(jq -r '.data.items[0].finalPrice' <<<"$RES_ON")" "90.00"
eq "到手价报错·空清单（业务码400）" "$(code_of POST '/marketing/FinalPrice' "$TOKEN" "{\"platformId\":$PLATFORM,\"items\":[]}")" "400"
eq "到手价报错·价格非正（HTTP 400）" "$(http_of POST '/marketing/FinalPrice' "$TOKEN" "{\"platformId\":$PLATFORM,\"items\":[{\"skuId\":$SKU,\"platformId\":$PLATFORM,\"merchantId\":$MERCHANT,\"price\":0,\"quantity\":1}]}")" "400"

echo
echo "== 链路 3：下单（带券）→ 支付 → 库存扣减 → 发货 → 签收（全链路数据一致性） =="
CREATE_ORDER() { # $1=key $2=selectedCoupons $3=customerId
  body_of POST '/orders/Create' "$TOKEN" "{
    \"idempotencyKey\":\"$1\",\"platformId\":$PLATFORM,\"customerNo\":\"U$3\",\"customerName\":\"$USER_NAME\",
    \"receiverName\":\"链路收货人\",\"receiverPhone\":\"13700000000\",\"receiverAddress\":\"广东省深圳市南山区链路测试路1号\",
    \"selectedUserCouponIds\":$2,
    \"items\":[{\"skuId\":$SKU,\"platformId\":$PLATFORM,\"merchantId\":$MERCHANT,\"productName\":\"链路商品\",\"price\":100,\"quantity\":1}],
    \"stockItems\":[{\"skuId\":$SKU,\"quantity\":1}]}"
}
ORDER=$(CREATE_ORDER "chain-$TS-1" "[$USER_COUPON]" "$USER_ID")
ORDER_NO=$(jq -r '.data.orderNo' <<<"$ORDER")
ORDER_ID=$(jq -r '.data.id' <<<"$ORDER")
eq "下单成功" "$(jq -r '.data.success' <<<"$ORDER")" "true"
eq "预测实付：100-5=95" "$(jq -r '.data.paymentPrice' <<<"$ORDER")" "95.00"
eq "订单落库：总价/总优惠/实付/券优惠" "$(PSQL simpleshoporder "select \"TotalPrice\"||'/'||\"AllDiscountPrice\"||'/'||\"PaymentPrice\"||'/'||\"CouponDiscountPrice\" from \"order\" where \"Id\"=$ORDER_ID")" "100.00/5.00/95.00/5.00"
eq "订单落库：状态10 未支付" "$(PSQL simpleshoporder "select \"OrderStatus\"||'/'||\"IsPayment\" from \"order\" where \"Id\"=$ORDER_ID")" "10/false"
eq "明细落库：行折扣5/券快照" "$(PSQL simpleshoporder "select \"DiscountAmount\"||'/'||\"MarketingType\"||'/'||\"UserCouponId\" from order_item where \"OrderId\"=$ORDER_ID")" "5.00/2/$USER_COUPON"
eq "券落库：已占用(2)+订单号" "$(PSQL simpleshopmarketing "select \"Status\"||'/'||\"UsedOrderNo\" from user_coupon where \"Id\"=$USER_COUPON")" "2/$ORDER_NO"
eq "库存落库：总10/锁定1/已扣0" "$(PSQL simpleshopinventory "select \"AvailableQuantity\"||'/'||\"LockedQuantity\"||'/'||\"DeductedQuantity\" from stock where \"SkuId\"=$SKU")" "10/1/0"
eq "可用库存=9（Available-Locked-Deducted）" "$(PSQL simpleshopinventory "select \"AvailableQuantity\"-\"LockedQuantity\"-\"DeductedQuantity\" from stock where \"SkuId\"=$SKU")" "9"
eq "库存流水：lock 1 条" "$(PSQL simpleshopinventory "select count(*) from stock_flow where \"BizNo\"='$ORDER_NO' and \"Action\"='lock'")" "1"
PAY=$(body_of POST '/payments/Create' "$TOKEN" "{\"bizNo\":\"$ORDER_NO\",\"platformId\":$PLATFORM,\"merchantId\":$MERCHANT,\"userId\":$USER_ID,\"amount\":95,\"items\":[{\"skuId\":$SKU,\"quantity\":1}]}")
eq "支付单落库：金额95/状态10" "$(PSQL simpleshoppayment "select \"Amount\"||'/'||\"Status\" from payment_order where \"BizNo\"='$ORDER_NO'")" "95.00/10"
body_of POST '/payments/Confirm' "$TOKEN" "{\"bizNo\":\"$ORDER_NO\",\"items\":[{\"skuId\":$SKU,\"quantity\":1}]}" >/dev/null
sleep 3   # 等 MQ：订单已支付 + 库存扣减 + 满赠发券
eq "支付单已支付(20)" "$(PSQL simpleshoppayment "select \"Status\" from payment_order where \"BizNo\"='$ORDER_NO'")" "20"
eq "订单已支付：状态20/IsPayment" "$(PSQL simpleshoporder "select \"OrderStatus\"||'/'||\"IsPayment\" from \"order\" where \"Id\"=$ORDER_ID")" "20/true"
eq "库存已扣减：总10/锁定0/已扣1（可用9）" "$(PSQL simpleshopinventory "select \"AvailableQuantity\"||'/'||\"LockedQuantity\"||'/'||\"DeductedQuantity\"||'/'||(\"AvailableQuantity\"-\"LockedQuantity\"-\"DeductedQuantity\") from stock where \"SkuId\"=$SKU")" "10/0/1/9"
eq "库存流水：deduct 1 条" "$(PSQL simpleshopinventory "select count(*) from stock_flow where \"BizNo\"='$ORDER_NO' and \"Action\"='deduct'")" "1"
eq "用券记录：抵扣5" "$(PSQL simpleshopmarketing "select \"DiscountAmount\" from coupon_record where \"OrderNo\"='$ORDER_NO'")" "5.00"
SHIP=$(body_of POST '/orders/Shipment' "$ADMIN" "{\"orderId\":$ORDER_ID,\"platformId\":$PLATFORM,\"merchantId\":$MERCHANT,\"logisticsCompany\":\"顺丰速运\",\"trackingNo\":\"SF$TS\",\"items\":[{\"orderItemId\":$(PSQL simpleshoporder "select \"Id\" from order_item where \"OrderId\"=$ORDER_ID"),\"skuId\":$SKU,\"quantity\":1}]}")
eq "发货成功" "$(jq -r '.code' <<<"$SHIP")" "200"
eq "发货单落库：物流公司/单号" "$(PSQL simpleshoporder "select \"LogisticsCompany\"||'/'||\"TrackingNo\" from shipment where \"OrderId\"=$ORDER_ID")" "顺丰速运/SF$TS"
eq "发货明细落库：数量1" "$(PSQL simpleshoporder "select \"Quantity\" from shipment_item where \"ShipmentId\"=$(PSQL simpleshoporder "select \"Id\" from shipment where \"OrderId\"=$ORDER_ID")")" "1"
eq "订单已发货：状态40" "$(PSQL simpleshoporder "select \"OrderStatus\" from \"order\" where \"Id\"=$ORDER_ID")" "40"
SHIP_ID=$(PSQL simpleshoporder "select \"Id\" from shipment where \"OrderId\"=$ORDER_ID")
body_of POST '/orders/Receive' "$TOKEN" "{\"shipmentId\":$SHIP_ID}" >/dev/null
eq "订单已完成：状态50" "$(PSQL simpleshoporder "select \"OrderStatus\" from \"order\" where \"Id\"=$ORDER_ID")" "50"
eq "发货单已签收" "$(PSQL simpleshoporder "select case when \"ReceivedAt\" is not null then 'received' else 'pending' end from shipment where \"Id\"=$SHIP_ID")" "received"

echo
echo "== 链路 4：退款（申请→审批→订单60 + 库存回补 + 报表） =="
REF=$(body_of POST '/payments/Refund' "$TOKEN" "{\"bizNo\":\"$ORDER_NO\",\"amount\":95,\"reason\":\"链路全额退款\",\"items\":[{\"skuId\":$SKU,\"quantity\":1}]}")
eq "退款申请成功" "$(jq -r '.code' <<<"$REF")" "200"
eq "退款单落库：金额95/状态10" "$(PSQL simpleshoppayment "select \"Amount\"||'/'||\"Status\" from refund_order where \"BizNo\"='$ORDER_NO'")" "95.00/10"
REF_ID=$(PSQL simpleshoppayment "select \"Id\" from refund_order where \"BizNo\"='$ORDER_NO'")
body_of POST '/payments/ApproveRefund' "$ADMIN" "{\"id\":$REF_ID}" >/dev/null
sleep 3
eq "退款单已退款(20)" "$(PSQL simpleshoppayment "select \"Status\" from refund_order where \"Id\"=$REF_ID")" "20"
eq "订单已退款：状态60/IsAllRefund" "$(PSQL simpleshoporder "select \"OrderStatus\"||'/'||\"IsAllRefund\" from \"order\" where \"Id\"=$ORDER_ID")" "60/true"
eq "库存回补：总10/已扣0（可用10）" "$(PSQL simpleshopinventory "select \"AvailableQuantity\"||'/'||\"DeductedQuantity\"||'/'||(\"AvailableQuantity\"-\"LockedQuantity\"-\"DeductedQuantity\") from stock where \"SkuId\"=$SKU")" "10/0/10"
REFUND_NO=$(PSQL simpleshoppayment "select \"RefundNo\" from refund_order where \"Id\"=$REF_ID")
RESTORED=0
for i in 1 2 3 4 5 6 7 8; do
  [[ "$(PSQL simpleshopinventory "select count(*) from stock_flow where \"BizNo\"='$REFUND_NO' and \"Action\"='restore'")" == "1" ]] && { RESTORED=1; break; }
  sleep 2
done
eq "库存流水：restore 1 条（退款号关联）" "$RESTORED" "1"
eq "券优先订单无活动参与记录" "$(PSQL simpleshopmarketing "select count(*) from marketing_activity_record where \"OrderNo\"='$ORDER_NO'")" "0"
eq "券报表：抵扣5" "$(PSQL simpleshopmarketing "select \"DiscountAmount\" from coupon_record where \"OrderNo\"='$ORDER_NO'")" "5.00"

echo
echo "== 链路 4b：纯活动订单（无券）→ 活动参与记录 + 报表金额 =="
USER3_NAME="chainact$TS"
REG3=$(body_of POST '/customers/Register' '' "{\"userName\":\"$USER3_NAME\",\"password\":\"Chain1234\",\"email\":\"act$TS@test.com\",\"phone\":\"135${TS: -8}\",\"platformId\":$PLATFORM,\"agreedAgreement\":true}")
TOKEN3=$(jq -r '.data.token' <<<"$REG3")
USER3_ID=$(jq -r '.data.user.id' <<<"$REG3")
FP_ACT=$(body_of POST '/marketing/FinalPrice' "$TOKEN3" "{\"platformId\":$PLATFORM,\"items\":[{\"skuId\":$SKU,\"platformId\":$PLATFORM,\"merchantId\":$MERCHANT,\"productName\":\"链路商品\",\"price\":100,\"quantity\":1}]}")
eq "无券用户到手价=活动价90" "$(jq -r '.data.items[0].finalPrice' <<<"$FP_ACT")" "90.00"
ORDER_ACT=$(body_of POST '/orders/Create' "$TOKEN3" "{\"idempotencyKey\":\"chain-$TS-act\",\"platformId\":$PLATFORM,\"customerNo\":\"U$USER3_ID\",\"customerName\":\"$USER3_NAME\",\"receiverName\":\"活动收货人\",\"receiverPhone\":\"13700000000\",\"receiverAddress\":\"链路地址\",\"selectedUserCouponIds\":[],\"items\":[{\"skuId\":$SKU,\"platformId\":$PLATFORM,\"merchantId\":$MERCHANT,\"productName\":\"链路商品\",\"price\":100,\"quantity\":1}],\"stockItems\":[{\"skuId\":$SKU,\"quantity\":1}]}")
ORDER_ACT_NO=$(jq -r '.data.orderNo' <<<"$ORDER_ACT")
eq "纯活动订单实付90（满100减10）" "$(jq -r '.data.paymentPrice' <<<"$ORDER_ACT")" "90.00"
eq "到手价=下单实付（展示与结算一致）" "$(jq -r '.data.items[0].finalPrice' <<<"$FP_ACT")" "$(jq -r '.data.paymentPrice' <<<"$ORDER_ACT")"
eq "活动参与记录：折扣10/类型满减" "$(PSQL simpleshopmarketing "select \"DiscountAmount\"||'/'||\"ActivityType\" from marketing_activity_record where \"OrderNo\"='$ORDER_ACT_NO'")" "10.00/1"
eq "活动记录商品行：行折扣10" "$(PSQL simpleshopmarketing "select \"DiscountAmount\" from marketing_activity_record_item where \"OrderNo\"='$ORDER_ACT_NO'")" "10.00"
ORDER_ACT_ID=$(jq -r '.data.id' <<<"$ORDER_ACT")
body_of POST '/payments/Create' "$TOKEN3" "{\"bizNo\":\"$ORDER_ACT_NO\",\"platformId\":$PLATFORM,\"merchantId\":$MERCHANT,\"userId\":$USER3_ID,\"amount\":90,\"items\":[{\"skuId\":$SKU,\"quantity\":1}]}" >/dev/null
body_of POST '/payments/Confirm' "$TOKEN3" "{\"bizNo\":\"$ORDER_ACT_NO\",\"items\":[{\"skuId\":$SKU,\"quantity\":1}]}" >/dev/null
sleep 3
eq "订单明细活动快照：类型1/活动ID" "$(PSQL simpleshoporder "select \"MarketingType\"||'/'||\"MarketingId\" from order_item where \"OrderId\"=$ORDER_ACT_ID")" "1/$ACT"

echo
echo "== 链路 5：取消订单 → 券回退 + 库存释放 =="
CLAIM2=$(body_of POST '/marketing/ClaimCoupon' "$TOKEN" "{\"couponActivityId\":$CACT}")
UC2=$(jq -r '.data.userCouponId' <<<"$CLAIM2")
ORDER2=$(CREATE_ORDER "chain-$TS-2" "[$UC2]" "$USER_ID")
ORDER2_ID=$(jq -r '.data.id' <<<"$ORDER2")
eq "第二单实付95" "$(jq -r '.data.paymentPrice' <<<"$ORDER2")" "95.00"
eq "取消前：券占用(2)" "$(PSQL simpleshopmarketing "select \"Status\" from user_coupon where \"Id\"=$UC2")" "2"
eq "取消前：锁定1（可用8=10-已扣1-锁定1）" "$(PSQL simpleshopinventory "select \"LockedQuantity\"||'/'||(\"AvailableQuantity\"-\"LockedQuantity\"-\"DeductedQuantity\") from stock where \"SkuId\"=$SKU")" "1/8"
body_of POST '/orders/Cancel' "$TOKEN" "{\"id\":$ORDER2_ID,\"reason\":\"链路取消\"}" >/dev/null
sleep 3
eq "订单已取消：状态90" "$(PSQL simpleshoporder "select \"OrderStatus\" from \"order\" where \"Id\"=$ORDER2_ID")" "90"
eq "取消原因落库" "$(PSQL simpleshoporder "select \"CancelReason\" from \"order\" where \"Id\"=$ORDER2_ID")" "链路取消"
eq "取消后：券回退未使用(1)" "$(PSQL simpleshopmarketing "select \"Status\" from user_coupon where \"Id\"=$UC2")" "1"
eq "取消后：锁定0（可用9=仅已扣1）" "$(PSQL simpleshopinventory "select \"LockedQuantity\"||'/'||(\"AvailableQuantity\"-\"LockedQuantity\"-\"DeductedQuantity\") from stock where \"SkuId\"=$SKU")" "0/9"
eq "取消后：release 流水 1 条" "$(PSQL simpleshopinventory "select count(*) from stock_flow where \"BizNo\"='$(jq -r '.data.orderNo' <<<"$ORDER2")' and \"Action\"='release'")" "1"

echo
echo "== 链路 6：满赠（无券订单支付成功 → 赠券入券包 + 活动记录） =="
GIFT_TPL=$(body_of POST '/marketing/SaveCouponTemplate' "$ADMIN" "{\"platformId\":$PLATFORM,\"name\":\"链路赠品券\",\"couponType\":1,\"threshold\":50,\"discountValue\":8,\"validDays\":30}" | jq -r '.data.id')
GIFT_CACT=$(body_of POST '/marketing/SaveCouponActivity' "$ADMIN" "{\"platformId\":$PLATFORM,\"name\":\"链路赠品券活动\",\"couponTemplateId\":$GIFT_TPL,\"scopeType\":1,\"totalStock\":50,\"perUserLimit\":10,\"isClaimable\":false}" | jq -r '.data.id')
GIFT_ACT=$(body_of POST '/marketing/SaveActivity' "$ADMIN" "{\"platformId\":$PLATFORM,\"name\":\"链路满1赠券\",\"activityType\":3,\"threshold\":1,\"discountValue\":0,\"giftCouponActivityId\":$GIFT_CACT,\"scopeType\":1,\"startAt\":\"2026-01-01T00:00:00\",\"isEnabled\":true}" | jq -r '.data.id')
# 用全新用户（无券）下单，显式不使用券 → 仅有赠券活动可命中
USER2_NAME="chaingift$TS"
REG2=$(body_of POST '/customers/Register' '' "{\"userName\":\"$USER2_NAME\",\"password\":\"Chain1234\",\"email\":\"gift$TS@test.com\",\"phone\":\"136${TS: -8}\",\"platformId\":$PLATFORM,\"agreedAgreement\":true}")
TOKEN2=$(jq -r '.data.token' <<<"$REG2")
USER2_ID=$(jq -r '.data.user.id' <<<"$REG2")
ORDER3=$(body_of POST '/orders/Create' "$TOKEN2" "{\"idempotencyKey\":\"chain-$TS-3\",\"platformId\":$PLATFORM,\"customerNo\":\"U$USER2_ID\",\"customerName\":\"$USER2_NAME\",\"receiverName\":\"赠券收货人\",\"receiverPhone\":\"13700000000\",\"receiverAddress\":\"链路地址\",\"selectedUserCouponIds\":[],\"items\":[{\"skuId\":$SKU,\"platformId\":$PLATFORM,\"merchantId\":$MERCHANT,\"productName\":\"链路商品\",\"price\":30,\"quantity\":1}],\"stockItems\":[{\"skuId\":$SKU,\"quantity\":1}]}")
ORDER3_NO=$(jq -r '.data.orderNo' <<<"$ORDER3")
eq "满赠订单折扣为0（赠券不折现）" "$(jq -r '.data.paymentPrice' <<<"$ORDER3")" "30"
body_of POST '/payments/Create' "$TOKEN2" "{\"bizNo\":\"$ORDER3_NO\",\"platformId\":$PLATFORM,\"merchantId\":$MERCHANT,\"userId\":$USER2_ID,\"amount\":30,\"items\":[{\"skuId\":$SKU,\"quantity\":1}]}" >/dev/null
body_of POST '/payments/Confirm' "$TOKEN2" "{\"bizNo\":\"$ORDER3_NO\",\"items\":[{\"skuId\":$SKU,\"quantity\":1}]}" >/dev/null
sleep 4
eq "赠券落库：来源满赠(2)" "$(PSQL simpleshopmarketing "select count(*) from user_coupon where \"UserId\"=$USER2_ID and \"Source\"=2")" "1"
eq "赠券张数=模板门槛券" "$(PSQL simpleshopmarketing "select \"Threshold\" from coupon_template where \"Id\"=$GIFT_TPL")" "50.00"
eq "活动记录：GiftCouponCount=1" "$(PSQL simpleshopmarketing "select \"GiftCouponCount\" from marketing_activity_record where \"OrderNo\"='$ORDER3_NO' and \"ActivityId\"=$GIFT_ACT")" "1"
eq "赠券活动发行量+1" "$(PSQL simpleshopmarketing "select \"IssuedCount\" from coupon_activity where \"Id\"=$GIFT_CACT")" "1"

echo
echo "== 精确报错矩阵（HTTP 码 / 业务码 / errors 字段 / 中文消息 全部预测） =="
# expect_validation <标签> <方法> <路径> <token> <body> <字段> <精确消息>
expect_validation() {
  local label="$1" method="$2" path="$3" token="$4" payload="$5" field="$6" message="$7"
  local resp code msg http
  http=$(http_of "$method" "$path" "$token" "$payload"); resp=$(body_of "$method" "$path" "$token" "$payload")
  code=$(jq -r '.code' <<<"$resp" 2>/dev/null)
  msg=$(jq -r ".errors[\"$field\"][0] // empty" <<<"$resp" 2>/dev/null)
  if [[ "$http" == "400" && "$code" == "400" && "$msg" == "$message" ]]; then ok "$label"; else bad "$label" "HTTP400/code400/$field=$message" "HTTP$http/code$code/$field=$msg"; fi
}
# expect_business <标签> <路径> <token> <body> <精确消息>
expect_business() {
  local label="$1" path="$2" token="$3" payload="$4" message="$5"
  local resp http code msg
  http=$(http_of POST "$path" "$token" "$payload"); resp=$(body_of POST "$path" "$token" "$payload")
  code=$(jq -r '.code' <<<"$resp" 2>/dev/null); msg=$(jq -r '.message' <<<"$resp" 2>/dev/null)
  if [[ "$code" == "400" && "$msg" == "$message" ]]; then ok "$label"; else bad "$label" "code400/$message" "HTTP$http/code$code/$msg"; fi
}

expect_validation "报错·注册邮箱格式" POST '/customers/Register' '' "{\"userName\":\"v$TS\",\"password\":\"Test1234\",\"email\":\"bad-email\",\"phone\":\"13800000000\"}" "Email" "邮箱格式不正确"
expect_validation "报错·注册手机号格式" POST '/customers/Register' '' "{\"userName\":\"v$TS\",\"password\":\"Test1234\",\"email\":\"v$TS@test.com\",\"phone\":\"123\"}" "Phone" "手机号格式不正确"
expect_validation "报错·注册密码强度" POST '/customers/Register' '' "{\"userName\":\"v$TS\",\"password\":\"12345678\",\"email\":\"v$TS@test.com\",\"phone\":\"13800000000\"}" "Password" "密码至少8位且包含字母和数字"
expect_validation "报错·注册用户名长度" POST '/customers/Register' '' "{\"userName\":\"ab\",\"password\":\"Test1234\",\"email\":\"v$TS@test.com\",\"phone\":\"13800000000\"}" "UserName" "用户名必须为3-64个字符"
expect_validation "报错·注册未勾选协议" POST '/customers/Register' '' "{\"userName\":\"v$TS\",\"password\":\"Test1234\",\"email\":\"v$TS@test.com\",\"phone\":\"13800000000\",\"platformId\":$PLATFORM,\"agreedAgreement\":false}" "AgreedAgreement" "请阅读并同意用户协议"
expect_validation "报错·注册来源非法" POST '/customers/Register' '' "{\"userName\":\"v$TS\",\"password\":\"Test1234\",\"email\":\"v$TS@test.com\",\"phone\":\"13800000000\",\"platformId\":$PLATFORM,\"agreedAgreement\":true,\"registerSource\":99}" "RegisterSource" "注册来源不正确"
expect_validation "报错·后台建号邮箱格式" POST '/users/Create' "$ADMIN" "{\"userName\":\"vchk$TS\",\"password\":\"Test1234\",\"email\":\"bad\",\"phone\":\"13800000000\",\"role\":\"customer\"}" "Email" "邮箱格式不正确"
expect_validation "报错·地址手机号格式" POST '/customers/SaveAddress' "$TOKEN" '{"receiverName":"x","receiverPhone":"123","province":"广东省","city":"深圳市","district":"南山区","detail":"x","isDefault":false}' "ReceiverPhone" "手机号格式不正确"
expect_validation "报错·地址详细地址必填" POST '/customers/SaveAddress' "$TOKEN" '{"receiverName":"x","receiverPhone":"13800000000","province":"广东省","city":"深圳市","district":"南山区","detail":"","isDefault":false}' "Detail" "请填写详细地址"
expect_validation "报错·下单手机号格式" POST '/orders/Create' "$TOKEN" "{\"idempotencyKey\":\"v-$TS\",\"platformId\":$PLATFORM,\"customerNo\":\"U$USER_ID\",\"customerName\":\"x\",\"receiverName\":\"x\",\"receiverPhone\":\"123\",\"receiverAddress\":\"x\",\"items\":[{\"skuId\":$SKU,\"productName\":\"x\",\"price\":1,\"quantity\":1}]}" "ReceiverPhone" "手机号格式不正确"
expect_validation "报错·购物车数量范围" POST '/carts/Add' "$TOKEN" "{\"productId\":$PRODUCT,\"skuId\":$SKU,\"merchantId\":$MERCHANT,\"platformId\":$PLATFORM,\"productName\":\"x\",\"price\":1,\"quantity\":0}" "Quantity" "数量必须为1-99"
expect_validation "报错·购物车价格为正" POST '/carts/Add' "$TOKEN" "{\"productId\":$PRODUCT,\"skuId\":$SKU,\"merchantId\":$MERCHANT,\"platformId\":$PLATFORM,\"productName\":\"x\",\"price\":-1,\"quantity\":1}" "Price" "商品价格必须大于0"
expect_validation "报错·支付金额为正" POST '/payments/Create' "$TOKEN" '{"bizNo":"none","platformId":1,"merchantId":1,"userId":1,"amount":0,"items":[{"skuId":1,"quantity":1}]}' "Amount" "支付金额必须大于0"
expect_validation "报错·退款原因必填" POST '/payments/Refund' "$TOKEN" '{"bizNo":"none","amount":1,"items":[{"skuId":1,"quantity":1}]}' "Reason" "退款原因不能为空"
expect_validation "报错·活动名称必填" POST '/marketing/SaveActivity' "$ADMIN" "{\"platformId\":$PLATFORM,\"name\":\"\",\"activityType\":1,\"threshold\":1,\"discountValue\":1,\"scopeType\":1,\"startAt\":\"2026-01-01T00:00:00\"}" "Name" "活动名称不能为空"
expect_validation "报错·满折折扣率范围" POST '/marketing/SaveActivity' "$ADMIN" "{\"platformId\":$PLATFORM,\"name\":\"v$TS\",\"activityType\":2,\"threshold\":1,\"discountValue\":1.5,\"scopeType\":1,\"startAt\":\"2026-01-01T00:00:00\"}" "DiscountValue" "折扣率必须在0.01-0.99之间（0.85=8.5折）"
expect_validation "报错·券名必填" POST '/marketing/SaveCouponTemplate' "$ADMIN" "{\"platformId\":$PLATFORM,\"name\":\"\",\"couponType\":1,\"threshold\":1,\"discountValue\":1,\"validDays\":7}" "Name" "券模板名称不能为空"
expect_business "报错·重复领券超限" "/marketing/ClaimCoupon" "$TOKEN" "{\"couponActivityId\":$CACT}" "已达到每人限领数量"
eq "报错·无权限访问营销后台（HTTP 403）" "$(http_of GET '/marketing/ActivityList?page=1&pageSize=5' "$TOKEN")" "403"

echo
echo "================ 链路测试结果 ================"
printf '通过 %d，失败 %d\n' "$PASS" "$FAIL"
if [[ $FAIL -gt 0 ]]; then printf '失败用例：\n'; printf '  - %s\n' "${FAILED_LABELS[@]}"; exit 1; fi
