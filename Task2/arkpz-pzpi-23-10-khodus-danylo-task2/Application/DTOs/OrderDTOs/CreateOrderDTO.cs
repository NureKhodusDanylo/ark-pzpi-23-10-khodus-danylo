namespace Application.DTOs.OrderDTOs
{
    public class CreateOrderDTO
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public double Weight { get; set; }
        public decimal ProductPrice { get; set; }
        public bool IsProductPaid { get; set; }
        public int RecipientId { get; set; }
    }
}
