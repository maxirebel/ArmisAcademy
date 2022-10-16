using ArmisApp.Models.Domain.context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArmisApp.Models.Attribute
{
    public class CheckUserAccess : IDisposable
    {
        private DataContext _context = null;

        public CheckUserAccess()
        {
            _context = new DataContext();
        }
        public bool CheckAccess(string UserID ,string AccessName)
        {
            var qAccess = _context.TblUserAccess
                .Where(a => a.UserID == UserID)
                .Where(a=>a.AccessName== AccessName)
                .SingleOrDefault();

            if (qAccess != null)
            {
                return true;
            }
            return false;
        }
        ~CheckUserAccess()
        {
            Dispose(true);
        }

        public void Dispose()
        {

        }

        public void Dispose(bool Dis)
        {
            if (Dis)
            {
                Dispose();
            }
        }
    }
}
