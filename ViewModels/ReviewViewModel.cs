using System;

namespace E_ShoppingManagement.ViewModels
{
    public class ReviewViewModel
    {
        public int Id { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string? ProfilePicture { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = "Pending";
        public string ProductName { get; set; } = string.Empty;
        public string? ImageUrls { get; set; }
        public int LikeCount { get; set; }
        public int DislikeCount { get; set; }
        public bool IsLikedByCurrentUser { get; set; }
        public bool IsDislikedByCurrentUser { get; set; }
        public List<ReviewReplyViewModel> Replies { get; set; } = new List<ReviewReplyViewModel>();
    }

    public class ReviewReplyViewModel
    {
        public int Id { get; set; }
        public string ReplierName { get; set; } = string.Empty;
        public string? ReplierImage { get; set; }
        public string ReplyText { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsSeller { get; set; }
    }
}
