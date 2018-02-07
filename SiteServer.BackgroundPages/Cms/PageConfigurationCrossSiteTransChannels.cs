﻿using System;
using System.Collections.Specialized;
using System.Web.UI.WebControls;
using SiteServer.Utils;
using SiteServer.BackgroundPages.Core;
using SiteServer.CMS.Core;
using SiteServer.CMS.Model.Enumerations;

namespace SiteServer.BackgroundPages.Cms
{
    public class PageConfigurationCrossSiteTransChannels : BasePageCms
    {
        public Repeater RptContents;

        private int _currentChannelId;

        public static string GetRedirectUrl(int siteId, int currentChannelId)
        {
            return PageUtils.GetCmsUrl(siteId, nameof(PageConfigurationCrossSiteTransChannels), new NameValueCollection
            {
                {"CurrentChannelId", currentChannelId.ToString()}
            });
        }

		public void Page_Load(object sender, EventArgs e)
        {
            if (IsForbidden) return;

			PageUtils.CheckRequestParameter("siteId");

            if (IsPostBack) return;

            VerifySitePermissions(ConfigManager.Permissions.WebSite.Configration);

            ClientScriptRegisterClientScriptBlock("NodeTreeScript", ChannelLoading.GetScript(SiteInfo, ELoadingType.ConfigurationCrossSiteTrans, null));

            if (Body.IsQueryExists("CurrentChannelId"))
            {
                _currentChannelId = Body.GetQueryInt("CurrentChannelId");
                var onLoadScript = ChannelLoading.GetScriptOnLoad(SiteId, _currentChannelId);
                if (!string.IsNullOrEmpty(onLoadScript))
                {
                    ClientScriptRegisterClientScriptBlock("NodeTreeScriptOnLoad", onLoadScript);
                }
            }

            RptContents.DataSource = DataProvider.ChannelDao.GetIdListByParentId(SiteId, 0);
            RptContents.ItemDataBound += RptContents_ItemDataBound;
            RptContents.DataBind();
        }

        private void RptContents_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            var channelId = (int)e.Item.DataItem;
            var enabled = IsOwningChannelId(channelId);
            if (!enabled)
            {
                if (!IsHasChildOwningChannelId(channelId)) e.Item.Visible = false;
            }
            var nodeInfo = ChannelManager.GetChannelInfo(SiteId, channelId);
            var ltlHtml = (Literal)e.Item.FindControl("ltlHtml");
            ltlHtml.Text = ChannelLoading.GetChannelRowHtml(SiteInfo, nodeInfo, enabled, ELoadingType.ConfigurationCrossSiteTrans, null, Body.AdminName);
        }
	}
}