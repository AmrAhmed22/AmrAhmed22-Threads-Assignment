using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase1
{
    public sealed class SyncCouponRepository
    {
        readonly JsonFileStore _store;

        public SyncCouponRepository(JsonFileStore store) => _store = store;

        public Coupon? GetByCode(string code)
        {
            Log.Info("SyncCouponRepository -> Opening connection...");
            Thread.Sleep(200);

            Log.Info($"SyncCouponRepository -> Executing query for Coupon '{code}'...");
            Thread.Sleep(Random.Shared.Next(300, 601));

            Coupon? coupon = _store.ReadAll<Coupon>(DataFiles.Coupons).FirstOrDefault(c =>
                string.Equals(c.Code, code, StringComparison.OrdinalIgnoreCase));

            Log.Info(coupon is null
                ? $"SyncCouponRepository -> Coupon '{code}' not found"
                : $"SyncCouponRepository -> Coupon '{coupon.Code}' found ({coupon.DiscountPercent}%)");

            return coupon;
        }
    }
}
