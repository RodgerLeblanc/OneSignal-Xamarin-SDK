﻿using System;
using System.Collections.Generic;
using Com.OneSignal.Abstractions;
using OSNotificationOpenedResult = Com.OneSignal.Abstractions.OSNotificationOpenedResult;
using OSNotification = Com.OneSignal.Abstractions.OSNotification;
using OSNotificationAction = Com.OneSignal.Abstractions.OSNotificationAction;
using OSNotificationPayload = Com.OneSignal.Abstractions.OSNotificationPayload;
using OSInFocusDisplayOption = Com.OneSignal.Abstractions.OSInFocusDisplayOption;
using System.Diagnostics;
using UserNotifications;

namespace Com.OneSignal
{
   public class OneSignalImplementation : OneSignalShared, IOneSignal
   {
      public static Dictionary<string, object> NSDictToPureDict(Foundation.NSDictionary nsDict)
      {
         if (nsDict == null)
            return null;
         Foundation.NSError error;
         Foundation.NSData jsonData = Foundation.NSJsonSerialization.Serialize(nsDict, 0, out error);
         Foundation.NSString jsonNSString = Foundation.NSString.FromData(jsonData, Foundation.NSStringEncoding.UTF8);
         string jsonString = jsonNSString.ToString();
         return Json.Deserialize(jsonString) as Dictionary<string, object>;
      }

      private OSNotificationOpenedResult OSNotificationOpenedResultToNative(iOS.OSNotificationOpenedResult result)
      {
         var openresult = new OSNotificationOpenedResult();
         openresult.action = new OSNotificationAction();
         iOS.OSNotificationAction action = result.Action;
         openresult.action.actionID = action.ActionID;
         openresult.action.type = (OSNotificationAction.ActionType)(int)action.Type;

         openresult.notification = OSNotificationToNative(result.Notification);

         return openresult;
      }

    private OSNotification OSNotificationToNative(iOS.OSNotification notif)
    {
      var notification = new OSNotification();
      notification.displayType = (OSNotification.DisplayType)notif.DisplayType;
      notification.shown = notif.Shown;
      notification.silentNotification = notif.SilentNotification;
      notification.isAppInFocus = notif.IsAppInFocus;
      notification.payload = new OSNotificationPayload();


         notification.payload.actionButtons = new List<Dictionary<string, object>>();
         if (notif.Payload.ActionButtons != null)
         {
            for (int i = 0; i < (int)notif.Payload.ActionButtons.Count; ++i)
            {
               Foundation.NSDictionary element = notif.Payload.ActionButtons.GetItem<Foundation.NSDictionary>((uint)i);
               notification.payload.actionButtons.Add(NSDictToPureDict(element));
            }
         }

         notification.payload.additionalData = new Dictionary<string, object>();
         if (notif.Payload.AdditionalData != null)
         {
            foreach (KeyValuePair<Foundation.NSObject, Foundation.NSObject> element in notif.Payload.AdditionalData)
            {
               notification.payload.additionalData.Add((Foundation.NSString)element.Key, element.Value);
            }
         }

         notification.payload.badge = (int)notif.Payload.Badge;
         notification.payload.body = notif.Payload.Body;
         notification.payload.contentAvailable = notif.Payload.ContentAvailable;
         notification.payload.launchURL = notif.Payload.LaunchURL;
         notification.payload.notificationID = notif.Payload.NotificationID;
         notification.payload.sound = notif.Payload.Sound;
         notification.payload.subtitle = notif.Payload.Subtitle;
         notification.payload.title = notif.Payload.Title;

         return notification;
      }

      // Init - Only required method you call to setup OneSignal to receive push notifications.
      public override void InitPlatform()
      {
         //extract settings
         bool autoPrompt = true, inAppLaunchURL = true;

         if (builder.iOSSettings != null)
         {
            if (builder.iOSSettings.ContainsKey(IOSSettings.kOSSettingsKeyAutoPrompt))
               autoPrompt = builder.iOSSettings[IOSSettings.kOSSettingsKeyAutoPrompt];
            if (builder.iOSSettings.ContainsKey(IOSSettings.kOSSettingsKeyInAppLaunchURL))
               inAppLaunchURL = builder.iOSSettings[IOSSettings.kOSSettingsKeyInAppLaunchURL];
         }
         Init(builder.mAppId, autoPrompt, inAppLaunchURL, builder.displayOption, logLevel, visualLogLevel);
      }

      public void Init(string appId, bool autoPrompt, bool inAppLaunchURLs, OSInFocusDisplayOption displayOption, LOG_LEVEL logLevel, LOG_LEVEL visualLevel)
      {
         var convertedLogLevel = (iOS.OneSLogLevel)((int)logLevel);
         var convertedVisualLevel = (iOS.OneSLogLevel)((int)visualLevel);

         iOS.OneSignal.SetLogLevel(convertedLogLevel, convertedVisualLevel);
         var dict = new Foundation.NSDictionary("kOSSettingsKeyInAppLaunchURL", new Foundation.NSNumber(inAppLaunchURLs),
                                                "kOSSettingsKeyAutoPrompt", new Foundation.NSNumber(autoPrompt),
                                                "kOSSettingsKeyInFocusDisplayOption", new Foundation.NSNumber((int)displayOption)
                                               );
         iOS.OneSignal.SetMSDKType("xam");
         iOS.OneSignal.InitWithLaunchOptions(new Foundation.NSDictionary(), appId, NotificationReceivedHandler, NotificationOpenedHandler, dict);

      }

      public override void RegisterForPushNotifications()
      {
         iOS.OneSignal.RegisterForPushNotifications();
      }

      public override void SendTag(string tagName, string tagValue)
      {
         iOS.OneSignal.SendTag(tagName, tagValue);
      }

      public override void SendTags(IDictionary<string, string> tags)
      {
         string jsonString = Json.Serialize(tags);
         iOS.OneSignal.SendTagsWithJsonString(jsonString);
      }

      public override void GetTags(TagsReceived tagsReceived)
      {
         if (tagsReceived == null)
            throw new ArgumentNullException(nameof(tagsReceived));
         iOS.OneSignal.GetTags(tags => tagsReceived(NSDictToPureDict(tags)));
      }

      public override void DeleteTag(string key)
      {
         iOS.OneSignal.DeleteTag(key);
      }

      public override void DeleteTags(IList<string> keys)
      {
         Foundation.NSObject[] objs = new Foundation.NSObject[keys.Count];
         for (int i = 0; i < keys.Count; i++)
         {
            objs[i] = (Foundation.NSString)keys[i];
         }
         iOS.OneSignal.DeleteTags(objs);
      }

		public override void ClearAndroidOneSignalNotifications()
		{
			Debug.WriteLine("ClearAndroidOneSignalNotifications() is an android-only function, and is not implemented in iOS.");
		}

      public override void UnsubscribeWhenNotificationsAreDisabled(bool set)
      {
         Debug.WriteLine("UnsubscribeWhenNotificationsAreDisabled() is an android-only function, and is not implemented in iOS.");
      }

		public override void IdsAvailable(IdsAvailableCallback idsAvailable)
		{
			if (idsAvailable == null)
				throw new ArgumentNullException(nameof(idsAvailable));
			iOS.OneSignal.IdsAvailable((playerId, pushToken) => idsAvailable(playerId, pushToken));
		}

		public override void SetSubscription(bool enable)
      {
         iOS.OneSignal.SetSubscription(enable);
      }

      public override void PostNotification(Dictionary<string, object> data, OnPostNotificationSuccess success, OnPostNotificationFailure failure)
      {
         string jsonString = Json.Serialize(data);
         iOS.OneSignal.PostNotificationWithJsonString(jsonString,
             result => success?.Invoke(NSDictToPureDict(result)),
             error =>
             {
                if (failure != null)
                {
                   Dictionary<string, object> dict;
                   if (error.UserInfo != null && error.UserInfo["returned"] != null)
                      dict = NSDictToPureDict(error.UserInfo);
                   else
                      dict = new Dictionary<string, object> { { "error", "HTTP no response error" } };
                   failure(dict);
                }
             });
      }

      public override void SetEmail(string email, string emailAuthCode, OnSetEmailSuccess success, OnSetEmailFailure failure)
      {
         iOS.OneSignal.SetEmail(email, emailAuthCode, () => success?.Invoke(), error =>
             {
                if (failure != null)
                {
                   Dictionary<string, object> dict;
                   if (error.UserInfo != null)
                      dict = NSDictToPureDict(error.UserInfo);
                   else
                      dict = new Dictionary<string, object> { { "error", "An unknown error occurred" } };
                   failure(dict);
                }
             });
      }

      public override void SetEmail(string email, OnSetEmailSuccess success, OnSetEmailFailure failure)
      {
         iOS.OneSignal.SetEmail(email, () => success?.Invoke(), error =>
             {
                if (failure != null)
                {
                   Dictionary<string, object> dict;
                   if (error.UserInfo != null)
                      dict = NSDictToPureDict(error.UserInfo);
                   else
                      dict = new Dictionary<string, object> { { "error", "An unknown error occurred" } };
                   failure(dict);
                }
             });
      }

      public override void LogoutEmail(OnSetEmailSuccess success, OnSetEmailFailure failure)
      {
         iOS.OneSignal.LogoutEmail(() => success?.Invoke(), error =>
             {
                if (failure != null)
                {
                   Dictionary<string, object> dict;
                   if (error.UserInfo != null)
                      dict = NSDictToPureDict(error.UserInfo);
                   else
                      dict = new Dictionary<string, object> { { "error", "An unknown error occurred" } };
                   failure(dict);
                }
             });
      }

      public override void UserDidProvidePrivacyConsent(bool granted) {
         iOS.OneSignal.ConsentGranted(granted);
      }
      
      public override bool RequiresUserPrivacyConsent() {
         return iOS.OneSignal.RequiresUserPrivacyConsent();
      }
      
      public override void SetRequiresUserPrivacyConsent(bool required) {
         iOS.OneSignal.SetRequiresUserPrivacyConsent(required);
      }
      
      public override void SetExternalUserId(string externalId) {
         iOS.OneSignal.SetExternalUserId(externalId);
      }
      
      public override void RemoveExternalUserId() {
         iOS.OneSignal.RemoveExternalUserId();
      }

      public override void SetLogLevel(LOG_LEVEL logLevel, LOG_LEVEL visualLevel)
      {
         base.SetLogLevel(logLevel, visualLevel);

         var convertedLogLevel = (iOS.OneSLogLevel)((ulong)((int)logLevel));
         var convertedVisualLevel = (iOS.OneSLogLevel)((ulong)((int)visualLevel));
         iOS.OneSignal.SetLogLevel(convertedLogLevel, convertedVisualLevel);
      }

      public void NotificationOpenedHandler(iOS.OSNotificationOpenedResult result)
      {
         OnPushNotificationOpened(OSNotificationOpenedResultToNative(result));
      }
      public void NotificationReceivedHandler(iOS.OSNotification notification)
      {
         OnPushNotificationReceived(OSNotificationToNative(notification));
      }

      [Obsolete("SyncHashedEmail has been deprecated. Please use SetEmail() instead.")]
      public override void SyncHashedEmail(string email)
      {
         iOS.OneSignal.SyncHashedEmail(email);
      }

      public override void PromptLocation()
      {
         iOS.OneSignal.PromptLocation();
      }
      
      public override void SetLocationShared(bool shared) {
         iOS.OneSignal.SetLocationShared(shared);
      }

      public void DidReceiveNotificationExtensionRequest(UNNotificationRequest request, UNMutableNotificationContent replacementContent)
      {
         iOS.OneSignal.DidReceiveNotificationExtensionRequest(request, replacementContent);
      }

      public void ServiceExtensionTimeWillExpireRequest(UNNotificationRequest request, UNMutableNotificationContent replacementContent)
      {
         iOS.OneSignal.ServiceExtensionTimeWillExpireRequest(request, replacementContent);
      }

      public override void AddTrigger(string key, object value)
      {
         iOS.OneSignal.Onesignal_Log(iOS.OneSLogLevel.Warn, "OneSignal: AddTrigger - Not yet implemented on iOS");
      }

      public override void AddTriggers(Dictionary<string, object> triggers)
      {
         iOS.OneSignal.Onesignal_Log(iOS.OneSLogLevel.Warn, "OneSignal: AddTriggers - Not yet implemented on iOS");
      }

      public override void RemoveTriggerForKey(string key)
      {
         iOS.OneSignal.Onesignal_Log(iOS.OneSLogLevel.Warn, "OneSignal: RemoveTriggerForKey - Not yet implemented on iOS");
      }

      public override void RemoveTriggersForKeys(List<string> keys)
      {
         iOS.OneSignal.Onesignal_Log(iOS.OneSLogLevel.Warn, "OneSignal: RemoveTriggersForKeys - Not yet implemented on iOS");
      }

      public override object GetTriggerValueForKey(string key)
      {
         iOS.OneSignal.Onesignal_Log(iOS.OneSLogLevel.Warn, "OneSignal: GetTriggerValueForKey - Not yet implemented on iOS");
         return null;
      }

      public override void PauseInAppMessages(bool pause)
      {
         iOS.OneSignal.Onesignal_Log(iOS.OneSLogLevel.Warn, "OneSignal: PauseInAppMessages - Not yet implemented on iOS");
      }
   }
}