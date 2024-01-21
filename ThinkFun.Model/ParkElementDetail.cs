using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThinkFun.Model;

public class ParkElementDetail
{
    public Queue? LastMesure { get; set; } = null;
    public HistoryArray? Today { get; set; } = null;
    public HistoryArray? LastDay { get; set; } = null;
    public HistoryArray? LastWeekSameDay { get; set; } = null;
}
