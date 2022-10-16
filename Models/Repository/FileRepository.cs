using ArmisApp.Models.Domain.context;
using ArmisApp.Models.Domain.db;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArmisApp.Models.Repository
{
    public class FileRepository : IDisposable
    {
        private DataContext db = null;
        public FileRepository()
        {
            db = new DataContext();
        }

        public TblImage GetImageByID(int? id)
        {
            var qImg = db.TblImage.Where(a => a.ID == id)
                 .Include(a => a.TblServer)
                 .SingleOrDefault();

            return qImg ?? null;
        }
        public TblVideo GetVideoByID(int? id)
        {
            var qVideo = db.TblVideo.Where(a => a.ID == id)
                 .Include(a => a.TblServer)
                 .SingleOrDefault();

            return qVideo ?? null;
        }
        public TblVideo GetVideoByToken(int ID,string Token)
        {
            var qVideo = db.TblVideo.Where(a=>a.ID==ID)
                .Where(a => a.Token == Token)
                 .Include(a => a.TblServer)
                 .SingleOrDefault();

            return qVideo ?? null;
        }
        public TblFiles GetFileByID(int? id)
        {
            var qFile = db.TblFiles.Where(a => a.ID == id)
                 .Include(a => a.TblServer)
                 .SingleOrDefault();

            return qFile ?? null;
        }
        public TblFiles GetFileByToken(int ID, string Token)
        {
            var qFile = db.TblFiles.Where(a => a.ID == ID)
                .Where(a => a.Token == Token)
                 .Include(a => a.TblServer)
                 .SingleOrDefault();

            return qFile ?? null;
        }
        ~FileRepository()
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
