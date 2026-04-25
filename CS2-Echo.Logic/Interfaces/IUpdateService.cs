using System;
using System.Collections.Generic;
using System.Text;

namespace CS2_Echo.Logic.Interfaces;

public interface IUpdateService
{
    Task<bool> CheckForUpdatesAsync();
    Task DownloadAndApplyUpdateAsync();
}

