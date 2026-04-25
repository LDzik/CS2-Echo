using System;
using System.Collections.Generic;
using System.Text;

namespace CS2_Echo.Logic.Interfaces;

public interface IOverlayService
{
    bool IsOverlayOpen { get; }
    bool IsOverlayLocked { get; }
    void ShowOverlay(bool startLocked = false);
    void CloseOverlay();
    void SetLockState(bool isLocked);
}
