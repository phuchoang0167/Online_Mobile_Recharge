using Online_Mobile_Recharge.Models.Entities;
using Online_Mobile_Recharge.Models.Enums;

namespace Online_Mobile_Recharge.Services
{
    public static class ProductSaleCalculator
    {
        public static bool IsActive(ProductSale? sale, DateTime now)
        {
            if (sale == null || sale.SaleType == ProductSaleType.None || sale.SaleValue <= 0)
            {
                return false;
            }

            if (sale.StartAt is DateTime start && now < start)
            {
                return false;
            }

            if (sale.EndAt is DateTime end && now > end)
            {
                return false;
            }

            return true;
        }

        public static decimal GetEffectivePrice(decimal originalPrice, ProductSale? sale)
        {
            if (sale == null || sale.SaleType == ProductSaleType.None || sale.SaleValue <= 0)
            {
                return originalPrice;
            }

            var computed = originalPrice;
            switch (sale.SaleType)
            {
                case ProductSaleType.Percent:
                    {
                        var percent = sale.SaleValue;
                        if (percent <= 0 || percent > 100)
                        {
                            return originalPrice;
                        }

                        computed = originalPrice * (1 - (percent / 100m));
                        break;
                    }
                case ProductSaleType.FixedAmount:
                    computed = originalPrice - sale.SaleValue;
                    break;
            }

            if (computed < 0)
            {
                computed = 0;
            }

            return decimal.Round(computed, 0, MidpointRounding.AwayFromZero);
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

