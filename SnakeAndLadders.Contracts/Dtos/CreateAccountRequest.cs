using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAndLadders.Contracts.Dtos
{
    public sealed class CreateAccountRequestDto
    {
        public string Username { get; set; }           
        public string FirstName { get; set; }          
        public string LastName { get; set; }       
        public string Email { get; set; }           
        public string PasswordHash { get; set; }       
        public string ProfileDescription { get; set; } 
        public string ProfilePhotoId { get; set; }   
    }
}
