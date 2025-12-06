namespace SnakeAndLadders.Contracts.Dtos
{
    public class AccountDto
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string ProfileDescription { get; set; } = string.Empty;
        public int Coins { get; set; }
        public bool HasProfilePhoto { get; set; }
        public string ProfilePhotoId { get; set; } = string.Empty;
        public int? CurrentSkinUnlockedId { get; set; }
        public string CurrentSkinId { get; set; } = string.Empty;
    }

}
