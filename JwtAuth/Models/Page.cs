using System.ComponentModel.DataAnnotations;

namespace JwtAuth.Models
{
    public class Page
    {
        [Key]
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
    }
}
