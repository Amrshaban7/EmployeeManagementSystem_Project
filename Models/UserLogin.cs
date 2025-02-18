using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace EMS_Project.Models;

public partial class UserLogin
{
    public int Id { get; set; }

    public string UserName { get; set; } = null!;

    public DateTime LoginTimestamp { get; set; }

    public string? Ipaddress { get; set; }

    public string? BrowserInfo { get; set; }

    public bool LoginStatus { get; set; }

    public int? UsersId { get; set; }

    [ForeignKey("UsersId")]
    public virtual User? Users { get; set; }
}
