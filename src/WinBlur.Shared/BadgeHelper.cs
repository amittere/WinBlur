using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace WinBlur.Shared
{
    public static class BadgeHelper
    {
        private static BadgeUpdater badgeUpdater;

        public static void UpdateNumericBadge(uint count)
        {
            XmlDocument badgeXml = BadgeUpdateManager.GetTemplateContent(BadgeTemplateType.BadgeNumber);
            XmlElement badgeElement = badgeXml.SelectSingleNode("/badge") as XmlElement;
            badgeElement.SetAttribute("value", count.ToString());

            if (badgeUpdater == null)
            {
                badgeUpdater = BadgeUpdateManager.CreateBadgeUpdaterForApplication();
            }

            badgeUpdater.Update(new BadgeNotification(badgeXml));
        }
    }
}
