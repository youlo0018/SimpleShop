export const hasPermission = (required) => {
  const user = JSON.parse(localStorage.getItem('admin_user') || 'null')
  const permissions = user?.permissions || []
  return permissions.includes('*') || permissions.includes(required)
}
