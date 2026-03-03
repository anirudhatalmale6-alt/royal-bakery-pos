using System.ComponentModel.DataAnnotations.Schema;

namespace RoyalBakeryCashier.Data.Entities
{
    [Table("MenuItems")]
    public class MenuItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int MenuCategoryId { get; set; }
        public bool IsQuick { get; set; } = false;
        /// <summary>
        /// 0 = not quick, 1 = Quicks 1, 2 = Quicks 2
        /// </summary>
        public int QuickCategory { get; set; } = 0;
    }
}