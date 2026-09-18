#!/usr/bin/env bash
set -euo pipefail

# 端到端主链路：注册 -> 下单 -> 支付 -> 商户发货 -> 用户签收。
BASE="${SIMPLESHOP_GATEWAY:-http://127.0.0.1:5008/gateway}"
PLATFORM_ID="${PLATFORM_ID:-13755080881608709}"
MERCHANT_ID="${MERCHANT_ID:-13755084495919109}"
SKU_ID="${SKU_ID:-13755084509486085}"
PRICE="${PRICE:-10.5}"

anon_post() { curl -fsS -X POST "$BASE/$1" -H 'Content-Type: application/json' -d "$2"; }
post() { curl -fsS -X POST "$BASE/$1" -H "Authorization: Bearer $CUSTOMER_TOKEN" -H 'Content-Type: application/json' -d "$2"; }
merchant_post() { curl -fsS -X POST "$BASE/$1" -H "Authorization: Bearer $MERCHANT_TOKEN" -H 'Content-Type: application/json' -d "$2"; }
get() { curl -fsS "$BASE/$1" -H "Authorization: Bearer $CUSTOMER_TOKEN"; }

timestamp=$(date +%s%N)
customer_name="e2e${timestamp}"
login_json=$(anon_post "customers/Register" "{\"userName\":\"$customer_name\",\"password\":\"E2e123456\",\"email\":\"$customer_name@test.local\",\"phone\":\"139${timestamp:0:8}\",\"platformId\":$PLATFORM_ID,\"agreedAgreement\":true}")
CUSTOMER_TOKEN=$(jq -r '.data.token' <<<"$login_json")
CUSTOMER_ID=$(jq -r '.data.user.id' <<<"$login_json")

# 后台账号登录：AuthService OpenIddict 令牌端点（password flow）。
merchant_login=$(curl -fsS -X POST "$BASE/auth/Token" -H 'Content-Type: application/x-www-form-urlencoded' \
  --data-urlencode 'grant_type=password' --data-urlencode 'username=merchantop' \
  --data-urlencode 'password=Op123456' --data-urlencode 'client_id=admin-app')
MERCHANT_TOKEN=$(jq -r '.access_token' <<<"$merchant_login")

order_payload=$(cat <<JSON
{
  "idempotencyKey": "e2e-$(date +%s%N)",
  "platformId": $PLATFORM_ID,
  "customerId": $CUSTOMER_ID,
  "customerNo": "U$CUSTOMER_ID",
  "customerName": "E2E用户",
  "receiverName": "收货人",
  "receiverPhone": "13800000000",
  "receiverAddress": "自动化测试地址",
  "items": [{"skuId": $SKU_ID, "platformId": $PLATFORM_ID, "merchantId": $MERCHANT_ID, "productName": "E2E商品", "price": $PRICE, "quantity": 1}],
  "stockItems": [{"skuId": $SKU_ID, "quantity": 1}]
}
JSON
)

echo "[1/5] 创建订单"
order_json=$(post "orders/Create" "$order_payload")
[[ $(jq -r '.data.success' <<<"$order_json") == true ]]
order_id=$(jq -r '.data.id' <<<"$order_json")
order_no=$(jq -r '.data.orderNo' <<<"$order_json")
echo "订单：$order_no / $order_id"

echo "[2/5] 创建并确认支付"
payment_payload="{\"bizNo\":\"$order_no\",\"platformId\":$PLATFORM_ID,\"merchantId\":$MERCHANT_ID,\"userId\":$CUSTOMER_ID,\"amount\":$PRICE,\"items\":[{\"skuId\":$SKU_ID,\"quantity\":1}]}"
post "payments/Create" "$payment_payload" >/dev/null
confirm_json=$(post "payments/Confirm" "{\"bizNo\":\"$order_no\",\"items\":[{\"skuId\":$SKU_ID,\"quantity\":1}]}")
[[ $(jq -r '.data.status' <<<"$confirm_json") == 20 ]]
sleep 5

echo "[3/5] 校验订单已支付"
order_state=$(get "orders/Get?id=$order_id&customerId=$CUSTOMER_ID" | jq -r '.data.orderStatus')
[[ $order_state == 20 ]]

echo "[4/5] 商户发货"
detail=$(get "orders/Detail?id=$order_id&customerId=$CUSTOMER_ID")
item=$(jq '.data.items[0]' <<<"$detail")
item_id=$(jq -r '.id' <<<"$item")
shipment_payload=$(jq -n --argjson orderId "$order_id" --argjson platformId "$PLATFORM_ID" --argjson merchantId "$MERCHANT_ID" --arg trackingNo "E2E$order_id" --argjson item "$item" '{orderId:$orderId,platformId:$platformId,merchantId:$merchantId,logisticsCompany:"E2E快递",trackingNo:$trackingNo,items:[{orderItemId:$item.id,skuId:$item.skuId,quantity:$item.quantity}]}')
shipment_json=$(merchant_post "orders/Shipment" "$shipment_payload")
[[ $(jq -r '.data.success' <<<"$shipment_json") == true ]]
sleep 1

echo "[5/5] 用户签收"
shipment_id=$(PGPASSWORD='Aa123456..' psql -h 127.0.0.1 -U postgres -d simpleshoporder -Atc "select \"Id\" from shipment where \"OrderId\"=$order_id limit 1")
receive_json=$(post "orders/Receive" "{\"shipmentId\":$shipment_id,\"customerId\":$CUSTOMER_ID}")
[[ $(jq -r '.data.status' <<<"$receive_json") == 50 ]]
echo "E2E 通过：订单、支付、库存、发货、签收全部成功。"
