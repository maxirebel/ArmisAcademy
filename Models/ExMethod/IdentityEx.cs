using ArmisApp.Models.Domain.context;
using ArmisApp.Models.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

public static class IdentityEx
{
    public static string GetUserID(this ClaimsPrincipal Claim)
    {
        if (Claim == null)
        {
            throw new ArgumentNullException("Argument Null Exception");
        }

        return Claim.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
    public static ApplicationUser GetUserDetails(this ClaimsPrincipal Claim)
    {
        if (Claim == null)
        {
            throw new ArgumentNullException("Argument Null Exception");
        }

        using (DataContext db = new DataContext())
        {
            string UserID = Claim.GetUserID();
            var q = (db.Users.Where(a => a.Id == UserID))
                .Include(a => a.TblImage)
                .ThenInclude(a => a.TblServer)
                .SingleOrDefault();
            return q;
        }
    }
    public static string GetRole(this ClaimsPrincipal Claim)
    {
        string UserID = Claim.GetUserID();
        string RoleName = "";
        using (DataContext db = new DataContext())
        {
            var userRole = (db.UserRoles.Where(a => a.UserId == UserID))
                .FirstOrDefault().RoleId;
            var role = db.Roles.Where(a => a.Id == userRole).SingleOrDefault();

            RoleName = role.Name;
            if (role.Name == "Employee")
            {
                RoleName = "Admin/Employee";
            }
            if (role.Name == "Student")
            {
                RoleName = "Profile";
            }
            if (role.Name == "Teacher")
            {
                RoleName = "Teacher";
            }
            if (role.Name == "Admin")
            {
                RoleName = "Admin";
            }
            return RoleName;
        }
    }
}