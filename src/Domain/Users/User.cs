using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RippleSync.Domain.Users;
public class User(Guid id, string email)
{
    public Guid Id { get; set; } = id;
    public string Email { get; set; } = email;
}
