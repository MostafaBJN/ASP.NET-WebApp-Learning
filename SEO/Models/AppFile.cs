using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace SEO.Models
{
    public class AppFile
    {
        public AppFile()
        {
            //file = new List<IFormFile>();   
        }
        public int Id { get; set; }
        public IFormFile UploadFile { get; set; }
      

    }

}
