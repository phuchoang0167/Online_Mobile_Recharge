using Online_Mobile_Recharge.Models.Entities;
using Online_Mobile_Recharge.Models.Enums;

namespace Online_Mobile_Recharge.Services
{
    public static class ProductSaleCalculator
    {
        private static bool IsValidSaleData(ProductSale? sale, out ProductSale validSale)
        {
            if (sale == null || sale.SaleType == ProductSaleType.None || sale.SaleValue <= 0)
            {
                validSale = null!;
                return false;
            }

            validSale = sale;
            return true;
        }

        public static bool IsActive(ProductSale? sale, DateTime now)
        {
            if (!IsValidSaleData(sale, out var validSale))
            {
                return false;
            }

            if (validSale.StartAt is DateTime start && now < start)
            {
                return false;
            }

            if (validSale.EndAt is DateTime end && now > end)
            {
                return false;
            }

            return true;
        }

        public static decimal GetEffectivePrice(decimal originalPrice, ProductSale? sale, DateTime now)
        {
            if (!IsActive(sale, now))
            {
                return originalPrice;
            }

            return GetEffectivePrice(originalPrice, sale);
        }

        public static decimal GetEffectivePrice(decimal originalPrice, ProductSale? sale)
        {
            if (!IsValidSaleData(sale, out var validSale))
            {
                return originalPrice;
            }

            var computed = originalPrice;
            switch (validSale.SaleType)
            {
                case ProductSaleType.Percent:
                    {
                        var percent = validSale.SaleValue;
                        if (percent <= 0 || percent > 100)
                        {
                            return originalPrice;
                        }

                        computed = originalPrice * (1 - (percent / 100m));
                        break;
                    }
                case ProductSaleType.FixedAmount:
                    computed = originalPrice - validSale.SaleValue;
                    break;
            }

            if (computed < 0)
            {
                computed = 0;
            }

            return decimal.Round(computed, 0, MidpointRounding.AwayFromZero);
        }

        public static string? GetBadgeText(ProductSale? sale, DateTime now)
        {
            if (!IsActive(sale, now))
            {
                return null;
            }

            return GetBadgeText(sale);
        }

        public static string? GetBadgeText(ProductSale? sale)
        {
            if (sale == null)
            {
                return null;
            }

            return sale.SaleType switch
            {
                ProductSaleType.Percent => $"-{decimal.Round(sale.SaleValue, 0, MidpointRounding.AwayFromZero):N0}%",
                ProductSaleType.FixedAmount => $"-{decimal.Round(sale.SaleValue, 0, MidpointRounding.AwayFromZero):N0} VND",
                _ => "Sale"
            };
        }
    }
}
