/* Yet Another Forum.NET
 * Copyright (C) 2003-2005 Bj�rnar Henden
 * Copyright (C) 2006-2010 Jaben Cargman
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
namespace YAF.Core
{
  #region Using

  using System;
  using System.Collections;
  using System.Web.Security;
  using System.Web.UI;
  using System.Web.UI.HtmlControls;
  using System.Web.UI.WebControls;

  using YAF.Classes;
  using YAF.Core; using YAF.Types.Interfaces; using YAF.Types.Constants;
  using YAF.Core.Services;
  using YAF.Classes.Data;
  using YAF.Utils;
  using YAF.Utils.Helpers;
  using YAF.Utils.Helpers.StringUtils;
  using YAF.Types.Constants;
  using YAF.Types.Interfaces;

  #endregion

  /// <summary>
  /// The class that all YAF forum pages are derived from.
  /// </summary>
  public class ForumPage : UserControl, IRaiseControlLifeCycles, IHaveServiceLocator
  {
    #region Constants and Fields

    /// <summary>
    ///   Cache for the page
    /// </summary>
    private readonly Hashtable _pageCache;

    /// <summary>
    /// The _trans page.
    /// </summary>
    private readonly string _transPage = string.Empty;

    /// <summary>
    ///   No Database Toggle
    /// </summary>
    private bool _noDataBase;

    /// <summary>
    /// The _show footer.
    /// </summary>
    private bool _showFooter = Config.ShowFooter;

    /// <summary>
    /// The _show tool bar.
    /// </summary>
    private bool _showToolBar = Config.ShowToolBar;

    /// <summary>
    /// The _top page control.
    /// </summary>
    private Control _topPageControl;

    #endregion

    #region Constructors and Destructors

    /// <summary>
    ///   Initializes a new instance of the <see cref = "ForumPage" /> class.
    /// </summary>
    public ForumPage()
      : this(string.Empty)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ForumPage"/> class.
    /// </summary>
    /// <param name="transPage">
    /// The trans page.
    /// </param>
    public ForumPage(string transPage)
    {
      // create empty hashtable for cache entries
      this._pageCache = new Hashtable();

      this._transPage = transPage;
      this.Init += this.ForumPage_Init;
      this.Load += this.ForumPage_Load;
      this.Unload += this.ForumPage_Unload;
      this.Error += this.ForumPage_Error;
      this.PreRender += this.ForumPage_PreRender;
    }

    #endregion

    #region Events

    /// <summary>
    /// The forum page rendered.
    /// </summary>
    public event EventHandler<ForumPageRenderedArgs> ForumPageRendered;

    #endregion

    #region Properties

    /// <summary>
    ///   Gets or sets a value indicating whether AllowAsPopup.
    /// </summary>
    public bool AllowAsPopup { get; protected set; }

    /// <summary>
    ///   Gets a value indicating whether CanLogin.
    /// </summary>
    [Obsolete("Useless property that always returns true. Do not use anymore.")]
    public bool CanLogin
    {
      get
      {
        return true;
      }
    }

    /// <summary>
    ///   Gets or sets ForumFooter.
    /// </summary>
    public Control ForumFooter { get; set; }

    /// <summary>
    ///   Gets or sets ForumHeader.
    /// </summary>
    public Control ForumHeader { get; set; }

    /// <summary>
    ///   Gets or sets the Notification.
    /// </summary>
    public Control Notification { get; set; }
    
    /// <summary>
    ///   Gets or sets ForumTopControl.
    /// </summary>
    public PlaceHolder ForumTopControl { get; set; }

    /// <summary>
    ///   Gets ForumURL.
    /// </summary>
    public string ForumURL
    {
      get
      {
        return YafBuildLink.GetLink(ForumPages.forum, true);
      }
    }

    /// <summary>
    ///   Gets a value indicating whether IsAdminPage.
    /// </summary>
    public bool IsAdminPage { get; protected set; }

    /// <summary>
    ///   Gets info whether page should be hidden to guest users when forum admin requires login.
    /// </summary>
    public virtual bool IsProtected
    {
      get
      {
        return true;
      }
    }

    /// <summary>
    ///   Gets a value indicating whether IsRegisteredPage.
    /// </summary>
    public bool IsRegisteredPage { get; protected set; }

    /// <summary>
    ///   Gets LoadMessage.
    /// </summary>
    [Obsolete("Use YafContext->LoadMessage")]
    public string LoadMessage
    {
      get
      {
        return this.PageContext.LoadMessage.LoadString;
      }
    }

    /// <summary>
    ///   Gets cache associated with this page.
    /// </summary>
    public Hashtable PageCache
    {
      get
      {
        return this._pageCache;
      }
    }

    /// <summary>
    ///   Gets the current forum Context (helper reference)
    /// </summary>
    public YafContext PageContext
    {
      get
      {
        return this.PageContext();
      }
    }

    /// <summary>
    ///   Gets or sets RefreshTime.
    /// </summary>
    public int RefreshTime { get; set; }

    /// <summary>
    ///   Adds a message that is displayed to the user when the page is loaded.
    /// </summary>
    public string RefreshURL { get; set; }

    /// <summary>
    ///   Gets or sets a value indicating whether ShowFooter.
    /// </summary>
    public bool ShowFooter
    {
      get
      {
        return this._showFooter;
      }

      protected set
      {
        this._showFooter = value;
      }
    }

    /// <summary>
    ///   Set to false if you don't want the menus at top and bottom. Only admin pages will set this to false
    /// </summary>
    public bool ShowToolBar
    {
      get
      {
        return this._showToolBar;
      }

      protected set
      {
        this._showToolBar = value;
      }
    }

    /// <summary>
    ///   Gets TopPageControl.
    /// </summary>
    public Control TopPageControl
    {
      get
      {
        if (this._topPageControl == null)
        {
          if (this.Page != null && this.Page.Header != null)
          {
            this._topPageControl = this.Page.Header;
          }
          else
          {
            this._topPageControl = this.FindControlRecursiveBoth("YafHead") ?? this.ForumTopControl;
          }
        }

        return this._topPageControl;
      }
    }

    /// <summary>
    ///   Gets the current user.
    /// </summary>
    public MembershipUser User
    {
      get
      {
        return this.PageContext.User;
      }
    }

    /// <summary>
    ///   Set to <see langword = "true" /> if this is the start page. Should only be set by the page that initialized the database.
    /// </summary>
    protected bool NoDataBase
    {
      set
      {
        this._noDataBase = value;
      }
    }

    #endregion

    #region IRaiseControlLifeCycles

    /// <summary>
    /// The raise init.
    /// </summary>
    void IRaiseControlLifeCycles.RaiseInit()
    {
      this.OnInit(new EventArgs());
    }

    /// <summary>
    /// The raise load.
    /// </summary>
    void IRaiseControlLifeCycles.RaiseLoad()
    {
      this.OnLoad(new EventArgs());
    }

    /// <summary>
    /// The raise pre render.
    /// </summary>
    void IRaiseControlLifeCycles.RaisePreRender()
    {
      this.OnPreRender(new EventArgs());
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// The is null.
    /// </summary>
    /// <param name="value">
    /// The value.
    /// </param>
    /// <returns>
    /// The is null.
    /// </returns>
    public static object IsNull(string value)
    {
      if (value == null || value.ToLower() == string.Empty)
      {
        return DBNull.Value;
      }
      else
      {
        return value;
      }
    }

    /// <summary>
    /// Gets the collapsible panel image url (expanded or collapsed). 
    ///   </summary>
    /// <param name="panelID">
    /// ID of collapsible panel
    /// </param>
    /// <param name="defaultState">
    /// Default Panel State
    /// </param>
    /// <returns>
    /// Image URL
    /// </returns>
    public string GetCollapsiblePanelImageURL(string panelID, CollapsiblePanelState defaultState)
    {
      return this.PageContext.Get<ITheme>().GetCollapsiblePanelImageURL(panelID, defaultState);
    }

    /// <summary>
    /// The get text.
    /// </summary>
    /// <param name="text">
    /// The text.
    /// </param>
    /// <returns>
    /// The get text.
    /// </returns>
    public string GetText(string text)
    {
      return this.PageContext.Localization.GetText(text);
    }

    /// <summary>
    /// The get text.
    /// </summary>
    /// <param name="page">
    /// The page.
    /// </param>
    /// <param name="text">
    /// The text.
    /// </param>
    /// <returns>
    /// The get text.
    /// </returns>
    public string GetText(string page, string text)
    {
      return this.PageContext.Localization.GetText(page, text);
    }

    /// <summary>
    /// The get text formatted.
    /// </summary>
    /// <param name="text">
    /// The text.
    /// </param>
    /// <param name="args">
    /// The args.
    /// </param>
    /// <returns>
    /// The get text formatted.
    /// </returns>
    public string GetTextFormatted(string text, params object[] args)
    {
      return this.PageContext.Localization.GetTextFormatted(text, args);
    }

    /// <summary>
    /// Get a value from the currently configured forum theme.
    /// </summary>
    /// <param name="page">
    /// Page to look under
    /// </param>
    /// <param name="tag">
    /// Theme item
    /// </param>
    /// <returns>
    /// Converted Theme information
    /// </returns>
    public string GetThemeContents(string page, string tag)
    {
      return this.PageContext.Get<ITheme>().GetItem(page, tag);
    }

    /// <summary>
    /// Get a value from the currently configured forum theme.
    /// </summary>
    /// <param name="page">
    /// Page to look under
    /// </param>
    /// <param name="tag">
    /// Theme item
    /// </param>
    /// <param name="defaultValue">
    /// Value to return if the theme item doesn't exist
    /// </param>
    /// <returns>
    /// Converted Theme information or Default Value if it doesn't exist
    /// </returns>
    public string GetThemeContents(string page, string tag, string defaultValue)
    {
      return this.PageContext.Get<ITheme>().GetItem(page, tag, defaultValue);
    }

    /// <summary>
    /// The html encode.
    /// </summary>
    /// <param name="data">
    /// The data.
    /// </param>
    /// <returns>
    /// The html encode.
    /// </returns>
    public string HtmlEncode(object data)
    {
      if (data == null || !(data is string))
      {
        return null;
      }

      return this.Server.HtmlEncode(data.ToString());
    }

    /// <summary>
    /// The redirect no access.
    /// </summary>
    public void RedirectNoAccess()
    {
      YafContext.Current.Get<IPermissions>().HandleRequest(ViewPermissions.RegisteredUsers);
    }

    #endregion

    #region Methods

    /// <summary>
    /// The create page links.
    /// </summary>
    protected virtual void CreatePageLinks()
    {
      // Page link creation goes to this method (overloads in descendant classes)
    }

    /// <summary>
    /// The insert css refresh.
    /// </summary>
    /// <param name="addTo">
    /// The add to.
    /// </param>
    protected void InsertCssRefresh(Control addTo)
    {
      // make the style sheet link controls.
      addTo.Controls.Add(ControlHelper.MakeCssIncludeControl(YafForumInfo.GetURLToResource("css/forum.css")));
      addTo.Controls.Add(ControlHelper.MakeCssIncludeControl(YafContext.Current.Theme.BuildThemePath("theme.css")));

      if (this.RefreshURL != null && this.RefreshTime >= 0)
      {
        var refresh = new HtmlMeta
          { HttpEquiv = "Refresh", Content = "{1};url={0}".FormatWith(this.RefreshURL, this.RefreshTime) };

        addTo.Controls.Add(refresh);
      }
    }


    protected override void OnPreRender(EventArgs e)
    {
        YafContext.Current.PageElements.RegisterJQuery();
        base.OnPreRender(e);
    }
    /// <summary>
    /// Writes the document
    /// </summary>
    /// <param name="writer">
    /// </param>
    protected override void Render(HtmlTextWriter writer)
    {
      base.Render(writer);

      // handle additional rendering if desired...
      if (this.ForumPageRendered != null)
      {
        this.ForumPageRendered(this, new ForumPageRenderedArgs(writer));
      }
    }

    /// <summary>
    /// The setup header elements.
    /// </summary>
    protected void SetupHeaderElements()
    {
      this.InsertCssRefresh(this.TopPageControl);
      string themeHeader = this.PageContext.Get<ITheme>().GetItem("THEME", "HEADER", string.Empty);

      if (themeHeader.IsSet())
      {
        var themeLiterial = new Literal();
        themeLiterial.Text = themeHeader.Replace("~", this.PageContext.Get<ITheme>().ThemeDir);
        TopPageControl.Controls.Add(themeLiterial);
      }
    }

    /// <summary>
    /// The forum page_ error.
    /// </summary>
    /// <param name="sender">
    /// The sender.
    /// </param>
    /// <param name="e">
    /// The e.
    /// </param>
    private void ForumPage_Error(object sender, EventArgs e)
    {
      // This doesn't seem to work...
      Exception x = this.Server.GetLastError();
      DB.eventlog_create(this.PageContext.PageUserID, this, x);

      // if (!YafForumInfo.IsLocal)
      // {
      // CreateMail.CreateLogEmail(this.Server.GetLastError());
      // }
    }

    /// <summary>
    /// Called first to initialize the context
    /// </summary>
    /// <param name="sender">
    /// </param>
    /// <param name="e">
    /// </param>
    private void ForumPage_Init(object sender, EventArgs e)
    {
      // fire init event...
      YafContext.Current.ForumPageInit(this, new EventArgs());

      if (this._noDataBase)
      {
        return;
      }

#if DEBUG
      QueryCounter.Reset();
#endif

      // set the current translation page...
      YafContext.Current.Get<LocalizationProvider>().TranslationPage = this._transPage;

      // fire preload event...
      YafContext.Current.ForumPagePreLoad(this, new EventArgs());
    }

    /// <summary>
    /// Called when page is loaded
    /// </summary>
    /// <param name="sender">
    /// </param>
    /// <param name="e">
    /// </param>
    private void ForumPage_Load(object sender, EventArgs e)
    {
      if (this.PageContext.BoardSettings.DoUrlReferrerSecurityCheck)
      {
        Security.CheckRequestValidity(this.Request);
      }
    }

    /// <summary>
    /// The forum page_ pre render.
    /// </summary>
    /// <param name="sender">
    /// The sender.
    /// </param>
    /// <param name="e">
    /// The e.
    /// </param>
    private void ForumPage_PreRender(object sender, EventArgs e)
    {
      // sets up the head elements in addition to the Css and image elements
      this.SetupHeaderElements();

      // setup the forum control header & footer properties
      this.ForumHeader.Visible = this.ShowToolBar;
      this.ForumFooter.Visible = this.ShowFooter;
    }

    /// <summary>
    /// Called when the page is unloaded
    /// </summary>
    /// <param name="sender">
    /// </param>
    /// <param name="e">
    /// </param>
    private void ForumPage_Unload(object sender, EventArgs e)
    {
      // release cache
      if (this._pageCache != null)
      {
        this._pageCache.Clear();
      }
    }

    #endregion

    #region Implementation of IHaveServiceLocator

    /// <summary>
    /// Gets ServiceLocator.
    /// </summary>
    public IServiceLocator ServiceLocator
    {
      get
      {
        return this.PageContext().ServiceLocator;
      }
    }

    #endregion
  }
}