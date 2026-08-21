using System;
using System.Collections.Generic;
using System.Text;

namespace EkimemoUtilities.Services;

public interface IOverlayService
{
    bool IsShowing { get; }
    bool HasPermission();
    void RequestPermission();
    void Show();
    void Hide();
    void Toggle();
}