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
    public class PayRepository : IDisposable
    {
        private DataContext db = null;

        public PayRepository()
        {
            db = new DataContext();
        }
        public int GetPackagePrice(int Pkg, TblOnlineCoursePrice t)
        {
            int Price = 0;
            switch (Pkg)
            {
                case 1:
                    Price = t.Price;
                    break;
                case 2:
                    Price = t.Price * 2;
                    break;
                case 4:
                    Price = t.Price * 4;
                    break;
                case 8:
                    Price = t.Price * 8;
                    break;
                case 12:
                    Price = t.Price * 12;
                    break;
            }
            return Price;
        }
        public int GetPackageDiscountPrice(int Pkg, TblOnlineCoursePrice t)
        {
            int Price = 0; switch (Pkg)
            {
                case 1:
                    Price = t.PkgDiscount1;
                    break;
                case 2:
                    Price = t.PkgDiscount2;
                    break;
                case 4:
                    Price = t.PkgDiscount3;
                    break;
                case 8:
                    Price = t.PkgDiscount4;
                    break;
                case 12:
                    Price = t.PkgDiscount5;
                    break;
            }
            return Price;
        }
        public int GetPackageFinalPrice(int Pkg, TblOnlineCoursePrice t)
        {
            int Price = 0;
            switch (Pkg)
            {
                case 1:
                    Price = t.Price - t.PkgDiscount1;
                    break;
                case 2:
                    Price = (t.Price * 2) - t.PkgDiscount2;
                    break;
                case 4:
                    Price = (t.Price * 4) - t.PkgDiscount3;
                    break;
                case 8:
                    Price = (t.Price * 8) - t.PkgDiscount4;
                    break;
                case 12:
                    Price = (t.Price * 12) - t.PkgDiscount5;
                    break;
            }
            return Price;
        }
        public int GetProfitPackagePrice(int Pkg,int TotalPrice, TblOnlineCoursePrice t)
        {
            int Price = 0;
            switch (Pkg)
            {
                case 1:
                    Price = t.Price - t.PkgDiscount1;
                    break;
                case 2:
                    Price = (t.Price * 2) - t.PkgDiscount2;
                    break;
                case 4:
                    Price = (t.Price * 4) - t.PkgDiscount3;
                    break;
                case 8:
                    Price = (t.Price * 8) - t.PkgDiscount4;
                    break;
                case 12:
                    Price = (t.Price * 12) - t.PkgDiscount5;
                    break;
            }
            Price = TotalPrice - Price;
            return Price;
        }
        public void GetPackageViewPrice(TblOnlineCoursePrice t, VmOnlineCourse vm)
        {
            if (t != null)
            {
                vm.PackagesPrice = new VmCoursePackagesPrice();
                vm.PackagesPrice.PkgPrice1 = t.Price;
                vm.PackagesPrice.PkgPrice2 = t.Price * 2;
                vm.PackagesPrice.PkgPrice3 = t.Price * 4;
                vm.PackagesPrice.PkgPrice4 = t.Price * 8;
                vm.PackagesPrice.PkgPrice5 = t.Price * 12;
                vm.PackagesPrice.PkgDiscount1 = vm.PackagesPrice.PkgPrice1 - t.PkgDiscount1;
                vm.PackagesPrice.PkgDiscount2 = vm.PackagesPrice.PkgPrice2 - t.PkgDiscount2;
                vm.PackagesPrice.PkgDiscount3 = vm.PackagesPrice.PkgPrice3 - t.PkgDiscount3;
                vm.PackagesPrice.PkgDiscount4 = vm.PackagesPrice.PkgPrice4 - t.PkgDiscount4;
                vm.PackagesPrice.PkgDiscount5 = vm.PackagesPrice.PkgPrice5 - t.PkgDiscount5;
            }
        }
        public string GetPackageName(int Pkg)
        {
            string Name = "";
            switch (Pkg)
            {
                case 1:
                    Name = "1 جلسه - 30 دقیقه";
                    break;
                case 2:
                    Name = "2 جلسه - 60 دقیقه";
                    break;
                case 4:
                    Name = "4 جلسه - 120 دقیقه";
                    break;
                case 8:
                    Name = "8 جلسه - 240 دقیقه";
                    break;
                case 12:
                    Name = "12 جلسه - 360 دقیقه";
                    break;
            }
            return Name;
        }
        public List<VmTeacherInvoice> GetPaymentReceipt(List<VmTeacherInvoice> LstVm, int FinalPrice, int packageNum, int tPercent, TblUserOnlineCourse qUserClass, List<TblUserEvents> qUserEvents)
        {
            int remainingEvents = packageNum - qUserEvents.Count();
            foreach (var item in qUserEvents)
            {
                VmTeacherInvoice vm = new VmTeacherInvoice();
                vm.ID = item.ID;
                vm.StartDate = item.StartDate.ToShamsi().ToString("yyyy/MM/dd - HH:mm");
                vm.BasePrice = FinalPrice / packageNum;
                //int tPrice = ((vm.TotalPrice * item.TblOnlineCourse.TeacherPercent) / 100);
                //if (tPercent > 0)
                //{
                //    vm.WagePrice = vm.BasePrice - ((vm.BasePrice * tPercent) / 100);
                //}
                vm.WagePrice = vm.BasePrice - ((vm.BasePrice * tPercent) / 100);
                vm.Status = item.Status;

                if (item.TblUserEventsInvoice != null)
                {
                    vm.Date = item.TblUserEventsInvoice.Date.ToShamsi().ToString("yyyy/MM/dd - HH:mm");
                    vm.InvoiceID = item.TblUserEventsInvoice.ID;
                    // مبلغ جریمه
                    vm.AmountOfFines = item.TblUserEventsInvoice.AmountOfFines; /* - ((item.TblUserEventsInvoice.AmountOfFines * 80) / 100);*/
                    // مبلغ اضافه برای استاد
                    vm.AmountOfAddition = item.TblUserEventsInvoice.AmountOfAddition;
                    //مبلغ سود سایت بابت لغو جلسه توسط هنرجو
                    vm.SiteProfitOfAdditional = item.TblUserEventsInvoice.ProfitOfAddition;
                }
                else
                {
                    vm.Date = "ثبت نشده";
                }
                //در صورتی که جلسه و یا کلاس توسط هنرجو لغو شده باشد
                if (item.Status == 3 || item.Status == 4)
                {
                    vm.WagePrice = 0;
                    //vm.AmountOfFines = 0;
                    vm.SiteProfit = item.TblUserEventsInvoice.ProfitOfAddition;
                    vm.AmountOfAddition = item.TblUserEventsInvoice.AmountOfAddition;
                    vm.TeacherPrice = vm.AmountOfAddition;
                }
                else
                {
                    vm.TeacherPrice = (vm.BasePrice - vm.AmountOfFines - vm.WagePrice + vm.AmountOfAddition);

                }
                // در صورتی که کلاس توسط هنرجو لغو شده باشد
                //if (qUserClass.Status == 3)
                //{
                //    vm.AmountOfFines = 0;
                //    var checkTeacherCanceled = qUserEvents.Where(a => a.Status == 2).ToList();
                //    // اگر استاد جلسه لغو شده داشته باشد
                //    if (checkTeacherCanceled.Count > 0)
                //    {
                //        // اگر کلاس لغو شود مبلغ جریمه لحاظ نمی شود اما 10 درصد سود به سایت می رسد
                //        vm.AmountOfFines = 0;
                //        vm.SiteProfit = qUserClass.CancelAmount;
                //    }
                //    else
                //    {
                //        // مبلغ جریمه بابت لغو کلاس برای استاد
                //        int teacherPercentAmount = (qUserClass.CancelAmount * 80) / 100;
                //        vm.AmountOfAddition += teacherPercentAmount;

                //        // مبلغ جریمه بابت لغو کلاس برای سایت
                //        vm.SiteProfit = qUserClass.CancelAmount - teacherPercentAmount;
                //    }

                //}

                // مبلغ سود کل برای سایت
                vm.SiteTotalProfit = vm.WagePrice + vm.AmountOfFines + vm.SiteProfit + vm.SiteProfitOfAdditional;
                LstVm.Add(vm);
            }
            // جلسه های برگزار نشده
            if(remainingEvents > 0)
            {
                for (int i = 0; i < remainingEvents; i++)
                {
                    VmTeacherInvoice vm = new VmTeacherInvoice();
                    vm.ID = i * 2;
                    vm.StartDate = "تاریخ رزرو تعیین نشده";
                    vm.BasePrice = FinalPrice / packageNum;
                    //int tPrice = ((vm.TotalPrice * item.TblOnlineCourse.TeacherPercent) / 100);
                    vm.Status = qUserClass.Status==3?4:0;
                    vm.Date = "ثبت نشده";
                    if(qUserClass.Status != 3)
                    {
                        // کارمزد سایت
                        vm.WagePrice = vm.BasePrice - ((vm.BasePrice * tPercent) / 100);
                        // مبلغ خالص استاد
                        vm.TeacherPrice = (vm.BasePrice + vm.AmountOfAddition) - vm.AmountOfFines - vm.WagePrice;
                        // مبلغ سود کل برای سایت
                        vm.SiteTotalProfit = vm.WagePrice + vm.AmountOfFines + vm.SiteProfit + vm.SiteProfitOfAdditional;
                    }
                    
                    LstVm.Add(vm);
                }
            }
            return LstVm;
        }

        public async Task DepositToTeacher(DataContext _context, UserManager<ApplicationUser> _userManager, string TeacherID,string Text, int finalPrice,int RefrenceID) 
        {
            // واریز  به حساب استاد
            var qTeacher = await _userManager.FindByIdAsync(TeacherID);
            qTeacher.Inventory += finalPrice;
            await _userManager.UpdateAsync(qTeacher);

            TblTransaction tr = new TblTransaction
            {
                Amount = finalPrice,
                SaleRefrenceID = RefrenceID,
                Status = 2,
                UserID = TeacherID,
                ToUserID = TeacherID,
                Type = 3,
                NewInventory = qTeacher.Inventory,
                Date = DateTime.Now,
                Description = Text
            };
            await _context.AddAsync(tr);
        }
        public async Task DepositToStudent(DataContext _context, UserManager<ApplicationUser> _userManager, string UserID, string Text, int finalPrice, int RefrenceID)
        {
            var qUser = await _userManager.FindByIdAsync(UserID);
            qUser.Inventory += finalPrice;
            await _userManager.UpdateAsync(qUser);

            TblTransaction te = new TblTransaction
            {
                Amount = finalPrice,
                SaleRefrenceID = RefrenceID,
                Status = 2,
                UserID = UserID,
                ToUserID = UserID,
                Type = 3,
                NewInventory = qUser.Inventory,
                Date = DateTime.Now,
                Description = Text
            };

            await _context.AddAsync(te);

        }
        public async Task CheckAndPayFinishedSe(UserManager<ApplicationUser> _userManager)
        {
            var qUserEvents = await db.TblUserEvents.Where(a => a.Status == 0)
                .Include(a=>a.TblOnlineCourse).Include(a=>a.TblUserEventsInvoice).ToListAsync();
            if (qUserEvents.Count() > 0)
            {
                var qUserClass = db.TblUserOnlineCourse.AsQueryable();
                bool update = false;
                int RefrenceID = 0;
                foreach (var item in qUserEvents)
                {
                    // اتمام خودکار جلسه بعد از گذشت 15 دقیقه از اتمام زمان
                    DateTime end15Minutes = item.EndDate.AddMinutes(15);
                    if (DateTime.Now > end15Minutes && item.Status == 0)
                    {
                        item.Status = 1;
                        update = true;
                        db.Update(item);

                        var qBookingInvoice = db.TblBookingInvoice.Where(a => a.ID == item.InvoiceID).SingleOrDefault();

                        var UserClass = qUserClass.Where(a => a.InvoiceID == item.InvoiceID).SingleOrDefault();

                        int totalPrice = qBookingInvoice.Amount;
                        int tPercent = qBookingInvoice.TeacherPercent > 0 ? qBookingInvoice.TeacherPercent : item.TblOnlineCourse.TeacherPercent;
                        int basePrice = totalPrice / UserClass.Package;

                        // درصد استاد
                        int teacherAmount = ((basePrice * tPercent) / 100);

                        if (item.TblUserEventsInvoice == null)
                        {
                            TblUserEventsInvoice t = new TblUserEventsInvoice();
                            t.Date = DateTime.Now;
                            // سود سایت
                            t.ProfitOfAddition = basePrice - teacherAmount;
                            // سود استاد
                            t.AmountOfAddition = teacherAmount;

                            t.Description = " جلسه در تاریخ" + t.Date.ToShamsi().ToString("yyyy/MM/dd - HH:mm") + " برگزار شد و " + teacherAmount +
                                " درصد بابت حق الزحمه به حساب شما منتقل شد";
                            t.TeacherID = item.TblOnlineCourse.TeacherID;
                            t.UserEventID = item.ID;
                            db.Add(t);
                            await db.SaveChangesAsync();

                            RefrenceID = t.ID;
                        }
                        else
                        {
                            RefrenceID = item.TblUserEventsInvoice.ID;
                        }

                        await DepositToTeacher(db, _userManager, item.TblOnlineCourse.TeacherID,
                            "پرداخت وجه بابت اتمام جلسه " + "|" + item.TblOnlineCourse.Title, teacherAmount, RefrenceID);
                    }
                }
                if (update)
                {
                    await db.SaveChangesAsync();
                }
            }
            
        }
        ~PayRepository()
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
