using System.ComponentModel.DataAnnotations;

namespace SEO.Models
{
    public class CustomerModel
    {
        [Required]
        [Display(Name = "File")]
        public IFormFile FormFile { get; set; }


    }
}
