using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YUClientSelenium
{
    class Common
    {
        public const String chromeDir = @"user-data-dir=C:\Users\Admin\AppData\Local\Google\Chrome\User Data\Sel"; // for save login session

    }

    class ElementIdentify
    {
        public const String likeXpath = "//*[@id=\"top-level-buttons\"]/ytd-toggle-button-renderer[1]";
        public const String disLikeXpath = "//*[@id=\"top-level-buttons\"]/ytd-toggle-button-renderer[2]";
        public const String shareXpath = "//*[@id=\"top-level-buttons\"]/ytd-button-renderer[1]";
        public const String saveXpath = "//*[@id=\"top-level-buttons\"]/ytd-button-renderer[2]";

        public const String subcribeXpath = "//*[@id=\"subscribe-button\"]";
        public const String subcribeTextXpath = "//*[@id=\"subscribe-button\"]/ytd-subscribe-button-renderer/paper-button/yt-formatted-string";
        public const String paperButtonCheckSub = "//*[@id=\"subscribe-button\"]/ytd-subscribe-button-renderer/paper-button";

        public const String watchList = "//*[@id=\"playlists\"]/ytd-playlist-add-to-option-renderer[1]";

        //for comment
        public const String cmtPositionId = "simplebox-placeholder";
        public const String cmtEditBoxId = "contenteditable-root";
        public const String cmtSubmitBtn = "submit-button";

        //play button
        public const String playBtnXpath = "//*[@id=\"movie_player\"]/div[24]/div[2]/div[1]/button";

        //for login
        public const String loginEditBoxId = "identifierId";
        public const String loginNextId = "identifierNext";
        public const String passEditBoxName = "password";
        public const String passNextId = "passwordNext";
    }
}
