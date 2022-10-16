using ArmisApp.Models.Domain.context;
using Microsoft.EntityFrameworkCore;
using Parbad.Gateway.Mellat;
using Parbad.GatewayBuilders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArmisApp.Models.Utility
{
    public class MellatAccountSource : IGatewayAccountSource<MellatGatewayAccount>
    {
        private readonly DataContext db = null;

        public MellatAccountSource(DataContext _context)
        {
            db = _context;
        }

        public async Task AddAccountsAsync(IGatewayAccountCollection<MellatGatewayAccount> accounts)
        {
            var settings = await db.TblPaymentGateway.Where(a=>a.ID==1).SingleOrDefaultAsync();

            accounts.Add(new MellatGatewayAccount
            {
                TerminalId=settings.TerminalID,
                UserName=settings.UserName,
                UserPassword=settings.Password
            });
        }
    }
}
