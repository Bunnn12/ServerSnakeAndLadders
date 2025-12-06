using System;
using System.Collections.Generic;

namespace SnakeAndLadders.Contracts.Dtos
{
    public sealed class AuthResult
    {
        public AuthResult()
        {
            Meta = new Dictionary<string, string>();
            DisplayName = string.Empty;
            ProfilePhotoId = string.Empty;
            CurrentSkinId = string.Empty;
        }

        public bool Success { get; set; }

        public string Code { get; set; }

        public Dictionary<string, string> Meta { get; set; }

        public int? UserId { get; set; }

        public string DisplayName { get; set; }

        public string ProfilePhotoId { get; set; }

        public string Token { get; set; }

        public DateTime? ExpiresAtUtc { get; set; }

        public int? CurrentSkinUnlockedId { get; set; }

        public string CurrentSkinId { get; set; }
    }
}
