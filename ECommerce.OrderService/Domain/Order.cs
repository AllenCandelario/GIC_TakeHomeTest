using System.Security.Principal;
using System.Xml.Linq;

namespace ECommerce.OrderService.Domain
{
    public sealed class Order
    {
        public Guid Id { get; set; }
        public Guid UserId {  get; set; }
        public string Product { get; set; }
        public int Quantity {  get; set; }
        public decimal Price { get; set; }
        public DateTime CreatedAtUtc {  get; set; }

        public Order(Guid userId, string product, int quantity, decimal price)
        {
            // userId validation
            if (userId == Guid.Empty)
            {
                throw new ArgumentException("UserId is required", nameof(userId));
            }

            // name validation
            if (string.IsNullOrWhiteSpace(product))
            {
                throw new ArgumentException("Product is required", nameof(product));
            }

            // quantity validation
            if (quantity <= 0)
            {
                throw new ArgumentException("Quantity cannot be 0 or less", nameof(quantity));
            }

            // decimal validation. enfore 2 dp
            if (price <= 0m)
            {
                throw new ArgumentException("Price cannot be 0 or less", nameof(price));
            }
            if (decimal.Round(price, 2) != price)
            {
                throw new ArgumentException("Price must have at most 2 decimal places", nameof(price));
            }

            Id = Guid.NewGuid();
            UserId = userId;
            Product = product.Trim();
            Quantity = quantity;
            Price = price;
            CreatedAtUtc = DateTime.UtcNow;
        }
    }
}