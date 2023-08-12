using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThinkFun.Model;

public class User
{
    public string Identifier { get; set; }
    public string Name { get; set; }
    public string PasswordHash { get; set; }
    public HashSet<string> Roles { get; set; } = new HashSet<string>();
}
