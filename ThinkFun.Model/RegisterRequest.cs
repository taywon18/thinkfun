﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThinkFun.Model;

public class RegisterRequest
{
    public string Name { get; set; }
    public string Password { get; set; }
    public string Mail { get; set; }
}
