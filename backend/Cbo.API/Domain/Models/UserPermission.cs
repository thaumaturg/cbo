using Cbo.API.Domain.Constants;

namespace Cbo.API.Domain.Models;

public class UserPermission
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; }
    public UserPermissionName PermissionName { get; set; }
} 
