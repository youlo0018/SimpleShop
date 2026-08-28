#!/usr/bin/env bash
set -u

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
start_service Gateway Gateway/Ocelot.ApiGateway 5008

env DOTNET_ENVIRONMENT=Development dotnet run --project src/ScheduledService/ScheduledService --no-build \
  >logs/runtime/Scheduled.log 2>&1 </dev/null &
echo $! >logs/runtime/Scheduled.pid

wait
