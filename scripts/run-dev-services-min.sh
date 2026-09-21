#!/usr/bin/env bash
# 最小内存启动（开发/低内存机器用，避免与 Rider 抢内存）：
#   - 工作站 GC + 积极回收 + 单进程堆硬上限 320MB
#   - 其余与 run-dev-services.sh 完全一致（服务清单/端口/日志）
set -u

export DOTNET_gcServer=0                 # 工作站 GC（默认服务器 GC 更吃内存）
export DOTNET_GCConserveMemory=9         # 0-9，越大越积极把内存还给系统
export DOTNET_GCHeapHardLimit=0x14000000 # 320MB 堆硬上限（超限进程会退出，开发足够）

start_service() {
  local name="$1"
  local project="$2"
  local port="$3"

  env ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS="http://0.0.0.0:${port}" \
    dotnet run --project "${project}" --no-build \
    >"logs/runtime/${name}.log" 2>&1 </dev/null &
  echo $! >"logs/runtime/${name}.pid"
}

mkdir -p logs/runtime
start_service Tool src/ToolService/ToolService.Api 5080
start_service Customer src/CustomerService/CustomerService.Api 5280
start_service Auth src/AuthService/AuthService/AuthService.Api 5019
start_service Permission src/PermissionService/PermissionService.Api 5022
start_service MerchantPlatform src/MerchantPlatformService/MerchantPlatformService.Api 5070
start_service Product src/ProductService/ProductService.Api 5058
start_service User src/UserService/UserService.Api 5011
start_service Cart src/CartService/CartService.Api 5060
start_service Inventory src/InventoryService/InventoryService.Api 5062
start_service Order src/OrderService/OrderService/OrderService.Api 5064
start_service Payment src/PaymentService/PaymentService.Api 5066
start_service Log src/LogService/LogService.Api 5088
start_service Marketing src/MarketingService/MarketingService.Api 5072
start_service Gateway Gateway/Ocelot.ApiGateway 5008

env DOTNET_gcServer=0 DOTNET_GCConserveMemory=9 DOTNET_GCHeapHardLimit=0x14000000 \
  DOTNET_ENVIRONMENT=Development dotnet run --project src/ScheduledService/ScheduledService --no-build \
  >logs/runtime/Scheduled.log 2>&1 </dev/null &
echo $! >logs/runtime/Scheduled.pid

wait
