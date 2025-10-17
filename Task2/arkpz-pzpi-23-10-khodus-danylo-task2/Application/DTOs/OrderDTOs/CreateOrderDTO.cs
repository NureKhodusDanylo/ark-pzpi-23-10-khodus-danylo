namespace Application.DTOs.OrderDTOs
{
    public class CreateOrderDTO
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public double Weight { get; set; }
        public decimal Price { get; set; }
        public int RecipientId { get; set; }
        public int PickupNodeId { get; set; }
        public int DropoffNodeId { get; set; }
    }
}
