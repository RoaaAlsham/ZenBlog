namespace ZenBlog.Domain.Entities
{
    public class Blog : Common.BaseEntity
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string? CoverImageUrl { get; set; }

        public Guid CategoryId { get; set; } // Foreign key to Category
        public virtual  Category Category { get; set; } // Navigation property to Category
        
        public string UserId { get; set; } // Foreign key to User
        public virtual AppUser User { get; set; } // Navigation property to User

        public virtual IList<Comment> Comments { get; set; } = new List<Comment>();
    }
}
