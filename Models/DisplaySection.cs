using System.ComponentModel.DataAnnotations;

namespace E_ShoppingManagement.Models
{
    public class DisplaySection
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
        public int DisplayOrder { get; set; } = 0;
    }
}
