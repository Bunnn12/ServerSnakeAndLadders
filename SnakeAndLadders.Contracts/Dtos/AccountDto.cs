namespace SnakeAndLadders.Contracts.Dtos
{
    public class AccountDto
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string ProfileDescription { get; set; }
        public int Coins { get; set; }
        public bool HasProfilePhoto { get; set; }
        public string ProfilePhotoId { get; set; }
        public int? CurrentSkinUnlockedId { get; set; }
        public string CurrentSkinId { get; set; }
    }

}
