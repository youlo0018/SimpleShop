#!/usr/bin/env bash
# SimpleShop API 回归测试（严格模式：HTTP 码 + 业务码 + 关键副作用）
#
# 覆盖 tests/TEST_CASES.md 中「自动化=api」的 P0/P1 用例：
#   认证/网关鉴权/多租户隔离/分类/商品/图片/购物车/订单/支付/退款/库存/活动/券/计算/报表/装修/校验/健康检查
#
# 用法：bash tests/e2e/api-regression.sh
# 前置：基础设施 + 全部后端服务已启动；演示平台（platformCode=demo）已由 seed 脚本创建。
set -uo pipefail

BASE="${SIMPLESHOP_GATEWAY:-http://127.0.0.1:5008/gateway}"
INVENTORY="${SIMPLESHOP_INVENTORY:-http://127.0.0.1:5062}"
ADMIN_USER="${SIMPLESHOP_ADMIN:-codexadmin}"
ADMIN_PASS="${SIMPLESHOP_ADMIN_PASS:-Admin123456}"
TS=$(date +%s)
PASS=0; FAIL=0
FAILED_LABELS=()

ok()   { PASS=$((PASS+1)); printf '  ✓ %s\n' "$1"; }
bad()  { FAIL=$((FAIL+1)); FAILED_LABELS+=("$1"); printf '  ✗ %s（期望[%s] 实际[%s]）\n' "$1" "$2" "$3"; }
check() { local label="$1" got="$2" want="$3"; [[ "$got" == "$want" ]] && ok "$label" || bad "$label" "$want" "$got"; }

# 请求：返回 "<http_code> <body>"
request() {
  local method="$1" path="$2" token="${3:-}" body="${4:-}"
  local args=(-s -m 15 -w '\n%{http_code}' -X "$method" "$BASE$path")
  [[ -n "$token" ]] && args+=(-H "Authorization: Bearer $token")
  [[ -n "$body" ]] && args+=(-H 'Content-Type: application/json' -d "$body")
  curl "${args[@]}"
}
http_of() { request "$@" | tail -n1; }
body_of() { request "$@" | sed '$d'; }
code_of() { body_of "$@" | jq -r '.code' 2>/dev/null; }
jq_of()   { local expr="$1"; shift; body_of "$@" | jq -r "$expr" 2>/dev/null; }

echo "== 准备测试数据 =="
ADMIN=$(jq_of '.data.token' POST '/users/Login' '' "{\"userName\":\"$ADMIN_USER\",\"password\":\"$ADMIN_PASS\"}")
[[ -n "$ADMIN" && "$ADMIN" != "null" ]] || { echo "无法登录后台账号，终止"; exit 1; }
PLATFORM_JSON=$(body_of GET '/platforms/List?page=1&pageSize=100' "$ADMIN" | jq -c '[.data.items[] | select(.platformCode=="demo")][0]')
PLATFORM=$(jq -r '.id' <<<"$PLATFORM_JSON")
[[ "$PLATFORM" != "null" ]] || { echo "未找到演示平台（demo），请先执行 node scripts/seed-test-data.js"; exit 1; }
MERCHANTS=$(body_of GET '/merchants/List?page=1&pageSize=100' "$ADMIN")
MERCHANT1=$(jq -r "[.data.items[] | select(.merchantName==\"演示旗舰店\")][0].id" <<<"$MERCHANTS")
MERCHANT2=$(jq -r "[.data.items[] | select(.merchantName==\"品质优选店\")][0].id" <<<"$MERCHANTS")
[[ "$MERCHANT1" != "null" && "$MERCHANT2" != "null" ]] || { echo "演示商户缺失，请先执行 seed 脚本"; exit 1; }
MERCHANT_TOKEN=$(jq_of '.data.token' POST '/users/Login' '' '{"userName":"demo-merchant","password":"Demo123456"}')

# 两个独立客户
AUTH_A=$(body_of POST '/users/Register' '' "{\"userName\":\"apit${TS}a\",\"password\":\"Test1234\",\"email\":\"apit${TS}a@test.com\",\"phone\":\"131${TS: -8}\"}")
TOKEN_A=$(jq -r '.data.token' <<<"$AUTH_A")
UID_A=$(jq -r '.data.user.id' <<<"$AUTH_A")
AUTH_B=$(body_of POST '/users/Register' '' "{\"userName\":\"apit${TS}b\",\"password\":\"Test1234\",\"email\":\"apit${TS}b@test.com\",\"phone\":\"132${TS: -8}\"}")
TOKEN_B=$(jq -r '.data.token' <<<"$AUTH_B")
[[ "$TOKEN_A" != "null" && "$TOKEN_B" != "null" ]] || { echo "测试客户注册失败"; exit 1; }

# 测试商品（含 3 个 SKU：常规 / 用于满减上限 / 用于指定商品范围）
CATEGORY=$(body_of GET '/products/GetCategoryTree' "$ADMIN" | jq -r '[.data[] | select(.children|length>0)][0].children[0].id // .data[0].id')
PRODUCT=$(body_of POST '/products/CreateProduct' "$ADMIN" "{
  \"platformId\":$PLATFORM,\"merchantId\":$MERCHANT1,\"name\":\"API回归商品$TS\",\"mainImage\":\"/static/placeholder.png\",
  \"categoryId\":$CATEGORY,\"brandId\":0,\"description\":\"API回归\",
  \"skus\":[
    {\"skuCode\":\"APIT-$TS-1\",\"price\":120,\"originalPrice\":150,\"stock\":50,\"image\":\"\",\"specName\":\"规格\",\"specValue\":\"标准\"},
    {\"skuCode\":\"APIT-$TS-2\",\"price\":10,\"originalPrice\":0,\"stock\":50,\"image\":\"\",\"specName\":\"规格\",\"specValue\":\"上限\"},
    {\"skuCode\":\"APIT-$TS-3\",\"price\":300,\"originalPrice\":0,\"stock\":50,\"image\":\"\",\"specName\":\"规格\",\"specValue\":\"范围\"}
  ]}")
PRODUCT_ID=$(jq -r '.data' <<<"$PRODUCT")
[[ "$PRODUCT_ID" != "null" ]] || { echo "测试商品创建失败：$(cat <<<"$PRODUCT")"; exit 1; }
body_of POST '/products/PublishProduct' "$ADMIN" "{\"id\":$PRODUCT_ID,\"approved\":true}" >/dev/null
DETAIL=$(body_of GET "/products/AdminDetail?id=$PRODUCT_ID" "$ADMIN")
SKU1=$(jq -r '.data.skus[0].id' <<<"$DETAIL")
SKU2=$(jq -r '.data.skus[1].id' <<<"$DETAIL")
SKU3=$(jq -r '.data.skus[2].id' <<<"$DETAIL")
sleep 2   # 等 product.created 事件在库存服务初始化库存

# 营销测试数据：0元减3券（限领1、库存可配置）、满100减10活动、满1减500（指定 SKU2，验证单行封顶）、满赠活动
TPL_ZERO=$(jq -r '.data.id' <<<"$(body_of POST '/marketing/SaveCouponTemplate' "$ADMIN" "{\"platformId\":$PLATFORM,\"name\":\"API0元减3-$TS\",\"couponType\":3,\"threshold\":0,\"discountValue\":3,\"validDays\":30}")")
COUPON_ACT=$(jq -r '.data.id' <<<"$(body_of POST '/marketing/SaveCouponActivity' "$ADMIN" "{\"platformId\":$PLATFORM,\"name\":\"API领券-$TS\",\"couponTemplateId\":$TPL_ZERO,\"scopeType\":1,\"totalStock\":100,\"perUserLimit\":1,\"isClaimable\":true}")")
COUPON_ACT_LIMIT=$(jq -r '.data.id' <<<"$(body_of POST '/marketing/SaveCouponActivity' "$ADMIN" "{\"platformId\":$PLATFORM,\"name\":\"API限量券-$TS\",\"couponTemplateId\":$TPL_ZERO,\"scopeType\":1,\"totalStock\":1,\"perUserLimit\":5,\"isClaimable\":true}")")
GIFT_COUPON_ACT=$(jq -r '.data.id' <<<"$(body_of POST '/marketing/SaveCouponActivity' "$ADMIN" "{\"platformId\":$PLATFORM,\"name\":\"API赠品券-$TS\",\"couponTemplateId\":$TPL_ZERO,\"scopeType\":1,\"totalStock\":10,\"perUserLimit\":10,\"isClaimable\":false}")")
ACT_100_10=$(jq -r '.data.id' <<<"$(body_of POST '/marketing/SaveActivity' "$ADMIN" "{\"platformId\":$PLATFORM,\"name\":\"API满100减10-$TS\",\"activityType\":1,\"threshold\":100,\"discountValue\":10,\"scopeType\":1,\"startAt\":\"2026-01-01T00:00:00\",\"isEnabled\":true}")")
ACT_CAP=$(jq -r '.data.id' <<<"$(body_of POST '/marketing/SaveActivity' "$ADMIN" "{\"platformId\":$PLATFORM,\"name\":\"API封顶活动-$TS\",\"activityType\":1,\"threshold\":1,\"discountValue\":500,\"scopeType\":3,\"targetProducts\":[{\"skuId\":$SKU2,\"merchantId\":$MERCHANT1}],\"startAt\":\"2026-01-01T00:00:00\",\"isEnabled\":true}")")
ACT_GIFT=$(jq -r '.data.id' <<<"$(body_of POST '/marketing/SaveActivity' "$ADMIN" "{\"platformId\":$PLATFORM,\"name\":\"API满赠-$TS\",\"activityType\":3,\"threshold\":1,\"discountValue\":0,\"giftCouponActivityId\":$GIFT_COUPON_ACT,\"scopeType\":1,\"startAt\":\"2026-01-01T00:00:00\",\"isEnabled\":true}")")
echo "  平台=$PLATFORM 商户=$MERCHANT1/$MERCHANT2 商品=$PRODUCT_ID SKU=$SKU1/$SKU2/$SKU3 券活动=$COUPON_ACT"

echo
echo "== SYS：健康检查与 Consul =="
HEALTH_OK=1
for port in 5019 5022 5070 5058 5011 5060 5062 5064 5066 5072 5088 5008; do
  [[ "$(curl -s -m 3 -o /dev/null -w '%{http_code}' "http://127.0.0.1:$port/health")" != "200" ]] && HEALTH_OK=0
done
check "SYS-01 12 个服务 /health 全 200" "$HEALTH_OK" "1"
SERVICES=$(curl -s -m 5 http://127.0.0.1:8500/v1/catalog/services | jq -r 'keys | length')
check "SYS-03 Consul 已注册服务数 ≥ 11" "$([[ $SERVICES -ge 11 ]] && echo yes || echo no)" "yes"

echo
echo "== AUTH / USER：认证、注册与列表 =="
check "AUTH-01 正确账号登录" "$(code_of POST '/users/Login' '' "{\"userName\":\"$ADMIN_USER\",\"password\":\"$ADMIN_PASS\"}")" "200"
check "AUTH-02 错误密码" "$(code_of POST '/users/Login' '' '{"userName":"codexadmin","password":"wrong-password"}')" "400"
check "AUTH-03 不存在用户" "$(code_of POST '/users/Login' '' "{\"userName\":\"no_such_$TS\",\"password\":\"Test1234\"}")" "400"
check "AUTH-04 用户名超长(HTTP)" "$(http_of POST '/users/Login' '' "{\"userName\":\"$(printf 'a%.0s' {1..80})\",\"password\":\"Test1234\"}")" "400"
check "AUTH-05 密码为空(HTTP)" "$(http_of POST '/users/Login' '' '{"userName":"codexadmin","password":""}')" "400"
check "AUTH-10 注册邮箱非法(HTTP)" "$(http_of POST '/users/Register' '' "{\"userName\":\"badmail$TS\",\"password\":\"Test1234\",\"email\":\"bad-email\",\"phone\":\"13800000000\"}")" "400"
check "AUTH-11 重复注册" "$(code_of POST '/users/Register' '' "{\"userName\":\"apit${TS}a\",\"password\":\"Test1234\",\"email\":\"x$TS@test.com\",\"phone\":\"133${TS: -8}\"}")" "400"
check "USER-01 用户分页结构" "$(jq_of '.data | has("items") and has("total")' GET '/users/Users?page=1&pageSize=10' "$ADMIN")" "true"
check "USER-02 分页越界(HTTP)" "$(http_of GET '/users/Users?page=1&pageSize=1000' "$ADMIN")" "400"
check "USER-03 建号非法手机号" "$(code_of POST '/users/Create' "$ADMIN" "{\"userName\":\"badphone$TS\",\"password\":\"Test1234\",\"email\":\"ok$TS@test.com\",\"phone\":\"123\",\"role\":\"customer\"}")" "400"
check "USER-06 客户读取本人资料" "$(jq_of '.data.userName' GET '/users/Profile' "$TOKEN_A")" "apit${TS}a"
check "USER-08 地址非法手机号" "$(code_of POST '/users/SaveAddress' "$TOKEN_A" '{"receiverName":"测试","receiverPhone":"123","province":"广东省","city":"深圳市","district":"南山区","detail":"测试路1号","isDefault":false}')" "400"
check "USER-11 收藏 ID 为空" "$(code_of POST '/users/ToggleFavorite' "$TOKEN_A" '{"productId":0}')" "400"
body_of POST '/users/ToggleFavorite' "$TOKEN_A" "{\"productId\":$PRODUCT_ID}" >/dev/null
check "USER-10 取消收藏" "$(jq_of '.data.favorited' POST '/users/ToggleFavorite' "$TOKEN_A" "{\"productId\":$PRODUCT_ID}")" "false"

echo
echo "== GW / TEN：鉴权与多租户隔离 =="
check "AUTH-07 无 token 访问受保护接口(HTTP)" "$(http_of GET '/users/Users' '')" "401"
check "AUTH-08 畸形 token(HTTP)" "$(http_of GET '/users/Users' 'not-a-jwt')" "401"
check "AUTH-09 过期 token(HTTP)" "$(http_of GET '/users/Users' 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxIiwiZXhwIjoxMDAwMDAwMDAwfQ.sig')" "401"
check "AUTH-06 客户访问后台接口(HTTP)" "$(http_of GET '/users/Users' "$TOKEN_A")" "403"
check "GW-01 伪造 X-Claim 头(HTTP)" "$(curl -s -m 5 -o /dev/null -w '%{http_code}' "$BASE/users/Users" -H 'X-Claim-UserId: 1' -H 'X-Claim-TenantType: platform')" "401"
check "GW-06 游客活动专区(HTTP)" "$(http_of GET "/marketing/ActiveActivities?platformId=$PLATFORM" '')" "200"
check "GW-07 游客店铺信息(HTTP)" "$(http_of GET "/merchants/Shop?id=$MERCHANT1" '')" "200"
check "GW-08 客户访问营销后台(HTTP)" "$(http_of GET '/marketing/ActivityList?page=1&pageSize=5' "$TOKEN_A")" "403"
# 商户 A 修改商户 B 的商品：构造一个 B 的商品
PRODUCT_B=$(jq -r '.data' <<<"$(body_of POST '/products/CreateProduct' "$ADMIN" "{
  \"platformId\":$PLATFORM,\"merchantId\":$MERCHANT2,\"name\":\"API隔离商品$TS\",\"mainImage\":\"/static/placeholder.png\",
  \"categoryId\":$CATEGORY,\"brandId\":0,\"description\":\"隔离\",
  \"skus\":[{\"skuCode\":\"APIT-B-$TS-1\",\"price\":50,\"originalPrice\":0,\"stock\":10,\"image\":\"\",\"specName\":\"规格\",\"specValue\":\"标准\"}]}")")
check "GW-03 商户改他商商品(越权拒绝)" "$(code_of POST '/products/Update' "$MERCHANT_TOKEN" "{\"id\":$PRODUCT_B,\"categoryId\":$CATEGORY,\"name\":\"越权改名$TS\",\"mainImage\":\"/static/placeholder.png\",\"description\":\"\",\"skus\":[{\"skuCode\":\"APIT-B-$TS-1\",\"price\":1,\"originalPrice\":0,\"stock\":1,\"image\":\"\"}]}")" "404"
check "GW-03b 他商商品名称未被修改" "$(jq_of '.data.product.name' GET "/products/AdminDetail?id=$PRODUCT_B" "$ADMIN" | grep -c '越权改名')" "0"
check "GW-02 商户访问他人订单详情(无权)" "$(jq_of '.data.success' GET "/orders/Detail?id=1&merchantId=$MERCHANT2" "$MERCHANT_TOKEN")" "false"

echo
echo "== CAT / PRD / IMG =="
P1=$(jq -r '.data.id' <<<"$(body_of POST '/products/CreateCategory' "$ADMIN" "{\"name\":\"API一级$TS\",\"parentId\":0,\"sort\":0}")")
P2=$(jq -r '.data.id' <<<"$(body_of POST '/products/CreateCategory' "$ADMIN" "{\"name\":\"API二级$TS\",\"parentId\":$P1,\"sort\":0}")")
P3=$(jq -r '.data.id' <<<"$(body_of POST '/products/CreateCategory' "$ADMIN" "{\"name\":\"API三级$TS\",\"parentId\":$P2,\"sort\":0}")")
check "CAT-02 第四级分类被拒" "$(code_of POST '/products/CreateCategory' "$ADMIN" "{\"name\":\"API四级$TS\",\"parentId\":$P3,\"sort\":0}")" "400"
check "CAT-03 分类名为空(HTTP)" "$(http_of POST '/products/CreateCategory' "$ADMIN" '{"name":"","parentId":0,"sort":0}')" "400"
check "CAT-04 分类树" "$(jq_of '.code' GET '/products/GetCategoryTree' "$ADMIN")" "200"
check "PRD-02 SKU 负价(HTTP)" "$(http_of POST '/products/CreateProduct' "$ADMIN" "{\"platformId\":$PLATFORM,\"merchantId\":$MERCHANT1,\"name\":\"非法$TS\",\"mainImage\":\"/x.png\",\"categoryId\":$CATEGORY,\"description\":\"\",\"skus\":[{\"skuCode\":\"BAD-$TS\",\"price\":-1,\"originalPrice\":0,\"stock\":1,\"image\":\"\"}]}")" "400"
check "PRD-02b 原价低于售价(HTTP)" "$(http_of POST '/products/CreateProduct' "$ADMIN" "{\"platformId\":$PLATFORM,\"merchantId\":$MERCHANT1,\"name\":\"非法$TS\",\"mainImage\":\"/x.png\",\"categoryId\":$CATEGORY,\"description\":\"\",\"skus\":[{\"skuCode\":\"BAD2-$TS\",\"price\":10,\"originalPrice\":5,\"stock\":1,\"image\":\"\"}]}")" "400"
check "PRD-03 名称超长(HTTP)" "$(http_of POST '/products/CreateProduct' "$ADMIN" "{\"platformId\":$PLATFORM,\"merchantId\":$MERCHANT1,\"name\":\"$(printf '商%.0s' {1..45})\",\"mainImage\":\"/x.png\",\"categoryId\":$CATEGORY,\"description\":\"\",\"skus\":[{\"skuCode\":\"BAD3-$TS\",\"price\":1,\"originalPrice\":0,\"stock\":1,\"image\":\"\"}]}")" "400"
check "PRD-04 SKU编码重复(HTTP)" "$(http_of POST '/products/CreateProduct' "$ADMIN" "{\"platformId\":$PLATFORM,\"merchantId\":$MERCHANT1,\"name\":\"重复编码$TS\",\"mainImage\":\"/x.png\",\"categoryId\":$CATEGORY,\"description\":\"\",\"skus\":[{\"skuCode\":\"DUP-$TS\",\"price\":1,\"originalPrice\":0,\"stock\":1,\"image\":\"\"},{\"skuCode\":\"dup-$TS\",\"price\":2,\"originalPrice\":0,\"stock\":1,\"image\":\"\"}]}")" "400"
check "PRD-07 编辑非法 SKU(HTTP)" "$(http_of POST '/products/Update' "$ADMIN" "{\"id\":$PRODUCT_ID,\"categoryId\":$CATEGORY,\"name\":\"API回归商品$TS\",\"mainImage\":\"/static/placeholder.png\",\"description\":\"\",\"skus\":[{\"skuCode\":\"APIT-$TS-1\",\"price\":0,\"originalPrice\":0,\"stock\":1,\"image\":\"\"}]}")" "400"
check "PRD-08 商品分页越界(HTTP)" "$(http_of GET '/products/List?page=1&pageSize=1000' "$ADMIN")" "400"
check "PRD-09 商品不存在" "$(code_of GET '/products/AdminDetail?id=999999999999' "$ADMIN")" "404"
# 图片上传：真实 PNG vs 伪 PNG
printf '\x89\x50\x4e\x47\x0d\x0a\x1a\x0a\x00\x00\x00\x0dIHDR' > /tmp/apit-ok.png
printf 'not an image' > /tmp/apit-fake.png
printf '<svg></svg>' > /tmp/apit-bad.svg
check "IMG-01 上传真实 PNG" "$(curl -s -m 10 -X POST "$BASE/products/Upload" -H "Authorization: Bearer $ADMIN" -F 'file=@/tmp/apit-ok.png;type=image/png' | jq -r '.code')" "200"
check "IMG-02 伪 PNG(魔数校验)" "$(curl -s -m 10 -X POST "$BASE/products/Upload" -H "Authorization: Bearer $ADMIN" -F 'file=@/tmp/apit-fake.png;type=image/png' | jq -r '.code')" "400"
check "IMG-03 非法扩展名" "$(curl -s -m 10 -X POST "$BASE/products/Upload" -H "Authorization: Bearer $ADMIN" -F 'file=@/tmp/apit-bad.svg;type=image/svg+xml' | jq -r '.code')" "400"

echo
echo "== CART：增量语义与校验 =="
body_of POST '/carts/Add' "$TOKEN_A" "{\"productId\":$PRODUCT_ID,\"skuId\":$SKU1,\"merchantId\":$MERCHANT1,\"platformId\":$PLATFORM,\"productName\":\"API回归\",\"image\":\"/x.png\",\"price\":120,\"quantity\":1}" >/dev/null
check "CART-01 加购数量为 1" "$(jq_of '.data.items[0].quantity' GET '/carts/Get' "$TOKEN_A")" "1"
body_of POST '/carts/Add' "$TOKEN_A" "{\"productId\":$PRODUCT_ID,\"skuId\":$SKU1,\"merchantId\":$MERCHANT1,\"platformId\":$PLATFORM,\"productName\":\"API回归\",\"price\":120,\"quantity\":1}" >/dev/null
check "CART-02 增量 +1 后为 2" "$(jq_of '.data.items[0].quantity' GET '/carts/Get' "$TOKEN_A")" "2"
check "CART-03 数量为 0(HTTP)" "$(http_of POST '/carts/Add' "$TOKEN_A" "{\"productId\":$PRODUCT_ID,\"skuId\":$SKU1,\"merchantId\":$MERCHANT1,\"platformId\":$PLATFORM,\"productName\":\"x\",\"price\":1,\"quantity\":0}")" "400"
check "CART-03b 负价(HTTP)" "$(http_of POST '/carts/Add' "$TOKEN_A" "{\"productId\":$PRODUCT_ID,\"skuId\":$SKU1,\"merchantId\":$MERCHANT1,\"platformId\":$PLATFORM,\"productName\":\"x\",\"price\":-1,\"quantity\":1}")" "400"
check "CART-04 移除条目" "$(code_of POST '/carts/Remove' "$TOKEN_A" "{\"skuId\":$SKU1}")" "200"

echo
echo "== ACT / CPN：活动与券 =="
check "ACT-01 创建活动" "$([[ "$ACT_100_10" != "null" ]] && echo yes || echo no)" "yes"
check "ACT-03 满赠缺券活动(HTTP)" "$(http_of POST '/marketing/SaveActivity' "$ADMIN" "{\"platformId\":$PLATFORM,\"name\":\"非法满赠$TS\",\"activityType\":3,\"threshold\":1,\"discountValue\":0,\"scopeType\":1,\"startAt\":\"2026-01-01T00:00:00\"}")" "400"
check "ACT-04 指定商户无商户(HTTP)" "$(http_of POST '/marketing/SaveActivity' "$ADMIN" "{\"platformId\":$PLATFORM,\"name\":\"非法范围$TS\",\"activityType\":1,\"threshold\":1,\"discountValue\":1,\"scopeType\":2,\"startAt\":\"2026-01-01T00:00:00\"}")" "400"
check "ACT-05 指定商品无商品(HTTP)" "$(http_of POST '/marketing/SaveActivity' "$ADMIN" "{\"platformId\":$PLATFORM,\"name\":\"非法范围2$TS\",\"activityType\":1,\"threshold\":1,\"discountValue\":1,\"scopeType\":3,\"startAt\":\"2026-01-01T00:00:00\"}")" "400"
check "ACT-10 进行中含新活动" "$(jq_of "[.data.activities[] | select((.id|tostring)==\"$ACT_100_10\")] | length" GET "/marketing/ActiveActivities?platformId=$PLATFORM" '')" "1"
check "CPN-02 0元减门槛强制为0" "$(jq_of '.data.threshold // "0"' POST '/marketing/SaveCouponTemplate' "$ADMIN" "{\"platformId\":$PLATFORM,\"name\":\"API门槛券-$TS\",\"couponType\":3,\"threshold\":100,\"discountValue\":1,\"validDays\":7}" | head -1)" "0"
check "CPN-03 满折折扣率非法(HTTP)" "$(http_of POST '/marketing/SaveCouponTemplate' "$ADMIN" "{\"platformId\":$PLATFORM,\"name\":\"API非法折扣-$TS\",\"couponType\":2,\"threshold\":100,\"discountValue\":1.5,\"validDays\":7}")" "400"
check "CPN-05 券活动引用不存在模板" "$(code_of POST '/marketing/SaveCouponActivity' "$ADMIN" "{\"platformId\":$PLATFORM,\"name\":\"API非法券活动-$TS\",\"couponTemplateId\":999999999999,\"scopeType\":1,\"totalStock\":1,\"perUserLimit\":1,\"isClaimable\":true}")" "400"
check "CPN-06 领券中心可见" "$(jq_of "[.data.items[] | select((.id|tostring)==\"$COUPON_ACT\")] | length" GET "/marketing/ClaimableCoupons?platformId=$PLATFORM" "$TOKEN_A")" "1"
CLAIM_A=$(body_of POST '/marketing/ClaimCoupon' "$TOKEN_A" "{\"couponActivityId\":$COUPON_ACT}")
USER_COUPON=$(jq -r '.data.userCouponId' <<<"$CLAIM_A")
check "CPN-07 领取成功" "$(jq -r '.code' <<<"$CLAIM_A")" "200"
check "CPN-07b 券包未使用 1 张" "$(jq_of '[.data.items[] | select((.status|tonumber)==1)] | length' GET '/marketing/MyCoupons' "$TOKEN_A")" "1"
check "CPN-08 超限领被拒" "$(code_of POST '/marketing/ClaimCoupon' "$TOKEN_A" "{\"couponActivityId\":$COUPON_ACT}")" "400"
body_of POST '/marketing/ClaimCoupon' "$TOKEN_B" "{\"couponActivityId\":$COUPON_ACT_LIMIT}" >/dev/null
check "CPN-09 库存 1 已被领完" "$(code_of POST '/marketing/ClaimCoupon' "$TOKEN_A" "{\"couponActivityId\":$COUPON_ACT_LIMIT}")" "400"

echo
echo "== CAL：优惠计算（互斥/优先级/0元减/封顶） =="
PREVIEW() { # $1=token $2=sku $3=price $4=selectedCouponIds(JSON or null)
  body_of POST '/marketing/SettlePreview' "$1" "{\"platformId\":$PLATFORM,\"selectedUserCouponIds\":$4,\"items\":[{\"skuId\":$2,\"platformId\":$PLATFORM,\"merchantId\":$MERCHANT1,\"productName\":\"API\",\"price\":$3,\"quantity\":1}]}"
}
body_of POST '/marketing/SaveConfig' "$ADMIN" "{\"platformId\":$PLATFORM,\"discountPriority\":2}" >/dev/null
RES=$(PREVIEW "$TOKEN_A" "$SKU1" 120 "$(jq -nc --argjson id "$USER_COUPON" '[$id]')")
check "CAL-01 券优先命中券3元" "$(jq -r '.data.couponDiscount' <<<"$RES")" "3.00"
check "CAL-01b 券优先不叠加活动" "$(jq -r '.data.activityDiscount' <<<"$RES")" "0"
body_of POST '/marketing/SaveConfig' "$ADMIN" "{\"platformId\":$PLATFORM,\"discountPriority\":1}" >/dev/null
RES=$(PREVIEW "$TOKEN_A" "$SKU1" 120 "$(jq -nc --argjson id "$USER_COUPON" '[$id]')")
# 活动优先取"折扣最大"：seeded 商户活动满100减15 > 本脚本的满100减10，故期望 15
check "CAL-02 活动优先命中最优活动15元" "$(jq -r '.data.activityDiscount' <<<"$RES")" "15.00"
check "CAL-02b 活动优先不叠加券" "$(jq -r '.data.couponDiscount' <<<"$RES")" "0"
body_of POST '/marketing/SaveConfig' "$ADMIN" "{\"platformId\":$PLATFORM,\"discountPriority\":2}" >/dev/null
# SKU2 命中封顶活动（满1减500 → 单行最多抵扣到 0.01）；显式传 [] 不使用券
RES=$(PREVIEW "$TOKEN_B" "$SKU2" 10 '[]')
check "CAL-05 单行封顶实付0.01" "$(jq -r '.data.items[0].payAmount' <<<"$RES")" "0.01"
# 0元减3 的券：商品行金额必须大于券优惠（3 元行不命中）
RES=$(PREVIEW "$TOKEN_A" "$SKU1" 3 "$(jq -nc --argjson id "$USER_COUPON" '[$id]')")
check "CAL-04 0元减3不命中3元行" "$(jq -r '.data.couponDiscount' <<<"$RES")" "0"
check "CAL-08 勾选无效券失败" "$(code_of POST '/marketing/SettlePreview' "$TOKEN_A" "{\"platformId\":$PLATFORM,\"selectedUserCouponIds\":[999999999999],\"items\":[{\"skuId\":$SKU1,\"platformId\":$PLATFORM,\"merchantId\":$MERCHANT1,\"productName\":\"x\",\"price\":120,\"quantity\":1}]}")" "400"
check "CAL-13 游客仅活动" "$(jq -r '.data.couponDiscount' <<<"$(PREVIEW '' "$SKU1" 120 null)")" "0"

echo
echo "== ORD / PAY / REF：下单、支付、退款 =="
ORDER() { # $1=token $2=key $3=sku $4=price $5=quantity $6=phone $7=selectedCoupons
  body_of POST '/orders/Create' "$1" "{
    \"idempotencyKey\":\"$2\",\"platformId\":$PLATFORM,\"customerNo\":\"U$( [[ $1 == "$TOKEN_A" ]] && echo $UID_A || echo 0 )\",
    \"customerName\":\"API测试\",\"receiverName\":\"收货人\",\"receiverPhone\":\"${6:-13800000000}\",\"receiverAddress\":\"API测试地址\",
    \"selectedUserCouponIds\":$7,
    \"items\":[{\"skuId\":$3,\"platformId\":$PLATFORM,\"merchantId\":$MERCHANT1,\"productName\":\"API商品\",\"price\":$4,\"quantity\":$5}],
    \"stockItems\":[{\"skuId\":$3,\"quantity\":$5}]}"
}
ORDER1=$(ORDER "$TOKEN_A" "apit-$TS-o1" "$SKU1" 120 1 '' "$(jq -nc --argjson id "$USER_COUPON" '[$id]')")
ORDER_NO=$(jq -r '.data.orderNo' <<<"$ORDER1")
ORDER_ID=$(jq -r '.data.id' <<<"$ORDER1")
check "MKT-01 带券下单成功" "$(jq -r '.data.success' <<<"$ORDER1")" "true"
check "MKT-01b 实付=120-3" "$(jq -r '.data.paymentPrice' <<<"$ORDER1")" "117.00"
check "MKT-02 券已占用" "$(jq_of "[.data.items[] | select((.id|tostring)==\"$USER_COUPON\")][0].status" GET '/marketing/MyCoupons' "$TOKEN_A")" "2"
DUP=$(ORDER "$TOKEN_A" "apit-$TS-o1" "$SKU1" 120 1 '' '[]')
check "ORD-02 幂等键重复下单返回同一订单" "$(jq -r '.data.orderNo' <<<"$DUP")" "$ORDER_NO"
check "ORD-03 下单手机号非法(HTTP)" "$(http_of POST '/orders/Create' "$TOKEN_A" "{\"idempotencyKey\":\"apit-$TS-bad\",\"platformId\":$PLATFORM,\"customerNo\":\"U$UID_A\",\"customerName\":\"x\",\"receiverName\":\"x\",\"receiverPhone\":\"123\",\"receiverAddress\":\"x\",\"items\":[{\"skuId\":$SKU1,\"productName\":\"x\",\"price\":1,\"quantity\":1}]}")" "400"
# 库存不足：库存 50，下单 99
ORDER_FAIL=$(ORDER "$TOKEN_B" "apit-$TS-stock" "$SKU1" 120 99 '' '[]')
check "INV-04 库存不足下单失败" "$(jq -r '.data.success' <<<"$ORDER_FAIL")" "false"
check "INV-04b 失败提示" "$(jq -r '.data.message' <<<"$ORDER_FAIL" | grep -c '库存')" "1"
# 支付：幂等创建 + 重复确认
PAY_BODY="{\"bizNo\":\"$ORDER_NO\",\"platformId\":$PLATFORM,\"merchantId\":$MERCHANT1,\"userId\":$UID_A,\"amount\":117,\"items\":[{\"skuId\":$SKU1,\"quantity\":1}]}"
body_of POST '/payments/Create' "$TOKEN_A" "$PAY_BODY" >/dev/null
PAY1_NO=$(jq_of '.data.paymentNo' POST '/payments/Create' "$TOKEN_A" "$PAY_BODY")
PAY2_NO=$(jq_of '.data.paymentNo' POST '/payments/Create' "$TOKEN_A" "$PAY_BODY")
check "PAY-02 支付单幂等" "$([[ -n "$PAY1_NO" && "$PAY1_NO" == "$PAY2_NO" ]] && echo yes || echo no)" "yes"
check "PAY-03 确认支付" "$(jq_of ".data.status" POST '/payments/Confirm' "$TOKEN_A" "{\"bizNo\":\"$ORDER_NO\",\"items\":[{\"skuId\":$SKU1,\"quantity\":1}]}")" "20"
check "PAY-04 重复确认幂等补发" "$(jq_of '.data.idempotent' POST '/payments/Confirm' "$TOKEN_A" "{\"bizNo\":\"$ORDER_NO\",\"items\":[{\"skuId\":$SKU1,\"quantity\":1}]}")" "true"
sleep 2
check "PAY-08 订单已支付" "$(jq_of '.data.orderStatus' GET "/orders/Get?id=$ORDER_ID&customerId=$UID_A" "$TOKEN_A")" "20"
check "PAY-06 支付金额为0(HTTP)" "$(http_of POST '/payments/Create' "$TOKEN_A" "{\"bizNo\":\"$ORDER_NO\",\"platformId\":$PLATFORM,\"merchantId\":$MERCHANT1,\"userId\":$UID_A,\"amount\":0,\"items\":[{\"skuId\":$SKU1,\"quantity\":1}]}")" "400"
check "PAY-07 确认缺明细(HTTP)" "$(http_of POST '/payments/Confirm' "$TOKEN_A" "{\"bizNo\":\"$ORDER_NO\"}")" "400"
check "REF-03 退款金额0(HTTP)" "$(http_of POST '/payments/Refund' "$TOKEN_A" "{\"bizNo\":\"$ORDER_NO\",\"amount\":0,\"reason\":\"测试\",\"items\":[]}")" "400"
check "REF-03b 退款缺原因(HTTP)" "$(http_of POST '/payments/Refund' "$TOKEN_A" "{\"bizNo\":\"$ORDER_NO\",\"amount\":1,\"items\":[]}")" "400"
REF=$(body_of POST '/payments/Refund' "$TOKEN_A" "{\"bizNo\":\"$ORDER_NO\",\"amount\":117,\"reason\":\"API全额退款\",\"items\":[{\"skuId\":$SKU1,\"quantity\":1}]}")
REF_ID=$(jq -r '.data.refundId // .data.id // empty' <<<"$REF")
check "REF-01 申请退款" "$(jq -r '.code' <<<"$REF")" "200"
check "REF-02 累计超额被拒" "$(code_of POST '/payments/Refund' "$TOKEN_A" "{\"bizNo\":\"$ORDER_NO\",\"amount\":1,\"reason\":\"超限\",\"items\":[]}")" "400"
REF_LIST=$(body_of GET "/payments/Refunds?keyword=$ORDER_NO&page=1&pageSize=5" "$ADMIN")
REF_ID=$(jq -r '.data.items[0].id' <<<"$REF_LIST")
check "REF-04 同意退款" "$(code_of POST '/payments/ApproveRefund' "$ADMIN" "{\"id\":$REF_ID}")" "200"
check "REF-06 重复审批被拒" "$(code_of POST '/payments/ApproveRefund' "$ADMIN" "{\"id\":$REF_ID}")" "400"
sleep 2
check "REF-04b 全额退款订单 60" "$(jq_of '.data.orderStatus' GET "/orders/Get?id=$ORDER_ID&customerId=$UID_A" "$TOKEN_A")" "60"
# 拒绝流程：另一笔订单申请部分退款后拒绝
ORDER2=$(ORDER "$TOKEN_B" "apit-$TS-o2" "$SKU1" 120 1 '' '[]')
ORDER2_NO=$(jq -r '.data.orderNo' <<<"$ORDER2")
body_of POST '/payments/Create' "$TOKEN_B" "{\"bizNo\":\"$ORDER2_NO\",\"platformId\":$PLATFORM,\"merchantId\":$MERCHANT1,\"userId\":0,\"amount\":120,\"items\":[{\"skuId\":$SKU1,\"quantity\":1}]}" >/dev/null
body_of POST '/payments/Confirm' "$TOKEN_B" "{\"bizNo\":\"$ORDER2_NO\",\"items\":[{\"skuId\":$SKU1,\"quantity\":1}]}" >/dev/null
sleep 2
body_of POST '/payments/Refund' "$TOKEN_B" "{\"bizNo\":\"$ORDER2_NO\",\"amount\":1,\"reason\":\"部分退款\",\"items\":[]}" >/dev/null
REF2_ID=$(jq -r '.data.items[0].id' <<<"$(body_of GET "/payments/Refunds?keyword=$ORDER2_NO&page=1&pageSize=5" "$ADMIN")")
check "REF-05 拒绝退款" "$(code_of POST '/payments/RejectRefund' "$ADMIN" "{\"id\":$REF2_ID,\"reason\":\"API拒绝\"}")" "200"
# 取消订单回退券：新领一张券再下单后取消
body_of POST '/marketing/ClaimCoupon' "$TOKEN_B" "{\"couponActivityId\":$COUPON_ACT}" >/dev/null
UC_B=$(jq_of '[.data.items[] | select((.status|tonumber)==1)][0].id' GET '/marketing/MyCoupons' "$TOKEN_B")
ORDER3=$(ORDER "$TOKEN_B" "apit-$TS-o3" "$SKU1" 120 1 '' "[$UC_B]")
ORDER3_ID=$(jq -r '.data.id' <<<"$ORDER3")
UC_B_STATUS_BEFORE=$(jq_of "[.data.items[] | select(.id==$UC_B)][0].status" GET '/marketing/MyCoupons' "$TOKEN_B")
body_of POST '/orders/Cancel' "$TOKEN_B" "{\"id\":$ORDER3_ID,\"reason\":\"API取消\"}" >/dev/null
sleep 2
check "MKT-04 取消后券回退未使用" "$(jq_of "[.data.items[] | select((.id|tostring)==\"$UC_B\")][0].status" GET '/marketing/MyCoupons' "$TOKEN_B")" "1"
check "ORD-10 重复取消被拒" "$(code_of POST '/orders/Cancel' "$TOKEN_B" "{\"id\":$ORDER3_ID,\"reason\":\"再取消\"}")" "400"
check "ORD-08 发货缺明细(HTTP)" "$(http_of POST '/orders/Shipment' "$ADMIN" "{\"orderId\":$ORDER3_ID,\"platformId\":$PLATFORM,\"merchantId\":$MERCHANT1,\"logisticsCompany\":\"SF\",\"trackingNo\":\"T1\",\"items\":[]}")" "400"
# 满赠：无可用折扣的用户 B 下低价订单（SKU3 无活动覆盖），支付后收到赠券
ORDER4=$(ORDER "$TOKEN_B" "apit-$TS-o4" "$SKU3" 30 1 '' '[]')
ORDER4_NO=$(jq -r '.data.orderNo' <<<"$ORDER4")
GIFT_BEFORE=$(jq_of '[.data.items[] | select((.source|tonumber)==2)] | length' GET '/marketing/MyCoupons' "$TOKEN_B")
body_of POST '/payments/Create' "$TOKEN_B" "{\"bizNo\":\"$ORDER4_NO\",\"platformId\":$PLATFORM,\"merchantId\":$MERCHANT1,\"userId\":0,\"amount\":30,\"items\":[{\"skuId\":$SKU3,\"quantity\":1}]}" >/dev/null
body_of POST '/payments/Confirm' "$TOKEN_B" "{\"bizNo\":\"$ORDER4_NO\",\"items\":[{\"skuId\":$SKU3,\"quantity\":1}]}" >/dev/null
sleep 4
GIFT_AFTER=$(jq_of '[.data.items[] | select((.source|tonumber)==2)] | length' GET '/marketing/MyCoupons' "$TOKEN_B")
check "MKT-06 满赠发券到券包" "$([[ ${GIFT_AFTER:-0} -gt ${GIFT_BEFORE:-0} ]] && echo yes || echo no)" "yes"

echo
echo "== INV / MKT 报表 / DSN：库存、记录与装修 =="
check "INV-07 锁库存空明细(HTTP)" "$(curl -s -m 5 -o /dev/null -w '%{http_code}' -X POST "$INVENTORY/api/Stock/Lock" -H 'Content-Type: application/json' -d '{"bizNo":"","items":[]}')" "400"
ACT_REPORT=$(body_of GET "/marketing/ActivityReport?activityId=$ACT_100_10&page=1&pageSize=10" "$ADMIN")
check "MKT-08 活动报表可查" "$(jq -r '.code' <<<"$ACT_REPORT")" "200"
COUPON_REPORT=$(body_of GET "/marketing/CouponReport?couponActivityId=$COUPON_ACT&page=1&pageSize=10" "$ADMIN")
check "MKT-09 券报表计入订单" "$(jq_of "[.data.records[] | select(.orderNo==\"$ORDER_NO\")] | length" GET "/marketing/CouponReport?couponActivityId=$COUPON_ACT&page=1&pageSize=10" "$ADMIN")" "1"
check "DSN-01 装修 Admin 接口" "$(jq -r '.code' <<<"$(body_of GET "/platform-configs/Admin?platformId=$PLATFORM" "$ADMIN")")" "200"
check "DSN-03 MiniApp 含我的服务" "$(jq_of '(.data.design.profile.services | length) > 0' GET "/platform-configs/MiniApp?platformCode=demo" '')" "true"
check "VAL-09 装修非法 JSON" "$(code_of POST '/platform-configs/Save' "$ADMIN" "{\"platformId\":$PLATFORM,\"configJson\":\"not-json\",\"publish\":false}")" "400"
check "VAL-09b 装修空配置" "$(code_of POST '/platform-configs/Save' "$ADMIN" "{\"platformId\":$PLATFORM,\"configJson\":\"\",\"publish\":false}")" "400"

echo
echo "================ 结果 ================"
printf '通过 %d，失败 %d\n' "$PASS" "$FAIL"
if [[ $FAIL -gt 0 ]]; then
  printf '失败用例：\n'
  printf '  - %s\n' "${FAILED_LABELS[@]}"
  exit 1
fi
