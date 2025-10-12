using Entities.Interfaces;

namespace Entities.Models
{
    public class Partner : IDbEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ContactPerson { get; set; }
        public string PhoneNumber { get; set; }


        public int NodeId { get; set; }
        public virtual Node Location { get; set; }
    }
}