using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase1
{
    public sealed class CouponRepository
    {
        readonly JsonFileStore _store;

        public CouponRepository(JsonFileStore store) => _store = store;

        public async Task<Coupon?> GetByCodeAsync(string code, CancellationToken ct)
        {
            Log.Info("CouponRepository -> Opening connection...");
            await Task.Delay(200, ct);

            Log.Info($"CouponRepository -> Executing query for Coupon '{code}'...");
            await Task.Delay(Random.Shared.Next(300, 601), ct);

            List<Coupon> all = await _store.ReadAllAsync<Coupon>(DataFiles.Coupons, ct);
            Coupon? coupon = all.FirstOrDefault(c =>
                string.Equals(c.Code, code, StringComparison.OrdinalIgnoreCase));

            Log.Info(coupon is null
                ? $"CouponRepository -> Coupon '{code}' not found"
                : $"CouponRepository -> Coupon '{coupon.Code}' found ({coupon.DiscountPercent}%)");

            return coupon;
        }
    }
}
