using ArmisApp.Models.Domain.context;
using ArmisApp.Models.Domain.db;
using ArmisApp.Models.ExMethod;
using ArmisApp.Models.Identity;
using ArmisApp.Models.Utility;
using ArmisApp.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArmisApp.Models.Repository
{
    public class ToolsRepository : IDisposable
    {
        private DataContext db = null;

        public ToolsRepository()
        {
            db = new DataContext();
        }
        public int UserCount()
        {
            var q = db.Users.ToList().Count();
            return q;
        }
        public int GetUsersCount()
        {
            int count = db.Users.Count();
            return count;
        }
        public int GetCourseCount()
        {
            int count = db.TblCourse.Count();
            return count;
        }
        public int GetSeasionCount()
        {
            int count = db.TblSession.Count();
            return count;
        }
        public int GetBlogCount()
        {
            int count = db.TblNews.Count();
            return count;
        }
        public TblSettings Settings()
        {
            var q = db.TblSettings.FirstOrDefault();
            return q;
        }
        public String GetTodayDate()
        {
            TimeUtility time = new TimeUtility();
            string date = time.GetDateName(DateTime.Now);
            return date;
        }
        public List<TblNews> getLstNews()
        {
            var q = db.TblNews
                .Where(a=>a.ShortLink!=null)
                .Where(a=>a.Status==true).OrderByDescending(a => a.Date).Take(12).ToList();
            return q;
        }
        ~ToolsRepository()
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
