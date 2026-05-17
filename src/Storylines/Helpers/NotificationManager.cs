using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace Storylines.Helpers;

class NotificationManager
{
    public static void DisplayBadgeNotification(string badgeGlyphValue)
    {
        XmlDocument badgeXml = BadgeUpdateManager.GetTemplateContent(short.TryParse(badgeGlyphValue, out _) ? BadgeTemplateType.BadgeNumber : BadgeTemplateType.BadgeGlyph);

        XmlElement badgeElement = badgeXml.SelectSingleNode("/badge") as XmlElement;
        badgeElement.SetAttribute("value", badgeGlyphValue);

        BadgeNotification badge = new BadgeNotification(badgeXml);
        BadgeUpdater badgeUpdater = BadgeUpdateManager.CreateBadgeUpdaterForApplication();

        badgeUpdater.Update(badge);
    }

    public static void ClearBadgeNotification()
    {
        BadgeUpdateManager.CreateBadgeUpdaterForApplication().Clear();
    }

    public static void NewUpdateAvailable_Close()
    {
        App.GetService<INotificationService>().DismissPersistentNotification();
        ClearBadgeNotification();
    }
}
