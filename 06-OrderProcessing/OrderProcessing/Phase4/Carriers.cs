using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase4
{
    public sealed class FastShip : ShippingCarrierBase
    {
        public FastShip(JsonFileStore store, IEnumerable<int>? failForOrderIds = null)
            : base("FastShip", 500, 1500, 1.00m, store, failForOrderIds) { }
    }

    public sealed class CairoExpress : ShippingCarrierBase
    {
        public CairoExpress(JsonFileStore store, IEnumerable<int>? failForOrderIds = null)
            : base("CairoExpress", 300, 2000, 0.95m, store, failForOrderIds) { }

        protected override bool ShouldFail(Order order) => order.Id % 2 == 0 || base.ShouldFail(order);
    }

    public sealed class NileCourier : ShippingCarrierBase
    {
        public NileCourier(JsonFileStore store, IEnumerable<int>? failForOrderIds = null)
            : base("NileCourier", 800, 1200, 1.10m, store, failForOrderIds) { }
    }
}
