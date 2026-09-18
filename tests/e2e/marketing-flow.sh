#!/usr/bin/env bash
# 营销模块端到端验证：券模板/券活动/活动创建 → 领券 → 计算优先级 → 下单结算 → 记录与报表 → 满赠发券。
set -uo pipefail
BASE="http://127.0.0.1:5008/gateway"
TS=$(date +%s)
PASS=0; FAIL=0

check() { local label="$1" got="$2" want="$3"; if [[ "$got" == "$want" ]]; then PASS=$((PASS+1)); echo "  ✓ $label"; else FAIL=$((FAIL+1)); echo "  ✗ $label 期望[$want] 实际[$got]"; fi; }

admin_login=$(curl -s -X POST "$BASE/auth/Token" -H 'Content-Type: application/x-www-form-urlencoded' \
  --data-urlencode 'grant_type=password' --data-urlencode 'username=codexadmin' \
  --data-urlencode 'password=Admin123456' --data-urlencode 'client_id=admin-app')
ADMIN=$(jq -r '.access_token' <<<"$admin_login")
PLATFORM_ID=$(curl -s "$BASE/platforms/List?page=1&pageSize=10" -H "Authorization: Bearer $ADMIN" | jq -r '.data.items[0].id')
MERCHANT_ID=$(curl -s "$BASE/merchants/List?page=1&pageSize=10" -H "Authorization: Bearer $ADMIN" | jq -r '.data.items[0].id')
SKU_BASE=$((TS % 900000 + 100000))
echo "平台 $PLATFORM_ID / 商户 $MERCHANT_ID"

post_admin() { curl -s -X POST "$BASE/$1" -H "Authorization: Bearer $ADMIN" -H 'Content-Type: application/json' -d "$2"; }

echo "== 1. 创建券模板与券活动（平台）"
tpl=$(post_admin "marketing/SaveCouponTemplate" "{\"platformId\":$PLATFORM_ID,\"name\":\"满50减5券\",\"couponType\":1,\"threshold\":50,\"discountValue\":5,\"validDays\":7}")
check "券模板创建" "$(jq -r '.code' <<<"$tpl")" 200
TEMPLATE_ID=$(jq -r '.data.id' <<<"$tpl")
tpl2=$(post_admin "marketing/SaveCouponTemplate" "{\"platformId\":$PLATFORM_ID,\"name\":\"满赠券10元\",\"couponType\":3,\"threshold\":0,\"discountValue\":10,\"validDays\":30}")
TEMPLATE2_ID=$(jq -r '.data.id' <<<"$tpl2")

coupon_act=$(post_admin "marketing/SaveCouponActivity" "{\"platformId\":$PLATFORM_ID,\"name\":\"新人满减券\",\"couponTemplateId\":$TEMPLATE_ID,\"scopeType\":1,\"totalStock\":100,\"perUserLimit\":1,\"isClaimable\":true}")
check "券活动创建" "$(jq -r '.code' <<<"$coupon_act")" 200
COUPON_ACT_ID=$(jq -r '.data.id' <<<"$coupon_act")

gift_act=$(post_admin "marketing/SaveCouponActivity" "{\"platformId\":$PLATFORM_ID,\"name\":\"满赠回馈券\",\"couponTemplateId\":$TEMPLATE2_ID,\"scopeType\":1,\"totalStock\":100,\"perUserLimit\":5,\"isClaimable\":false}")
GIFT_ACT_ID=$(jq -r '.data.id' <<<"$gift_act")

echo "== 2. 创建活动（满100减10 / 满赠）"
act=$(post_admin "marketing/SaveActivity" "{\"platformId\":$PLATFORM_ID,\"name\":\"满100减10\",\"activityType\":1,\"threshold\":100,\"discountValue\":10,\"scopeType\":1,\"startAt\":\"2026-01-01T00:00:00\",\"isEnabled\":true}")
check "满减活动创建" "$(jq -r '.code' <<<"$act")" 200
ACT_ID=$(jq -r '.data.id' <<<"$act")
gift=$(post_admin "marketing/SaveActivity" "{\"platformId\":$PLATFORM_ID,\"name\":\"满1元赠券\",\"activityType\":3,\"threshold\":1,\"discountValue\":0,\"giftCouponActivityId\":$GIFT_ACT_ID,\"scopeType\":1,\"startAt\":\"2026-01-01T00:00:00\",\"isEnabled\":true}")
check "满赠活动创建" "$(jq -r '.code' <<<"$gift")" 200

echo "== 3. 用户注册、领券、券包"
reg=$(curl -s -X POST "$BASE/customers/Register" -H 'Content-Type: application/json' -d "{\"userName\":\"mk${TS}\",\"password\":\"Test1234\",\"email\":\"mk${TS}@test.com\",\"phone\":\"139${TS: -8}\",\"platformId\":$PLATFORM_ID,\"agreedAgreement\":true}")
USER_TOKEN=$(jq -r '.data.token' <<<"$reg")
USER_ID=$(jq -r '.data.user.id' <<<"$reg")
check "用户注册" "$(jq -r '.code' <<<"$reg")" 200

claimable=$(curl -s "$BASE/marketing/ClaimableCoupons?platformId=$PLATFORM_ID" -H "Authorization: Bearer $USER_TOKEN")
check "领券中心含券活动" "$(jq -r '[.data.items[] | select((.id|tostring)=="'"$COUPON_ACT_ID"'")] | length' <<<"$claimable")" 1
claim=$(curl -s -X POST "$BASE/marketing/ClaimCoupon" -H "Authorization: Bearer $USER_TOKEN" -H 'Content-Type: application/json' -d "{\"couponActivityId\":$COUPON_ACT_ID}")
check "领取优惠券" "$(jq -r '.code' <<<"$claim")" 200
USER_COUPON_ID=$(jq -r '.data.userCouponId' <<<"$claim")
again=$(curl -s -X POST "$BASE/marketing/ClaimCoupon" -H "Authorization: Bearer $USER_TOKEN" -H 'Content-Type: application/json' -d "{\"couponActivityId\":$COUPON_ACT_ID}")
check "重复领取被拦截" "$(jq -r '.code' <<<"$again")" 400
mine=$(curl -s "$BASE/marketing/MyCoupons" -H "Authorization: Bearer $USER_TOKEN")
check "券包含新券" "$(jq -r '[.data.items[] | select((.id|tostring)=="'"$USER_COUPON_ID"'")] | length' <<<"$mine")" 1

preview_body() { echo "{\"platformId\":$PLATFORM_ID,\"items\":[{\"skuId\":$1,\"platformId\":$PLATFORM_ID,\"merchantId\":$MERCHANT_ID,\"productName\":\"营销测试商品\",\"price\":$2,\"quantity\":1}]}"; }

echo "== 4. 结算预览与优先级（券优先）"
preview=$(curl -s -X POST "$BASE/marketing/SettlePreview" -H "Authorization: Bearer $USER_TOKEN" -H 'Content-Type: application/json' -d "$(preview_body $((SKU_BASE+1)) 120)")
check "券优先命中券5元" "$(jq -r '.data.couponDiscount' <<<"$preview")" "5.00"
check "券优先不叠加活动" "$(jq -r '.data.activityDiscount' <<<"$preview")" "0"

save_cfg=$(post_admin "marketing/SaveConfig" "{\"platformId\":$PLATFORM_ID,\"discountPriority\":1}")
check "保存活动优先配置" "$(jq -r '.code' <<<"$save_cfg")" 200
preview2=$(curl -s -X POST "$BASE/marketing/SettlePreview" -H "Authorization: Bearer $USER_TOKEN" -H 'Content-Type: application/json' -d "$(preview_body $((SKU_BASE+2)) 120)")
check "活动优先命中活动10元" "$(jq -r '.data.activityDiscount' <<<"$preview2")" "10.00"
check "活动优先不叠加券" "$(jq -r '.data.couponDiscount' <<<"$preview2")" "0"
post_admin "marketing/SaveConfig" "{\"platformId\":$PLATFORM_ID,\"discountPriority\":2}" >/dev/null

echo "== 5. 下单使用券（券优先）"
order=$(curl -s -X POST "$BASE/orders/Create" -H "Authorization: Bearer $USER_TOKEN" -H 'Content-Type: application/json' -d "{
  \"idempotencyKey\":\"mkt-${TS}\",\"platformId\":$PLATFORM_ID,\"customerNo\":\"U$USER_ID\",\"customerName\":\"营销测试\",
  \"receiverName\":\"收货人\",\"receiverPhone\":\"13800000000\",\"receiverAddress\":\"测试地址\",
  \"selectedUserCouponIds\":[$USER_COUPON_ID],
  \"items\":[{\"skuId\":$((SKU_BASE+3)),\"platformId\":$PLATFORM_ID,\"merchantId\":$MERCHANT_ID,\"productName\":\"营销测试商品\",\"price\":120,\"quantity\":1}]}")
check "下单成功" "$(jq -r '.data.success' <<<"$order")" true
check "实付=120-5" "$(jq -r '.data.paymentPrice' <<<"$order")" "115.00"
check "总优惠=5" "$(jq -r '.data.discountAmount' <<<"$order")" "5.00"
ORDER_NO=$(jq -r '.data.orderNo' <<<"$order")
ORDER_ID=$(jq -r '.data.id' <<<"$order")

detail=$(curl -s "$BASE/orders/Get?id=$ORDER_ID&customerId=$USER_ID" -H "Authorization: Bearer $USER_TOKEN")
check "订单详情含券折扣" "$(jq -r '.data.allDiscountPrice' <<<"$detail")" "5.00"
check "订单详情含卡券优惠" "$(jq -r '.data.couponDiscountPrice' <<<"$detail")" "5.00"

echo "== 6. 支付成功与用券报表"
pay=$(curl -s -X POST "$BASE/payments/Create" -H "Authorization: Bearer $USER_TOKEN" -H 'Content-Type: application/json' -d "{\"bizNo\":\"$ORDER_NO\",\"platformId\":$PLATFORM_ID,\"merchantId\":$MERCHANT_ID,\"userId\":$USER_ID,\"amount\":115,\"items\":[{\"skuId\":$((SKU_BASE+3)),\"quantity\":1}]}")
check "支付单创建" "$(jq -r '.code' <<<"$pay")" 200
confirm=$(curl -s -X POST "$BASE/payments/Confirm" -H "Authorization: Bearer $USER_TOKEN" -H 'Content-Type: application/json' -d "{\"bizNo\":\"$ORDER_NO\",\"items\":[{\"skuId\":$((SKU_BASE+3)),\"quantity\":1}]}")
check "支付确认" "$(jq -r '.data.status' <<<"$confirm")" "20"

coupon_report=$(curl -s "$BASE/marketing/CouponReport?couponActivityId=$COUPON_ACT_ID&page=1&pageSize=10" -H "Authorization: Bearer $ADMIN")
check "券报表计入订单" "$(jq -r '[.data.records[] | select(.orderNo=="'"$ORDER_NO"'")] | length' <<<"$coupon_report")" 1
check "券报表折扣合计" "$(jq -r '[.data.summary[] | select((.couponActivityId|tostring)=="'"$COUPON_ACT_ID"'")][0].discountAmount' <<<"$coupon_report")" "5.00"

echo "== 7. 活动订单与满赠发券"
reg2=$(curl -s -X POST "$BASE/customers/Register" -H 'Content-Type: application/json' -d "{\"userName\":\"mk${TS}b\",\"password\":\"Test1234\",\"email\":\"mk${TS}b@test.com\",\"phone\":\"138${TS: -8}\",\"platformId\":$PLATFORM_ID,\"agreedAgreement\":true}")
USER2=$(jq -r '.data.token' <<<"$reg2")
USER2_ID=$(jq -r '.data.user.id' <<<"$reg2")
order2=$(curl -s -X POST "$BASE/orders/Create" -H "Authorization: Bearer $USER2" -H 'Content-Type: application/json' -d "{
  \"idempotencyKey\":\"mkt2-${TS}\",\"platformId\":$PLATFORM_ID,\"customerNo\":\"U$USER2_ID\",\"customerName\":\"满赠测试\",
  \"receiverName\":\"收货人\",\"receiverPhone\":\"13800000000\",\"receiverAddress\":\"测试地址\",
  \"items\":[{\"skuId\":$((SKU_BASE+4)),\"platformId\":$PLATFORM_ID,\"merchantId\":$MERCHANT_ID,\"productName\":\"满赠商品\",\"price\":30,\"quantity\":1}]}")
check "满赠订单成功" "$(jq -r '.data.success' <<<"$order2")" true
ORDER2_NO=$(jq -r '.data.orderNo' <<<"$order2")
ORDER2_ID=$(jq -r '.data.id' <<<"$order2")
curl -s -X POST "$BASE/payments/Create" -H "Authorization: Bearer $USER2" -H 'Content-Type: application/json' -d "{\"bizNo\":\"$ORDER2_NO\",\"platformId\":$PLATFORM_ID,\"merchantId\":$MERCHANT_ID,\"userId\":$USER2_ID,\"amount\":30,\"items\":[{\"skuId\":$((SKU_BASE+4)),\"quantity\":1}]}" >/dev/null
curl -s -X POST "$BASE/payments/Confirm" -H "Authorization: Bearer $USER2" -H 'Content-Type: application/json' -d "{\"bizNo\":\"$ORDER2_NO\",\"items\":[{\"skuId\":$((SKU_BASE+4)),\"quantity\":1}]}" >/dev/null
sleep 12
mine2=$(curl -s "$BASE/marketing/MyCoupons" -H "Authorization: Bearer $USER2")
check "满赠券已发放到券包(来源=满赠)" "$(jq -r '[.data.items[] | select((.source|tonumber)==2)] | length' <<<"$mine2")" 1
activity_report=$(curl -s "$BASE/marketing/ActivityReport?activityId=$ACT_ID&page=1&pageSize=10" -H "Authorization: Bearer $ADMIN")
check "活动报表可用" "$(jq -r '.code' <<<"$activity_report")" 200

echo
echo "结果：通过 $PASS，失败 $FAIL"
[[ $FAIL -eq 0 ]]
