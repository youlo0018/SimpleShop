-- 为每个服务初始化独立的 AgileConfig 应用、配置草稿和 DEV 发布快照。
-- 公共项继续放在 public 应用中；服务专属的数据库、端口和注册名不允许共用。

CREATE OR REPLACE FUNCTION pg_temp.seed_agile_config(
    p_app_id text,
    configs jsonb,
    inherit_public boolean DEFAULT true
) RETURNS void LANGUAGE plpgsql AS $$
DECLARE
    next_version integer;
    timeline_id text;
    definition record;
BEGIN
    INSERT INTO agc_app (id, name, "group", secret, enabled, type, creator, create_time)
    VALUES (p_app_id, p_app_id, 'simpleShop', 'Aa123456..', true, 0, 'super_admin', now())
    ON CONFLICT (id) DO UPDATE
        SET name = EXCLUDED.name,
            "group" = EXCLUDED."group",
            secret = EXCLUDED.secret,
            enabled = true;

    IF inherit_public AND NOT EXISTS (
        SELECT 1 FROM "agc_appInheritanced"
        WHERE appid = p_app_id AND inheritanced_appid = 'public'
    ) THEN
        INSERT INTO "agc_appInheritanced" (appid, inheritanced_appid, sort, id)
        VALUES (p_app_id, 'public', 0, md5('inheritance:' || p_app_id));
    END IF;

    SELECT coalesce(max(version), 0) + 1 INTO next_version
    FROM agc_publish_timeline
    WHERE app_id = p_app_id AND env = 'DEV';

    timeline_id := md5(p_app_id || ':DEV:v' || next_version);
    DELETE FROM agc_config WHERE app_id = p_app_id AND env = 'DEV';
    DELETE FROM agc_publish_detail WHERE app_id = p_app_id AND env = 'DEV';
    DELETE FROM agc_config_published WHERE app_id = p_app_id AND env = 'DEV';

    INSERT INTO agc_publish_timeline
        (id, app_id, publish_time, publish_user_id, publish_user_name, version, log, env)
    VALUES
        (timeline_id, p_app_id, now(), 'super_admin', 'admin', next_version,
         'seeded by init-service-configs.sql', 'DEV');

    FOR definition IN
        SELECT entry->>'g' AS g,
               entry->>'k' AS k,
               entry->>'v' AS v,
               coalesce(entry->>'d', '') AS d
        FROM jsonb_array_elements(configs) AS entry
    LOOP
        INSERT INTO agc_config
            (id, app_id, "g", k, v, description, create_time, update_time,
             status, online_status, edit_status, env)
        VALUES
            (md5(p_app_id || '|config|' || definition.g || '|' || definition.k),
             p_app_id, definition.g, definition.k, definition.v, definition.d,
             now(), null, 1, 1, 10, 'DEV');

        INSERT INTO agc_publish_detail
            (id, app_id, "Version", publish_timeline_id, config_id, "g", k, v,
             description, edit_status, env)
        VALUES
            (md5(p_app_id || '|detail|' || definition.g || '|' || definition.k),
             p_app_id, next_version, timeline_id,
             md5(p_app_id || '|config|' || definition.g || '|' || definition.k),
             definition.g, definition.k, definition.v, definition.d, 10, 'DEV');

        INSERT INTO agc_config_published
            (id, app_id, "g", k, v, publish_time, config_id,
             publish_timeline_id, version, status, env)
        VALUES
            (md5(p_app_id || '|published|' || definition.g || '|' || definition.k),
             p_app_id, definition.g, definition.k, definition.v, now(),
             md5(p_app_id || '|config|' || definition.g || '|' || definition.k),
             timeline_id, next_version, 1, 'DEV');
    END LOOP;
END;
$$;

SELECT pg_temp.seed_agile_config('ProductService', $$
[
  {"g":"Basic","k":"sqlConnectionString","v":"Host=127.0.0.1;Port=5432;Database=simpleshopproduct;Username=postgres;Password=Aa123456..;Ssl Mode=Disable;","d":"商品库"},
  {"g":"Basic","k":"redisDb","v":"0","d":"Redis库"},
  {"g":"Basic:port","k":"grpcport","v":"5058","d":"gRPC 端口"},
  {"g":"Basic:Consul","k":"Enabled","v":"true","d":"启用服务注册"},
  {"g":"Basic:Consul","k":"ServiceName","v":"ProductService","d":"服务发现名称"},
  {"g":"Basic:Consul","k":"ServiceAddress","v":"172.18.0.1","d":"注册地址"},
  {"g":"Basic:Consul","k":"ServicePort","v":"5058","d":"HTTP 健康检查与路由端口"},
  {"g":"Basic:Consul","k":"HealthCheckEndpoint","v":"/health","d":"健康检查端点"},
  {"g":"Basic:port","k":"httpport","v":"5058","d":"REST 端口"},
  {"g":"Basic","k":"redisConnectionString","v":"localhost:6379,password=Aa123456..,defaultDatabase=0","d":"Redis 连接"},
  {"g":"RabbitMQ","k":"HostName","v":"localhost","d":"消息队列"},
  {"g":"RabbitMQ","k":"UserName","v":"admin","d":"消息队列账号"},
  {"g":"RabbitMQ","k":"Password","v":"admin123","d":"消息队列密码"},
  {"g":"RabbitMQ","k":"Exchange","v":"simpleshop.events","d":"事件交换机"}
]$$);

SELECT pg_temp.seed_agile_config('InventoryService', $$
[
  {"g":"Basic","k":"sqlConnectionString","v":"Host=127.0.0.1;Port=5432;Database=simpleshopinventory;Username=postgres;Password=Aa123456..;Ssl Mode=Disable;","d":"库存库"},
  {"g":"Basic","k":"redisDb","v":"1","d":"Redis库"},
  {"g":"Basic:port","k":"httpport","v":"5062","d":"REST 端口"},
  {"g":"Basic:port","k":"grpcport","v":"5063","d":"独立 gRPC 端口，明文 HTTP/2"},
  {"g":"Basic:Consul","k":"ServiceName","v":"InventoryService","d":"服务发现名称"},
  {"g":"Basic:Consul","k":"ServiceAddress","v":"172.18.0.1","d":"注册地址"},
  {"g":"Basic:Consul","k":"Enabled","v":"true","d":"启用服务注册"},
  {"g":"Basic:Consul","k":"ServicePort","v":"5062","d":"HTTP 健康检查与路由端口"},
  {"g":"Basic:Consul","k":"MetaData:GrpcPort","v":"5063","d":"Consul 服务间 gRPC 发现元数据"},
  {"g":"Basic:Consul","k":"HealthCheckEndpoint","v":"/health","d":"健康检查端点"},
  {"g":"Basic","k":"redisConnectionString","v":"localhost:6379,password=Aa123456..,defaultDatabase=1","d":"Redis 连接"},
  {"g":"RabbitMQ","k":"HostName","v":"localhost","d":"消息队列"},
  {"g":"RabbitMQ","k":"UserName","v":"admin","d":"消息队列账号"},
  {"g":"RabbitMQ","k":"Password","v":"admin123","d":"消息队列密码"},
  {"g":"RabbitMQ","k":"Exchange","v":"simpleshop.events","d":"事件交换机"}
]$$);

SELECT pg_temp.seed_agile_config('CartService', $$
[
  {"g":"Basic","k":"sqlConnectionString","v":"Host=127.0.0.1;Port=5432;Database=simpleshopcart;Username=postgres;Password=Aa123456..;Ssl Mode=Disable;","d":"购物车保留独立库，当前数据在 Redis"},
  {"g":"Basic","k":"redisDb","v":"0","d":"购物车 Redis 库"},
  {"g":"Basic:port","k":"grpcport","v":"5060","d":"gRPC 端口"},
  {"g":"Basic:Consul","k":"ServiceName","v":"CartService","d":"服务发现名称"},
  {"g":"Basic:Consul","k":"ServiceAddress","v":"172.18.0.1","d":"注册地址"},
  {"g":"Basic:Consul","k":"Enabled","v":"true","d":"启用服务注册"},
  {"g":"Basic:Consul","k":"ServicePort","v":"5060","d":"HTTP 健康检查与路由端口"},
  {"g":"Basic:Consul","k":"HealthCheckEndpoint","v":"/health","d":"健康检查端点"},
  {"g":"Basic:port","k":"httpport","v":"5060","d":"REST 端口"},
  {"g":"Basic","k":"redisConnectionString","v":"localhost:6379,password=Aa123456..,defaultDatabase=0","d":"Redis 连接"}
]$$);

SELECT pg_temp.seed_agile_config('PaymentService', $$
[
  {"g":"Basic","k":"sqlConnectionString","v":"Host=127.0.0.1;Port=5432;Database=simpleshoppayment;Username=postgres;Password=Aa123456..;Ssl Mode=Disable;","d":"支付库"},
  {"g":"Basic","k":"redisDb","v":"2","d":"Redis库"},
  {"g":"Basic:port","k":"grpcport","v":"5066","d":"gRPC 端口"},
  {"g":"Basic:Consul","k":"ServiceName","v":"PaymentService","d":"服务发现名称"},
  {"g":"Basic:Consul","k":"ServiceAddress","v":"172.18.0.1","d":"注册地址"},
  {"g":"Basic:Consul","k":"Enabled","v":"true","d":"启用服务注册"},
  {"g":"Basic:Consul","k":"ServicePort","v":"5066","d":"HTTP 健康检查与路由端口"},
  {"g":"Basic:Consul","k":"HealthCheckEndpoint","v":"/health","d":"健康检查端点"},
  {"g":"Basic:port","k":"httpport","v":"5066","d":"REST 端口"},
  {"g":"RabbitMQ","k":"HostName","v":"localhost","d":"消息队列"},
  {"g":"RabbitMQ","k":"UserName","v":"admin","d":"消息队列账号"},
  {"g":"RabbitMQ","k":"Password","v":"admin123","d":"消息队列密码"},
  {"g":"RabbitMQ","k":"Exchange","v":"simpleshop.events","d":"事件交换机"}
]$$);

SELECT pg_temp.seed_agile_config('MerchantPlatformService', $$
[
  {"g":"Basic","k":"sqlConnectionString","v":"Host=127.0.0.1;Port=5432;Database=simpleshopmerchant;Username=postgres;Password=Aa123456..;Ssl Mode=Disable;","d":"商户平台库"},
  {"g":"Basic","k":"redisDb","v":"3","d":"Redis库"},
  {"g":"Basic:port","k":"grpcport","v":"5070","d":"gRPC 端口"},
  {"g":"Basic:Consul","k":"ServiceName","v":"MerchantPlatformService","d":"服务发现名称"},
  {"g":"Basic:Consul","k":"ServiceAddress","v":"172.18.0.1","d":"注册地址"},
  {"g":"Basic:Consul","k":"Enabled","v":"true","d":"启用服务注册"},
  {"g":"Basic:Consul","k":"ServicePort","v":"5070","d":"HTTP 健康检查与路由端口"},
  {"g":"Basic:Consul","k":"HealthCheckEndpoint","v":"/health","d":"健康检查端点"},
  {"g":"Basic:port","k":"httpport","v":"5070","d":"REST 端口"}
]$$);

SELECT pg_temp.seed_agile_config('ScheduledService', $$
[
  {"g":"Basic","k":"sqlConnectionString","v":"Host=127.0.0.1;Port=5432;Database=simpleshopscheduled;Username=postgres;Password=Aa123456..;Ssl Mode=Disable;","d":"定时任务补偿独立库"},
  {"g":"Order","k":"sqlConnectionString","v":"Host=127.0.0.1;Port=5432;Database=simpleshoporder;Username=postgres;Password=Aa123456..;Ssl Mode=Disable;","d":"订单业务库，定时任务只读与关单更新"},
  {"g":"Basic","k":"redisConnectionString","v":"localhost:6379,password=Aa123456..,defaultDatabase=2","d":"分布式锁 Redis"},
  {"g":"Basic","k":"redisDb","v":"2","d":"Redis库"},
  {"g":"InventoryService","k":"ServiceName","v":"InventoryService","d":"Consul 服务发现名称"},
  {"g":"Basic:Consul","k":"Enabled","v":"false","d":"定时进程不注册流量入口"},
  {"g":"Basic:Consul","k":"Address","v":"http://localhost:8500","d":"服务发现地址"},
  {"g":"Jobs:PaymentTimeout","k":"BatchSize","v":"50","d":"每批关单数"},
  {"g":"Jobs:PaymentTimeout","k":"IntervalSeconds","v":"30","d":"执行间隔"},
  {"g":"Jobs:PaymentTimeout","k":"ScanLockExpirySeconds","v":"45","d":"全局扫描锁租期"},
  {"g":"RabbitMQ","k":"HostName","v":"localhost","d":"消息队列"},
  {"g":"RabbitMQ","k":"UserName","v":"admin","d":"消息队列账号"},
  {"g":"RabbitMQ","k":"Password","v":"admin123","d":"消息队列密码"},
  {"g":"RabbitMQ","k":"Exchange","v":"simpleshop.events","d":"事件交换机"}
]$$);

SELECT pg_temp.seed_agile_config('LogService', $$
[
  {"g":"urls","k":"urls","v":"http://127.0.0.1:5088","d":"日志 API 地址"},
  {"g":"Basic:Consul","k":"Enabled","v":"true","d":"启用服务注册"},
  {"g":"Basic:Consul","k":"ServiceName","v":"LogService","d":"服务发现名称"},
  {"g":"Basic:Consul","k":"ServiceAddress","v":"172.18.0.1","d":"注册地址"},
  {"g":"Basic:Consul","k":"ServicePort","v":"5088","d":"HTTP 健康检查与路由端口"},
  {"g":"Basic:Consul","k":"HealthCheckEndpoint","v":"/health","d":"健康检查端点"},
  {"g":"Elasticsearch","k":"Url","v":"http://localhost:9200","d":"ES 地址"},
  {"g":"LoggingApi","k":"ApiKey","v":"local-log-api-key","d":"查询 API 密钥"},
  {"g":"RabbitMQ","k":"HostName","v":"localhost","d":"消息队列"},
  {"g":"RabbitMQ","k":"UserName","v":"admin","d":"消息队列账号"},
  {"g":"RabbitMQ","k":"Password","v":"admin123","d":"消息队列密码"},
  {"g":"RabbitMQ","k":"Exchange","v":"simpleshop.events","d":"事件交换机"}
]$$);

SELECT pg_temp.seed_agile_config('OrderService', $$
[
  {"g":"Basic","k":"sqlConnectionString","v":"Host=127.0.0.1;Port=5432;Database=simpleshoporder;Username=postgres;Password=Aa123456..;Ssl Mode=Disable;","d":"订单库"},
  {"g":"Basic","k":"redisDb","v":"2","d":"Redis库"},
  {"g":"Basic:port","k":"httpport","v":"5064","d":"REST 端口"},
  {"g":"Basic:port","k":"grpcport","v":"5002","d":"gRPC 端口"},
  {"g":"Basic:Consul","k":"Enabled","v":"true","d":"启用服务注册"},
  {"g":"Basic:Consul","k":"ServiceName","v":"OrderService","d":"服务发现名称"},
  {"g":"Basic:Consul","k":"ServiceAddress","v":"172.18.0.1","d":"注册地址"},
  {"g":"Basic:Consul","k":"ServicePort","v":"5064","d":"HTTP 健康检查与路由端口"},
  {"g":"Basic","k":"redisConnectionString","v":"localhost:6379,password=Aa123456..,defaultDatabase=2","d":"Redis 连接"},
  {"g":"Basic:Consul","k":"HealthCheckEndpoint","v":"/health","d":"健康检查端点"},
  {"g":"RabbitMQ","k":"HostName","v":"localhost","d":"消息队列"},
  {"g":"RabbitMQ","k":"UserName","v":"admin","d":"消息队列账号"},
  {"g":"RabbitMQ","k":"Password","v":"admin123","d":"消息队列密码"},
  {"g":"RabbitMQ","k":"Exchange","v":"simpleshop.events","d":"事件交换机"}
]$$);

SELECT pg_temp.seed_agile_config('AuthService', $$
[
  {"g":"Basic","k":"sqlConnectionString","v":"Host=127.0.0.1;Port=5432;Database=simpleshopauth;Username=postgres;Password=Aa123456..;Ssl Mode=Disable;","d":"认证库"},
  {"g":"Basic","k":"redisDb","v":"4","d":"Redis库"},
  {"g":"Basic:port","k":"httpport","v":"5019","d":"REST 端口"},
  {"g":"Basic:port","k":"grpcport","v":"5004","d":"gRPC 端口"},
  {"g":"Basic","k":"redisConnectionString","v":"localhost:6379,password=Aa123456..,defaultDatabase=4","d":"Redis 连接"},
  {"g":"Basic:Consul","k":"Enabled","v":"true","d":"启用服务注册"},
  {"g":"Basic:Consul","k":"ServiceName","v":"AuthService","d":"服务发现名称"},
  {"g":"Basic:Consul","k":"ServiceAddress","v":"172.18.0.1","d":"注册地址"},
  {"g":"Basic:Consul","k":"ServicePort","v":"5019","d":"HTTP 健康检查与路由端口"},
  {"g":"Basic:Consul","k":"HealthCheckEndpoint","v":"/health","d":"健康检查端点"}
]$$);

SELECT pg_temp.seed_agile_config('UserService', $$
[
  {"g":"Basic","k":"sqlConnectionString","v":"Host=127.0.0.1;Port=5432;Database=simpleshopuser;Username=postgres;Password=Aa123456..;Ssl Mode=Disable;","d":"用户库"},
  {"g":"Basic","k":"redisDb","v":"3","d":"Redis库"},
  {"g":"Basic:port","k":"httpport","v":"5011","d":"REST 端口"},
  {"g":"Basic:port","k":"grpcport","v":"5003","d":"gRPC 端口"},
  {"g":"Basic","k":"redisConnectionString","v":"localhost:6379,password=Aa123456..,defaultDatabase=3","d":"Redis 连接"},
  {"g":"Basic:Consul","k":"Enabled","v":"true","d":"启用服务注册"},
  {"g":"Basic:Consul","k":"ServiceName","v":"UserService","d":"服务发现名称"},
  {"g":"Basic:Consul","k":"ServiceAddress","v":"172.18.0.1","d":"注册地址"},
  {"g":"Basic:Consul","k":"ServicePort","v":"5011","d":"HTTP 健康检查与路由端口"},
  {"g":"Basic:Consul","k":"MetaData:GrpcPort","v":"5003","d":"Consul 服务间 gRPC 发现元数据"},
  {"g":"Basic:Consul","k":"HealthCheckEndpoint","v":"/health","d":"健康检查端点"}
]$$);

SELECT pg_temp.seed_agile_config('CustomerService', $$
[
  {"g":"Basic","k":"sqlConnectionString","v":"Host=127.0.0.1;Port=5432;Database=simpleshopcustomer;Username=postgres;Password=Aa123456..;Ssl Mode=Disable;","d":"客户库"},
  {"g":"Basic","k":"redisDb","v":"1","d":"Redis库"},
  {"g":"Basic:port","k":"httpport","v":"5280","d":"REST 端口"},
  {"g":"Basic:port","k":"grpcport","v":"5001","d":"gRPC 端口"},
  {"g":"Basic","k":"redisConnectionString","v":"localhost:6379,password=Aa123456..,defaultDatabase=1","d":"Redis 连接"},
  {"g":"Basic:Consul","k":"Enabled","v":"true","d":"启用服务注册"},
  {"g":"Basic:Consul","k":"ServiceName","v":"CustomerService","d":"服务发现名称"},
  {"g":"Basic:Consul","k":"ServiceAddress","v":"172.18.0.1","d":"注册地址"},
  {"g":"Basic:Consul","k":"ServicePort","v":"5280","d":"HTTP 健康检查与路由端口"},
  {"g":"Basic:Consul","k":"HealthCheckEndpoint","v":"/health","d":"健康检查端点"}
]$$);

SELECT pg_temp.seed_agile_config('PermissionService', $$
[
  {"g":"Basic","k":"sqlConnectionString","v":"Host=127.0.0.1;Port=5432;Database=simpleshoppermission;Username=postgres;Password=Aa123456..;Ssl Mode=Disable;","d":"权限中心独立库"},
  {"g":"Basic","k":"redisConnectionString","v":"localhost:6379,password=Aa123456..,defaultDatabase=5","d":"权限引导分布式锁 Redis"},
  {"g":"Basic","k":"redisDb","v":"5","d":"Redis库"},
  {"g":"Basic:Consul","k":"Enabled","v":"true","d":"启用服务注册"},
  {"g":"Basic:Consul","k":"ServiceName","v":"PermissionService","d":"服务发现名称"},
  {"g":"Basic:Consul","k":"ServiceAddress","v":"172.18.0.1","d":"Docker 网关地址，容器可访问宿主机服务"},
  {"g":"Basic:Consul","k":"ServicePort","v":"5022","d":"HTTP 健康检查与路由端口"},
  {"g":"Basic:Consul","k":"MetaData:GrpcPort","v":"5023","d":"Consul 服务间 gRPC 发现元数据"},
  {"g":"Basic:Consul","k":"HealthCheckEndpoint","v":"/health","d":"健康检查端点"},
  {"g":"Basic:port","k":"httpport","v":"5022","d":"REST 端口"},
  {"g":"Basic:port","k":"grpcport","v":"5023","d":"独立 gRPC 端口，明文 HTTP/2"}
]$$);

SELECT pg_temp.seed_agile_config('MarketingService', $$
[
  {"g":"Basic","k":"sqlConnectionString","v":"Host=127.0.0.1;Port=5432;Database=simpleshopmarketing;Username=postgres;Password=Aa123456..;Ssl Mode=Disable;","d":"营销库（活动/券/使用记录）"},
  {"g":"Basic","k":"redisDb","v":"6","d":"Redis库"},
  {"g":"Basic:port","k":"httpport","v":"5072","d":"REST 端口"},
  {"g":"Basic:port","k":"grpcport","v":"5073","d":"独立 gRPC 端口，明文 HTTP/2"},
  {"g":"Basic:Consul","k":"Enabled","v":"true","d":"启用服务注册"},
  {"g":"Basic:Consul","k":"ServiceName","v":"MarketingService","d":"服务发现名称"},
  {"g":"Basic:Consul","k":"ServiceAddress","v":"172.18.0.1","d":"注册地址"},
  {"g":"Basic:Consul","k":"ServicePort","v":"5072","d":"HTTP 健康检查与路由端口"},
  {"g":"Basic:Consul","k":"MetaData:GrpcPort","v":"5073","d":"Consul 服务间 gRPC 发现元数据"},
  {"g":"Basic:Consul","k":"HealthCheckEndpoint","v":"/health","d":"健康检查端点"},
  {"g":"Basic","k":"redisConnectionString","v":"localhost:6379,password=Aa123456..,defaultDatabase=6","d":"Redis 连接"},
  {"g":"RabbitMQ","k":"HostName","v":"localhost","d":"消息队列"},
  {"g":"RabbitMQ","k":"UserName","v":"admin","d":"消息队列账号"},
  {"g":"RabbitMQ","k":"Password","v":"admin123","d":"消息队列密码"},
  {"g":"RabbitMQ","k":"Exchange","v":"simpleshop.events","d":"事件交换机"}
]$$);

DROP FUNCTION pg_temp.seed_agile_config(text, jsonb, boolean);
