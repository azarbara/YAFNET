﻿/* Yet Another Forum.NET
 * Copyright (C) 2006-2011 Jaben Cargman
 * http://www.yetanotherforum.net/
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 */

namespace YAF.DotNetNuke
{
    using System;
    using System.Data;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Web.Security;

    using global::DotNetNuke.Common.Utilities;
    using global::DotNetNuke.Entities.Modules;
    using global::DotNetNuke.Entities.Portals;
    using global::DotNetNuke.Entities.Profile;
    using global::DotNetNuke.Entities.Users;
    using global::DotNetNuke.Services.FileSystem;

    using YAF.Classes;
    using YAF.Classes.Data;
    using YAF.Core;
    using YAF.Types.Constants;
    using YAF.Types.EventProxies;
    using YAF.Types.Interfaces;
    using YAF.Utils;

    using FileInfo = global::DotNetNuke.Services.FileSystem.FileInfo;

    /// <summary>
    /// YAF DNN Profile Syncronization 
    /// </summary>
    public class ProfileSyncronizer : PortalModuleBase
    {
        /// <summary>
        /// The _portal settings.
        /// </summary>
        private static PortalSettings _portalSettings;

        /// <summary>
        /// Gets CurrentPortalSettings.
        /// </summary>
        private static PortalSettings CurrentPortalSettings
        {
            get
            {
                return _portalSettings ?? (_portalSettings = PortalController.GetCurrentPortalSettings());
            }
        }

        /// <summary>
        /// Syncronizes The Yaf Profile with Dnn Profile or reverse if 
        /// one profile is newer then the other
        /// </summary>
        /// <param name="yafUserId">
        /// The Yaf UserId
        /// </param>
        /// <param name="dnnUserInfo">
        /// Dnn UserInfo of current User
        /// </param>
        /// <param name="membershipUser">
        /// MemberShip of current User
        /// </param>
        /// <param name="portalId">
        /// The PortalId
        /// </param>
        /// <param name="boardId">
        /// The board Id.
        /// </param>
        public static void UpdateUserProfile(int yafUserId, UserInfo dnnUserInfo, MembershipUser membershipUser, int portalId, int boardId)
        {
            var yafUserProfile = YafUserProfile.GetProfile(membershipUser.UserName);

            var yafTime = yafUserProfile.LastUpdatedDate;
            var dnnTime = DataController.YafDnnGetLastUpdatedProfile(10);

            TimeSpan timeCompare = dnnTime - yafTime;

            if (timeCompare.TotalSeconds > 0)
            {
                SyncYaf(yafUserId, yafUserProfile, dnnUserInfo, portalId, boardId);
            }
            else
            {
                SyncDnn(yafUserId, yafUserProfile, dnnUserInfo, membershipUser, portalId);
            }
        }

        /// <summary>
        /// The get user time zone offset.
        /// </summary>
        /// <param name="userInfo">
        /// The user info.
        /// </param>
        /// <param name="portalSettings">
        /// Current Portal Settings
        /// </param>
        /// <returns>
        /// The get user time zone offset.
        /// </returns>
        public static int GetUserTimeZoneOffset(UserInfo userInfo, PortalSettings portalSettings)
        {
            int timeZone;

            if ((userInfo != null) && (userInfo.UserID != Null.NullInteger))
            {
                timeZone = userInfo.Profile.TimeZone;
            }
            else
            {
                if (portalSettings != null)
                {
                    timeZone = portalSettings.TimeZoneOffset;
                }
                else
                {
                    timeZone = -480;
                }
            }

            return timeZone;
        }

        /// <summary>
        /// yaf profile is newer sync dnn now
        /// </summary>
        /// <param name="yafUserId">
        /// The yaf user id.
        /// </param>
        /// <param name="yafUserProfile">
        /// The yaf user profile.
        /// </param>
        /// <param name="dnnUserInfo">
        /// The dnn user info.
        /// </param>
        /// <param name="membershipUser">
        /// The membership user.
        /// </param>
        /// <param name="portalId">
        /// The portal id.
        /// </param>
        private static void SyncDnn(int yafUserId, YafUserProfile yafUserProfile, UserInfo dnnUserInfo, MembershipUser membershipUser, int portalId)
        {
            var yafUserData = new CombinedUserDataHelper(yafUserId);

            if (yafUserProfile.RealName.Contains(" "))
            {
                // Split Fullname into First and Lastname
                var firstName = yafUserProfile.RealName.Remove(yafUserProfile.RealName.IndexOf(" "));
                var lastName = yafUserProfile.RealName.Substring(yafUserProfile.RealName.IndexOf(" ") + 1);

                dnnUserInfo.Profile.FirstName = firstName;
                dnnUserInfo.Profile.LastName = lastName;
            }
            else
            {
                dnnUserInfo.Profile.FirstName = yafUserProfile.RealName;
            }

            dnnUserInfo.Profile.Country = yafUserProfile.Location;
            dnnUserInfo.Profile.Website = yafUserProfile.Homepage;
            dnnUserInfo.Email = membershipUser.Email;
            dnnUserInfo.Profile.TimeZone = int.Parse(yafUserData.TimeZone.ToString());

            if (!string.IsNullOrEmpty(yafUserData.CultureUser))
            {
                dnnUserInfo.Profile.PreferredLocale = yafUserData.CultureUser;
            }

            // TODO : Save Yaf Avatar
            /*var userAvatarUrl = YafContext.Current.Get<IAvatars>().GetAvatarUrlForUser(yafUserId);

            if (!string.IsNullOrEmpty(userAvatarUrl))
            {
                dnnUserInfo.Profile.Photo = SaveYafAvatar(userAvatarUrl, dnnUserInfo, portalId);
            }*/

            // Save other Yaf Profile Properties as Custom DNN Profile Properties
            dnnUserInfo.Profile.SetProfileProperty("Biography", yafUserProfile.Interests);

            dnnUserInfo.Profile.SetProfileProperty("Birthday", yafUserProfile.Birthday.ToString());
            dnnUserInfo.Profile.SetProfileProperty("Occupation", yafUserProfile.Occupation);
            dnnUserInfo.Profile.SetProfileProperty("Gender", yafUserProfile.Gender.ToString());
            dnnUserInfo.Profile.SetProfileProperty("Blog", yafUserProfile.Blog);
            dnnUserInfo.Profile.SetProfileProperty("MSN", yafUserProfile.MSN);
            dnnUserInfo.Profile.SetProfileProperty("YIM", yafUserProfile.YIM);
            dnnUserInfo.Profile.SetProfileProperty("AIM", yafUserProfile.AIM);
            dnnUserInfo.Profile.SetProfileProperty("ICQ", yafUserProfile.ICQ);
            dnnUserInfo.Profile.SetProfileProperty("XMPP", yafUserProfile.XMPP);
            dnnUserInfo.Profile.SetProfileProperty("Skype", yafUserProfile.Skype);

            ProfileController.UpdateUserProfile(dnnUserInfo, dnnUserInfo.Profile.ProfileProperties);

            // Save both to make sure both have the same Last Update flag.
            UserController.UpdateUser(portalId, dnnUserInfo);
        }

        /// <summary>
        /// Dnn Profile is newer, sync yaf now
        /// NOTE : no neeed to manually sync Email Adress
        /// </summary>
        /// <param name="yafUserId">
        /// The yaf user id.
        /// </param>
        /// <param name="yafUserProfile">
        /// The yaf user profile.
        /// </param>
        /// <param name="dnnUserInfo">
        /// The dnn user info.
        /// </param>
        /// <param name="portalId">
        /// The Portal id.</param>
        /// <param name="boardId">
        /// The board Id.
        /// </param>
        private static void SyncYaf(int yafUserId, YafUserProfile yafUserProfile, UserInfo dnnUserInfo, int portalId, int boardId)
        {
            var yafUserData = new CombinedUserDataHelper(yafUserId);

            yafUserProfile.Location = dnnUserInfo.Profile.Country;
            yafUserProfile.Homepage = dnnUserInfo.Profile.Website;

            if (!string.IsNullOrEmpty(dnnUserInfo.Profile.Photo))
            {
                FileController fileController = new FileController();

                FileInfo fileInfoAvatar = fileController.GetFileById(int.Parse(dnnUserInfo.Profile.Photo), portalId);

                if (fileInfoAvatar != null)
                {
                    SaveDnnAvatar(fileInfoAvatar, yafUserId);
                }
            }

            // Save other Yaf Profile Properties as Custom DNN Profile Properties
            try
            {
                yafUserProfile.Interests = dnnUserInfo.Profile.GetPropertyValue("Biography");
                yafUserProfile.Birthday = DateTime.Parse(dnnUserInfo.Profile.GetPropertyValue("Birthday"));
                yafUserProfile.Occupation = dnnUserInfo.Profile.GetPropertyValue("Occupation");
                yafUserProfile.Gender = int.Parse(dnnUserInfo.Profile.GetPropertyValue("Gender"));
                yafUserProfile.Blog = dnnUserInfo.Profile.GetPropertyValue("Blog");
                yafUserProfile.MSN = dnnUserInfo.Profile.GetPropertyValue("MSN");
                yafUserProfile.YIM = dnnUserInfo.Profile.GetPropertyValue("YIM");
                yafUserProfile.AIM = dnnUserInfo.Profile.GetPropertyValue("AIM");
                yafUserProfile.ICQ = dnnUserInfo.Profile.GetPropertyValue("ICQ");
                yafUserProfile.XMPP = dnnUserInfo.Profile.GetPropertyValue("XMPP");
                yafUserProfile.Skype = dnnUserInfo.Profile.GetPropertyValue("Skype");
            }
            finally 
            {
                yafUserProfile.Save();
            }
            

            YafDnnModule.YafCultureInfo userCuluture = new YafDnnModule.YafCultureInfo
                {
                    LanguageFile = yafUserData.LanguageFile,
                    Culture = yafUserData.CultureUser
                };

            if (string.IsNullOrEmpty(dnnUserInfo.Profile.PreferredLocale))
            {
                return;
            }

            CultureInfo newCulture = new CultureInfo(dnnUserInfo.Profile.PreferredLocale);

            foreach (DataRow row in
                StaticDataHelper.Cultures().Rows.Cast<DataRow>().Where(
                    row => dnnUserInfo.Profile.PreferredLocale == row["CultureTag"].ToString() || newCulture.TwoLetterISOLanguageName == row["CultureTag"].ToString()))
            {
                userCuluture.LanguageFile = row["CultureFile"].ToString();
                userCuluture.Culture = row["CultureTag"].ToString();
            }

            LegacyDb.user_save(
                yafUserId,
                boardId,
                null,
                yafUserData.DisplayName,
                null,
                dnnUserInfo.Profile.TimeZone,
                userCuluture.LanguageFile,
                userCuluture.Culture,
                yafUserData.ThemeFile,
                yafUserData.UseMobileTheme,
                null,
                null,
                null,
                yafUserData.DSTUser,
                yafUserData.IsActiveExcluded,
                null);
        }

        /*
        /// <summary>
        /// TODO : Extract YAF Avatar to DNN Photo
        /// </summary>
        /// <param name="avatarUrl">
        /// The avatar url.
        /// </param>
        /// <param name="dnnUserInfo">
        /// The dnn user info.
        /// </param>
        /// <param name="portalId">
        /// The portal id.
        /// </param>
        /// <returns>
        /// </returns>
        private static string SaveYafAvatar(string avatarUrl, UserInfo dnnUserInfo, int portalId)
        {
            try
            {
                var folderPath = FileSystemUtils.GetUserFolderPath(dnnUserInfo.UserID);

                // Make sure the user folder exists
                FileSystemUtils.AddUserFolder(
                    CurrentPortalSettings,
                    CurrentPortalSettings.HomeDirectoryMapPath,
                    (int)FolderController.StorageLocationTypes.InsecureFileSystem,
                    dnnUserInfo.UserID);

                FolderInfo folder = new FolderController().GetFolder(portalId, folderPath, true);

                var yafAvatarName = "YafAvatar{0}.jpg".FormatWith(dnnUserInfo.UserID);

                if (!avatarUrl.StartsWith("http"))
                {
                    avatarUrl = BaseUrlBuilder.BaseUrl + avatarUrl;
                }

                // Download Yaf Avatar
                WebClient wc = new WebClient();

                wc.Headers.Add("Referer", YafBuildLink.GetLink(ForumPages.forum));


                wc.DownloadFile(avatarUrl, Path.Combine(CurrentPortalSettings.HomeDirectoryMapPath, Path.Combine(folderPath, yafAvatarName)));

                FileSystemUtils.AddFile(Path.Combine(folderPath, yafAvatarName), portalId, true, folder);

                FileController fileController = new FileController();

                FileInfo info = fileController.GetFile(yafAvatarName, portalId, folder.FolderID);

                return info.FileId.ToString();
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }*/


        /// <summary>
        /// Save Dnn Avatar as Yaf Remote Avatar with relative Path.
        /// </summary>
        /// <param name="fileInfoAvatar">
        /// The file info avatar.
        /// </param>
        /// <param name="yafUserId">
        /// The yaf user id.
        /// </param>
        private static void SaveDnnAvatar(FileInfo fileInfoAvatar, int yafUserId)
        {
            // update
            LegacyDb.user_saveavatar(
                yafUserId,
                BaseUrlBuilder.BaseUrl + Path.Combine(CurrentPortalSettings.HomeDirectory, fileInfoAvatar.RelativePath),
                null,
                null);

            // clear the cache for this user...
            YafContext.Current.Get<IRaiseEvent>().Raise(new UpdateUserEvent(yafUserId));
        }
    }
}