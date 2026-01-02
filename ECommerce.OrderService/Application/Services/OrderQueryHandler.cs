using ECommerce.OrderService.Application.Interfaces;
using ECommerce.OrderService.Domain;
using ECommerce.Shared.Exceptions;
using ECommerce.Shared.Kafka.Contracts;
using ECommerce.Shared.Kafka.Interfaces;

namespace ECommerce.OrderService.Application.Services
{
    public sealed class OrderQueryHandler : IOrderQueryHandler
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IKafkaProducer _kafkaProducer;
        private readonly string _orderCreatedTopic;

        public OrderQueryHandler(IOrderRepository orderRepository, IKafkaProducer kafkaProducer, IConfiguration config)
        {
            _orderRepository = orderRepository;
            _kafkaProducer = kafkaProducer;
            _orderCreatedTopic = config["Kafka:Topics:OrderCreatedTopic"] ?? throw new InvalidOperationException("Kafka:Topics:OrderCreatedTopic missing");

        }

        public async Task<Order> AddOrderAsync(Guid userId, string product, int quantity, decimal price, CancellationToken ct)
        {
            // service validation??

            var order = new Order(userId, product, quantity, price);
            await _orderRepository.AddAsync(order, ct);
            await _orderRepository.SaveChangesAsync(ct);

            // publish message after db write
            var orderCreatedEvent = new OrderCreatedEventV1(order.Id, order.UserId, order.Product, order.Quantity, order.Price, order.CreatedAtUtc);
    
            var kafkaEvent = new KafkaEvent<OrderCreatedEventV1>(
                EventId: Guid.NewGuid(),
                EventType: _orderCreatedTopic,
                Version: 1,
                OccurredAtUtc: DateTime.UtcNow,
                Producer: "order-service",
                Key: order.UserId.ToString(), // key strategy = ordering per user
                Data: orderCreatedEvent
            );

            try
            {
                await _kafkaProducer.PublishAsync(_orderCreatedTopic, kafkaEvent.Key, kafkaEvent, ct);
            }
            catch (Exception ex)
            {
                // dead stop here, no outbox or retry mechanisms for now
                throw new ServiceUnavailableException($"Failed to publish order creation event: {ex.Message}, please retry");
            }

            return order;
        }

        public async Task<IReadOnlyList<Order>> GetAllOrdersByUserIdAsync(Guid userId, CancellationToken ct)
        {
            var orders = await _orderRepository.GetAllByUserIdAsync(userId, ct);
            return orders;
        }

        public async Task<Order> GetOrderByIdAsync(Guid orderId, CancellationToken ct)
        {
            var order = await _orderRepository.GetByIdAsync(orderId, ct);
            if (order is null)
            {
                throw new NotFoundException($"Order {orderId} not found");
            }
            return order;
        }
    }
}