using OrderProcessing.Shared.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Shared.Storage
{

    public static class SeedData
    {
        public static List<Customer> Customers() =>
        [
        new() { Id = 1,  Name = "Ahmed Ali",       Email = "ahmed.ali@example.com",       IsActive = true,  LoyaltyTier = LoyaltyTier.Gold   },
        new() { Id = 2,  Name = "Sara Mohamed",    Email = "sara.mohamed@example.com",    IsActive = true,  LoyaltyTier = LoyaltyTier.Silver },
        new() { Id = 3,  Name = "Omar Hassan",     Email = "omar.hassan@example.com",     IsActive = true,  LoyaltyTier = LoyaltyTier.Bronze },
        new() { Id = 4,  Name = "Mona Ibrahim",    Email = "mona.ibrahim@example.com",    IsActive = false, LoyaltyTier = LoyaltyTier.Silver },
        new() { Id = 5,  Name = "Youssef Adel",    Email = "youssef.adel@example.com",    IsActive = true,  LoyaltyTier = LoyaltyTier.Gold   },
        new() { Id = 6,  Name = "Nour Khaled",     Email = "nour.khaled@example.com",     IsActive = true,  LoyaltyTier = LoyaltyTier.Bronze },
        new() { Id = 7,  Name = "Karim Samir",     Email = "karim.samir@example.com",     IsActive = true,  LoyaltyTier = LoyaltyTier.Silver },
        new() { Id = 8,  Name = "Laila Mahmoud",   Email = "laila.mahmoud@example.com",   IsActive = true,  LoyaltyTier = LoyaltyTier.Gold   },
        new() { Id = 9,  Name = "Hassan Tarek",    Email = "hassan.tarek@example.com",    IsActive = true,  LoyaltyTier = LoyaltyTier.Bronze },
        new() { Id = 10, Name = "Dina Fathy",      Email = "dina.fathy@example.com",      IsActive = true,  LoyaltyTier = LoyaltyTier.Silver },
    ];

        public static List<Product> Products() =>
        [
        new() { Id = 1, Name = "Laptop",              Price = 1200.00m, StockQuantity = 10, WeightKg = 2.2m },
        new() { Id = 2, Name = "Smartphone",          Price = 800.00m,  StockQuantity = 15, WeightKg = 0.4m },
        new() { Id = 3, Name = "Wireless Mouse",      Price = 25.00m,   StockQuantity = 2,  WeightKg = 0.1m },
        new() { Id = 4, Name = "Mechanical Keyboard", Price = 90.00m,   StockQuantity = 1,  WeightKg = 0.9m },
        new() { Id = 5, Name = "USB-C Hub",           Price = 45.00m,   StockQuantity = 30, WeightKg = 0.2m },
        new() { Id = 6, Name = "Headphones",          Price = 150.00m,  StockQuantity = 12, WeightKg = 0.3m },
        new() { Id = 7, Name = "27\" Monitor",        Price = 350.00m,  StockQuantity = 6,  WeightKg = 5.0m },
        new() { Id = 8, Name = "Webcam",              Price = 70.00m,   StockQuantity = 20, WeightKg = 0.2m },
    ];

        public static List<Coupon> Coupons() =>
        [
        new() { Code = "SAVE10",    DiscountPercent = 10, ExpiryDate = DateTime.Today.AddDays(180) },
        new() { Code = "WELCOME15", DiscountPercent = 15, ExpiryDate = DateTime.Today.AddDays(90)  },
        new() { Code = "FLASH20",   DiscountPercent = 20, ExpiryDate = DateTime.Today.AddDays(7)   },
        new() { Code = "OLD20",     DiscountPercent = 20, ExpiryDate = DateTime.Today.AddDays(-30) },
    ];

        public static List<Order> IncomingOrders()
        {
            DateTime now = DateTime.Now;

            Order MakeOrder(int id, int customerId, string? coupon, params (int productId, int qty)[] items) => new()
            {
                Id = id,
                CustomerId = customerId,
                CouponCode = coupon,
                Items = items.Select(i => new OrderItem { ProductId = i.productId, Quantity = i.qty, UnitPrice = 0m }).ToList(),
                Status = OrderStatus.Pending,
                CreatedAt = now,
                FailureReason = null
            };

            return
            [
            MakeOrder(1,  1,  "SAVE10",    (1, 1), (3, 1)),
            MakeOrder(2,  2,  null,        (2, 1)),
            MakeOrder(3,  3,  "WELCOME15", (5, 2), (8, 1)),
            MakeOrder(4,  4,  null,        (6, 1)),
            MakeOrder(5,  5,  null,        (7, 1)),
            MakeOrder(6,  6,  "FLASH20",   (5, 1)),
            MakeOrder(7,  7,  null,        (6, 1), (8, 1)),
            MakeOrder(8,  8,  "SAVE10",    (1, 1)),
            MakeOrder(9,  9,  null,        (3, 5)),
            MakeOrder(10, 10, null,        (4, 1)),
            MakeOrder(11, 1,  null,        (4, 1)),
            MakeOrder(12, 2,  "OLD20",     (2, 1)),
            MakeOrder(13, 3,  null,        (6, 2)),
            MakeOrder(14, 5,  "WELCOME15", (8, 2), (5, 1)),
            MakeOrder(15, 99, null,        (2, 1)),
            MakeOrder(16, 6,  null,        (7, 1), (5, 1)),
            MakeOrder(17, 7,  "SAVE10",    (2, 1)),
            MakeOrder(18, 8,  "FAKE50",    (6, 1)),
            MakeOrder(19, 9,  null,        (8, 1)),
            MakeOrder(20, 10, "FLASH20",   (1, 1), (6, 1)),
        ];
        }
    }
}