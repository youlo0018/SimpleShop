/**
 * 客户注册来源（与后端 CustomerService.Domain.Enums.RegisterSource 对齐）：
 * 用于渠道分析与风控，新增渠道需前后端同步。
 */
export const RegisterSource = {
  MiniApp: 1,
  Web: 2,
  Admin: 3
}
