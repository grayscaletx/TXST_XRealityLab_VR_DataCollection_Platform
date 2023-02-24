using XRealityLab.Common;
using Infrastructure.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XRealityLab.Users.Queries
{
    public class UserRepository : IUserRepository
    {
        private readonly XRealityLabdbContext _context;
        public UserRepository(XRealityLabdbContext context)
        {
            _context = context;
        }
        public User GetAdministrator(int Id)
        {
            return _context.User.FirstOrDefault(k => k.UserId == Id && k.RoleId == (int)RoleType.Administrator);
        }
    }
}
