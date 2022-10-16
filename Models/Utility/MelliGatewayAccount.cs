using ArmisApp.Models.Domain.context;
using Microsoft.EntityFrameworkCore;
using Parbad.Gateway.Melli;
using Parbad.GatewayBuilders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArmisApp.Models.Utility
{
    public class MelliAccountSource : IGatewayAccountSource<MelliGatewayAccount>
    {
        private readonly DataContext db = null;

        public MelliAccountSource(DataContext _context)
        {
            db = _context;
        }

        public async Task AddAccountsAsync(IGatewayAccountCollection<MelliGatewayAccount> accounts)
        {
            var settings = await db.TblPaymentGateway.Where(a=>a.ID==5).SingleOrDefaultAsync();

            accounts.Add(new MelliGatewayAccount
            {
                TerminalId = settings.TerminalID.ToString(),
                TerminalKey = settings.Password,
                MerchantId= settings.UserName
            });
        }
    }
}
