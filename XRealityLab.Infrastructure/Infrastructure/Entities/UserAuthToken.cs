using System;
using System.Collections.Generic;

namespace Infrastructure.Entities
{
    public partial class UserAuthToken
    {
        public int UserAuthTokenId { get; set; }
        public int UserId { get; set; }
        public string AuthToken { get; set; }
        public DateTime GeneratedAt { get; set; }

        public virtual User User { get; set; }
    }
}
