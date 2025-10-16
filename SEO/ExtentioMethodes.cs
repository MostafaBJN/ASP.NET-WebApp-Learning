using System.Text;

namespace SEO
{
    public static class ExtentioMethodes
    {
        public static string ReadAsString(this IFormFile file)
        {
            var result = new StringBuilder();
            using (var reader = new StreamReader(file.OpenReadStream()))
            {
                while (reader.Peek() >= 0)
                    result.AppendLine(reader.ReadLine());
            }
            return result.ToString();
        }

    }
}
