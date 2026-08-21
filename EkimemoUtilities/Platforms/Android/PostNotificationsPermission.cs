using Microsoft.Maui.ApplicationModel;

namespace EkimemoUtilities.Platforms.Android;

public class PostNotificationsPermission : Permissions.BasePlatformPermission
{
    public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
        new[] { (global::Android.Manifest.Permission.PostNotifications, true) };
}