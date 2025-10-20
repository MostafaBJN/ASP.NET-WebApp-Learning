namespace SEO.Models
{
    public class Post
    {
        public int Id { get; set; }
        public string Title { get; set; }

        // Store EditorJS JSON as string
        public string EditorDataJson { get; set; }
    }
}
