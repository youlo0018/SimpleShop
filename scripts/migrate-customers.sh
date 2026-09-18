#!/usr/bin/env bash
# 客户账号域迁移：simpleshopuser（User 表）→ simpleshopcustomer（customer/customer_address/customer_favorite）
#
# 背景：架构要求前台客户归 CustomerService、后台账号（平台/商户/运营）归 UserService。
# 本脚本把 C 端客户数据复制到 CustomerService 库（保留雪花 Id，订单/购物车/营销的外键值不变），
# 并排除后台账号（legacy admin + 权限中心已绑定角色的账号）。
#
# 用法：
#   bash scripts/migrate-customers.sh            # 只复制（幂等，ON CONFLICT DO NOTHING）
#   bash scripts/migrate-customers.sh --purge    # 复制后从 User 表删除已迁移客户，完成账号域分离
#
# 前置：postgres 容器运行中；CustomerService 已启动过（customer 表已建）。
set -euo pipefail

USER_DB=simpleshopuser
CUSTOMER_DB=simpleshopcustomer
PERMISSION_DB=simpleshoppermission
PSQL() { docker exec -i postgres psql -U postgres -d "$1" -tAc "$2" | tr -d '\r'; }
PURGE="${1:-}"

echo "== 1/4 计算后台账号集合（legacy admin + 权限中心绑定） =="
ADMIN_IDS=$(PSQL "$USER_DB" "select string_agg(''||\"Id\", ',') from \"User\" where \"Role\"='admin' and \"IsDeleted\"=false")
BOUND_IDS=$(PSQL "$PERMISSION_DB" "select string_agg(''||\"UserId\", ',') from user_role where \"IsDeleted\"=false")
EXCLUDE=$(python3 -c "
ids = set(filter(None, '$ADMIN_IDS'.split(','))) | set(filter(None, '$BOUND_IDS'.split(',')))
print(','.join(sorted(ids)) if ids else '0')
")
echo "  后台账号 Id：$EXCLUDE"

echo "== 2/4 迁移客户账号（保留 Id，幂等） =="
{
  echo 'create temp table t_customer ("Id" bigint, "IsDeleted" boolean, "CreatedAt" timestamp, "UpdatedAt" timestamp, "DeletedAt" timestamp, "CustomeNo" varchar(24), "CustomerName" varchar(64), "Email" varchar(64), "Phone" varchar(18), "Avatar" varchar(255), "Gender" int, "Birth" timestamp, "IsCancel" boolean, pwd varchar(255), "Salt" varchar(50), "IsAllAgreeAgreement" boolean, "RegisterSource" int);'
  echo '\copy t_customer from stdin with (format csv)'
  PSQL "$USER_DB" "\copy (select \"Id\", \"IsDeleted\", \"CreatedAt\", \"UpdatedAt\", \"DeletedAt\", 'C'||\"Id\", \"UserName\", \"Email\", \"Phone\", \"Avatar\", 0, null::timestamp, \"IsCancel\", pwd, \"Salt\", \"IsAllAgreeAgreement\", 1 from \"User\" where \"Role\"='customer' and \"IsDeleted\"=false and \"Id\" not in ($EXCLUDE)) to stdout with (format csv)"
  echo '\.'
  echo 'insert into customer ("Id", "IsDeleted", "CreatedAt", "UpdatedAt", "DeletedAt", "CustomeNo", "CustomerName", "Email", "Phone", "Avatar", "Gender", "Birth", "IsCancel", pwd, "Salt", "IsAllAgreeAgreement", "RegisterSource") select * from t_customer on conflict ("Id") do nothing;'
} | docker exec -i postgres psql -U postgres -d "$CUSTOMER_DB" | tail -1

echo "== 3/4 迁移地址与收藏 =="
{
  echo 'create temp table t_address ("Id" bigint, "IsDeleted" boolean, "CreatedAt" timestamp, "UpdatedAt" timestamp, "DeletedAt" timestamp, "CustomerId" bigint, "ReceiverName" varchar(32), "ReceiverPhone" varchar(20), "Province" varchar(64), "City" varchar(64), "District" varchar(64), "Detail" varchar(255), "IsDefault" boolean);'
  echo '\copy t_address from stdin with (format csv)'
  PSQL "$USER_DB" "\copy (select \"Id\", \"IsDeleted\", \"CreatedAt\", \"UpdatedAt\", \"DeletedAt\", \"UserId\", \"ReceiverName\", \"ReceiverPhone\", \"Province\", \"City\", \"District\", \"Detail\", \"IsDefault\" from user_address where \"IsDeleted\"=false and \"UserId\" not in ($EXCLUDE)) to stdout with (format csv)"
  echo '\.'
  echo 'insert into customer_address ("Id", "IsDeleted", "CreatedAt", "UpdatedAt", "DeletedAt", "CustomerId", "ReceiverName", "ReceiverPhone", "Province", "City", "District", "Detail", "IsDefault") select * from t_address on conflict ("Id") do nothing;'
  echo 'create temp table t_favorite ("Id" bigint, "IsDeleted" boolean, "CreatedAt" timestamp, "UpdatedAt" timestamp, "DeletedAt" timestamp, "CustomerId" bigint, "ProductId" bigint);'
  echo '\copy t_favorite from stdin with (format csv)'
  PSQL "$USER_DB" "\copy (select \"Id\", \"IsDeleted\", \"CreatedAt\", \"UpdatedAt\", \"DeletedAt\", \"UserId\", \"ProductId\" from user_favorite where \"IsDeleted\"=false and \"UserId\" not in ($EXCLUDE)) to stdout with (format csv)"
  echo '\.'
  echo 'insert into customer_favorite ("Id", "IsDeleted", "CreatedAt", "UpdatedAt", "DeletedAt", "CustomerId", "ProductId") select * from t_favorite on conflict ("Id") do nothing;'
} | docker exec -i postgres psql -U postgres -d "$CUSTOMER_DB" | tail -1

echo "== 4/4 校验计数 =="
echo "  源库客户（排除后台）：$(PSQL "$USER_DB" "select count(*) from \"User\" where \"Role\"='customer' and \"IsDeleted\"=false and \"Id\" not in ($EXCLUDE)")"
echo "  目标库客户：$(PSQL "$CUSTOMER_DB" "select count(*) from customer")"
echo "  目标库地址：$(PSQL "$CUSTOMER_DB" "select count(*) from customer_address")"
echo "  目标库收藏：$(PSQL "$CUSTOMER_DB" "select count(*) from customer_favorite")"

if [[ "$PURGE" == "--purge" ]]; then
  echo "== --purge：从 User 表删除已迁移客户（账号域彻底分离） =="
  PSQL "$USER_DB" "delete from user_address where \"UserId\" not in ($EXCLUDE)" >/dev/null
  PSQL "$USER_DB" "delete from user_favorite where \"UserId\" not in ($EXCLUDE)" >/dev/null
  PSQL "$USER_DB" "delete from \"User\" where \"Role\"='customer' and \"IsDeleted\"=false and \"Id\" not in ($EXCLUDE)"
  echo "  清理后 User 表剩余账号：$(PSQL "$USER_DB" "select count(*) from \"User\" where \"IsDeleted\"=false")（应仅后台账号）"
fi
