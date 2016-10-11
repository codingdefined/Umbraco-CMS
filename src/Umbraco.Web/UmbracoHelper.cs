﻿using System;
using System.ComponentModel;
using System.Web;
using System.Web.Security;
using System.Xml.XPath;
using Umbraco.Core;
using Umbraco.Core.Dictionary;
using Umbraco.Core.Security;
using Umbraco.Core.Services;
using Umbraco.Core.Xml;
using Umbraco.Web.Routing;
using Umbraco.Web.Security;
using System.Collections.Generic;
using System.IO;
using System.Web.Mvc;
using Umbraco.Core.Cache;
using Umbraco.Core.Models.PublishedContent;

namespace Umbraco.Web
{
    /// <summary>
	/// A helper class that provides many useful methods and functionality for using Umbraco in templates
	/// </summary>
	public class UmbracoHelper : IUmbracoComponentRenderer
    {
        private static readonly HtmlStringUtilities StringUtilities = new HtmlStringUtilities();

        private readonly UmbracoContext _umbracoContext;
		private readonly IPublishedContent _currentPage;
        private readonly IPublishedContentQuery _iQuery;
        private readonly ServiceContext _services;
        private readonly CacheHelper _appCache;

        private IUmbracoComponentRenderer _componentRenderer;
        private PublishedContentQuery _query;
        private MembershipHelper _membershipHelper;
        private ITagQuery _tag;
        private IDataTypeService _dataTypeService;
        private UrlProvider _urlProvider;
        private ICultureDictionary _cultureDictionary;

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="UmbracoHelper"/> class.
        /// </summary>
        /// <remarks>For tests.</remarks>
        internal UmbracoHelper(UmbracoContext umbracoContext, IPublishedContent content,
            IPublishedContentQuery query,
            ITagQuery tagQuery,
            IDataTypeService dataTypeService,
            UrlProvider urlProvider,
            ICultureDictionary cultureDictionary,
            IUmbracoComponentRenderer componentRenderer,
            MembershipHelper membershipHelper,
            ServiceContext services,
            CacheHelper appCache)
        {
            if (umbracoContext == null) throw new ArgumentNullException(nameof(umbracoContext));
            if (content == null) throw new ArgumentNullException(nameof(content));
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (tagQuery == null) throw new ArgumentNullException(nameof(tagQuery));
            if (dataTypeService == null) throw new ArgumentNullException(nameof(dataTypeService));
            if (urlProvider == null) throw new ArgumentNullException(nameof(urlProvider));
            if (cultureDictionary == null) throw new ArgumentNullException(nameof(cultureDictionary));
            if (componentRenderer == null) throw new ArgumentNullException(nameof(componentRenderer));
            if (membershipHelper == null) throw new ArgumentNullException(nameof(membershipHelper));
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (appCache == null) throw new ArgumentNullException(nameof(appCache));

            _umbracoContext = umbracoContext;
            _tag = new TagQuery(tagQuery);
            _dataTypeService = dataTypeService;
            _urlProvider = urlProvider;
            _cultureDictionary = cultureDictionary;
            _componentRenderer = componentRenderer;
            _membershipHelper = membershipHelper;
            _currentPage = content;
            _iQuery = query;
            _services = services;
            _appCache = appCache;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UmbracoHelper"/> class.
        /// </summary>
        /// <remarks>For tests - nothing is initialized.</remarks>
        internal UmbracoHelper()
        { }


        /// <summary>
        /// Initializes a new instance of the <see cref="UmbracoHelper"/> class with an Umbraco context
        /// and a specific content item.
        /// </summary>
        /// <param name="umbracoContext">An Umbraco context.</param>
        /// <param name="content">A content item.</param>
        /// <param name="services">A services context.</param>
        /// <param name="appCache">An application cache helper.</param>
        /// <remarks>Sets the current page to the supplied content item.</remarks>
        public UmbracoHelper(UmbracoContext umbracoContext, ServiceContext services, CacheHelper appCache, IPublishedContent content)
            : this(umbracoContext, services, appCache)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));
            _currentPage = content;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UmbracoHelper"/> class with an Umbraco context.
        /// </summary>
        /// <param name="umbracoContext">An Umbraco context.</param>
        /// <param name="services">A services context.</param>
        /// <param name="appCache">An application cache helper.</param>
        /// <remarks>Sets the current page to the context's published content request's content item.</remarks>
        public UmbracoHelper(UmbracoContext umbracoContext, ServiceContext services, CacheHelper appCache)
        {
            if (umbracoContext == null) throw new ArgumentNullException(nameof(umbracoContext));
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (appCache == null) throw new ArgumentNullException(nameof(appCache));

            _umbracoContext = umbracoContext;
            if (_umbracoContext.IsFrontEndUmbracoRequest)
                _currentPage = _umbracoContext.PublishedContentRequest.PublishedContent;
            _services = services;
            _appCache = appCache;
        }

        #endregion

        /// <summary>
        /// Gets the tag context.
        /// </summary>
        public ITagQuery TagQuery => _tag ??
            (_tag = new TagQuery(_services.TagService, _iQuery ?? ContentQuery));

        /// <summary>
        /// Gets the query context.
        /// </summary>
        public PublishedContentQuery ContentQuery => _query ??
            (_query = _iQuery != null
                ? new PublishedContentQuery(_iQuery)
                : new PublishedContentQuery(UmbracoContext.ContentCache, UmbracoContext.MediaCache));

        /// <summary>
        /// Gets the Umbraco context.
        /// </summary>
        public UmbracoContext UmbracoContext
        {
            get
            {
                if (_umbracoContext == null)
                    throw new NullReferenceException("UmbracoContext has not been set.");
                return _umbracoContext;
            }
        }

        /// <summary>
        /// Gets the membership helper.
        /// </summary>
        public MembershipHelper MembershipHelper => _membershipHelper 
            ?? (_membershipHelper = new MembershipHelper(UmbracoContext));

        /// <summary>
        /// Gets the url provider.
        /// </summary>
        public UrlProvider UrlProvider => _urlProvider 
            ?? (_urlProvider = UmbracoContext.UrlProvider);

        /// <summary>
        /// Gets the datatype service.
        /// </summary>
        private IDataTypeService DataTypeService => _dataTypeService 
            ?? (_dataTypeService = _services.DataTypeService);

        /// <summary>
        /// Gets the component renderer.
        /// </summary>
        public IUmbracoComponentRenderer UmbracoComponentRenderer => _componentRenderer 
            ?? (_componentRenderer = new UmbracoComponentRenderer(UmbracoContext));

        /// <summary>
        /// Returns the current IPublishedContent item assigned to the UmbracoHelper
        /// </summary>
        /// <remarks>
        /// Note that this is the assigned IPublishedContent item to the UmbracoHelper, this is not necessarily the Current IPublishedContent item
        /// being rendered. This IPublishedContent object is contextual to the current UmbracoHelper instance.
        ///
        /// In some cases accessing this property will throw an exception if there is not IPublishedContent assigned to the Helper
        /// this will only ever happen if the Helper is constructed with an UmbracoContext and it is not a front-end request
        /// </remarks>
        /// <exception cref="InvalidOperationException">Thrown if the UmbracoHelper is constructed with an UmbracoContext and it is not a front-end request</exception>
	    public IPublishedContent AssignedContentItem
	    {
	        get
	        {
	            if (_currentPage == null)
                    throw new InvalidOperationException("Cannot return the " + typeof(IPublishedContent).Name + " because the " + typeof(UmbracoHelper).Name + " was constructed with an " + typeof(UmbracoContext).Name + " and the current request is not a front-end request.");

                return _currentPage;
	        }
	    }

	    /// <summary>
		/// Renders the template for the specified pageId and an optional altTemplateId.
		/// </summary>
		/// <param name="pageId"></param>
		/// <param name="altTemplateId">If not specified, will use the template assigned to the node</param>
		/// <returns></returns>
		public IHtmlString RenderTemplate(int pageId, int? altTemplateId = null)
	    {
            return UmbracoComponentRenderer.RenderTemplate(pageId, altTemplateId);
	    }

		#region RenderMacro

		/// <summary>
		/// Renders the macro with the specified alias.
		/// </summary>
		/// <param name="alias">The alias.</param>
		/// <returns></returns>
		public IHtmlString RenderMacro(string alias)
		{
            return UmbracoComponentRenderer.RenderMacro(alias, new { });
		}

		/// <summary>
		/// Renders the macro with the specified alias, passing in the specified parameters.
		/// </summary>
		/// <param name="alias">The alias.</param>
		/// <param name="parameters">The parameters.</param>
		/// <returns></returns>
		public IHtmlString RenderMacro(string alias, object parameters)
		{
            return UmbracoComponentRenderer.RenderMacro(alias, parameters.ToDictionary<object>());
		}

		/// <summary>
		/// Renders the macro with the specified alias, passing in the specified parameters.
		/// </summary>
		/// <param name="alias">The alias.</param>
		/// <param name="parameters">The parameters.</param>
		/// <returns></returns>
		public IHtmlString RenderMacro(string alias, IDictionary<string, object> parameters)
		{
            return UmbracoComponentRenderer.RenderMacro(alias, parameters);
		}

		#endregion

		#region Dictionary

		/// <summary>
		/// Returns the dictionary value for the key specified
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public string GetDictionaryValue(string key)
		{
            return CultureDictionary[key];
		}

        /// <summary>
        /// Returns the ICultureDictionary for access to dictionary items
        /// </summary>
        public ICultureDictionary CultureDictionary => _cultureDictionary 
            ?? (_cultureDictionary = Current.CultureDictionaryFactory.CreateDictionary());

        #endregion

		#region Membership

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("Use the IsProtected method that only specifies path")]
        public bool IsProtected(int documentId, string path)
		{
            return IsProtected(path.EnsureEndsWith("," + documentId));
		}

        /// <summary>
        /// Check if a document object is protected by the "Protect Pages" functionality in umbraco
        /// </summary>
        /// <param name="path">The full path of the document object to check</param>
        /// <returns>True if the document object is protected</returns>
        public bool IsProtected(string path)
        {
            return _services.PublicAccessService.IsProtected(path);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use the MemberHasAccess method that only specifies path")]
        public bool MemberHasAccess(int nodeId, string path)
        {
            return MemberHasAccess(path.EnsureEndsWith("," + nodeId));
        }

        /// <summary>
        /// Check if the current user has access to a document
        /// </summary>
        /// <param name="path">The full path of the document object to check</param>
        /// <returns>True if the current user has access or if the current document isn't protected</returns>
        public bool MemberHasAccess(string path)
        {
            if (IsProtected(path))
            {
                return MembershipHelper.IsLoggedIn()
                       && _services.PublicAccessService.HasAccess(path, GetCurrentMember(), Roles.Provider);
            }
            return true;
        }

        /// <summary>
        /// Gets (or adds) the current member from the current request cache
        /// </summary>
        private MembershipUser GetCurrentMember()
        {
            return _appCache.RequestCache.GetCacheItem<MembershipUser>("UmbracoHelper.GetCurrentMember", () =>
            {
                var provider = Core.Security.MembershipProviderExtensions.GetMembersMembershipProvider();
                return provider.GetCurrentUser();
            });
        }

		/// <summary>
		/// Whether or not the current member is logged in (based on the membership provider)
		/// </summary>
		/// <returns>True is the current user is logged in</returns>
		public bool MemberIsLoggedOn()
		{
            return MembershipHelper.IsLoggedIn();
		}

		#endregion

		#region NiceUrls

		/// <summary>
		/// Returns a string with a friendly url from a node.
		/// IE.: Instead of having /482 (id) as an url, you can have
		/// /screenshots/developer/macros (spoken url)
		/// </summary>
		/// <param name="nodeId">Identifier for the node that should be returned</param>
		/// <returns>String with a friendly url from a node</returns>
		public string NiceUrl(int nodeId)
		{
		    return Url(nodeId);
		}

        /// <summary>
        /// Gets the url of a content identified by its identifier.
        /// </summary>
        /// <param name="contentId">The content identifier.</param>
        /// <returns>The url for the content.</returns>
        public string Url(int contentId)
        {
            return UrlProvider.GetUrl(contentId);
        }

        /// <summary>
        /// Gets the url of a content identified by its identifier, in a specified mode.
        /// </summary>
        /// <param name="contentId">The content identifier.</param>
        /// <param name="mode">The mode.</param>
        /// <returns>The url for the content.</returns>
	    public string Url(int contentId, UrlProviderMode mode)
	    {
            return UrlProvider.GetUrl(contentId, mode);
	    }

		/// <summary>
		/// This method will always add the domain to the path if the hostnames are set up correctly.
		/// </summary>
		/// <param name="nodeId">Identifier for the node that should be returned</param>
		/// <returns>String with a friendly url with full domain from a node</returns>
		public string NiceUrlWithDomain(int nodeId)
		{
		    return UrlAbsolute(nodeId);
		}

        /// <summary>
        /// Gets the absolute url of a content identified by its identifier.
        /// </summary>
        /// <param name="contentId">The content identifier.</param>
        /// <returns>The absolute url for the content.</returns>
        public string UrlAbsolute(int contentId)
        {
            return UrlProvider.GetUrl(contentId, true);
        }

		#endregion

        #region Members

        public IPublishedContent Member(object id)
        {
            var asInt = id.TryConvertTo<int>();
            return asInt ? MembershipHelper.GetById(asInt.Result) : MembershipHelper.GetByProviderKey(id);
        }

        public IPublishedContent Member(int id)
        {
            return MembershipHelper.GetById(id);
        }

        public IPublishedContent Member(string id)
        {
            var asInt = id.TryConvertTo<int>();
            return asInt ? MembershipHelper.GetById(asInt.Result) : MembershipHelper.GetByProviderKey(id);
        }

        #endregion

        #region Content

        public IPublishedContent Content(object id)
		{
		    int intId;
            return ConvertIdObjectToInt(id, out intId) ? ContentQuery.Content(intId) : null;
		}

		public IPublishedContent Content(int id)
		{
            return ContentQuery.Content(id);
		}

		public IPublishedContent Content(string id)
		{
            int intId;
            return ConvertIdObjectToInt(id, out intId) ? ContentQuery.Content(intId) : null;
		}

        public IPublishedContent ContentSingleAtXPath(string xpath, params XPathVariable[] vars)
        {
            return ContentQuery.ContentSingleAtXPath(xpath, vars);
        }

		public IEnumerable<IPublishedContent> Content(params object[] ids)
		{
            return ContentQuery.Content(ConvertIdsObjectToInts(ids));
		}

        /// <summary>
        /// Gets the contents corresponding to the identifiers.
        /// </summary>
        /// <param name="ids">The content identifiers.</param>
        /// <returns>The existing contents corresponding to the identifiers.</returns>
        /// <remarks>If an identifier does not match an existing content, it will be missing in the returned value.</remarks>
		public IEnumerable<IPublishedContent> Content(params int[] ids)
		{
            return ContentQuery.Content(ids);
		}

        /// <summary>
        /// Gets the contents corresponding to the identifiers.
        /// </summary>
        /// <param name="ids">The content identifiers.</param>
        /// <returns>The existing contents corresponding to the identifiers.</returns>
        /// <remarks>If an identifier does not match an existing content, it will be missing in the returned value.</remarks>
        public IEnumerable<IPublishedContent> Content(params string[] ids)
		{
            return ContentQuery.Content(ConvertIdsObjectToInts(ids));
		}

        /// <summary>
        /// Gets the contents corresponding to the identifiers.
        /// </summary>
        /// <param name="ids">The content identifiers.</param>
        /// <returns>The existing contents corresponding to the identifiers.</returns>
        /// <remarks>If an identifier does not match an existing content, it will be missing in the returned value.</remarks>
        public IEnumerable<IPublishedContent> Content(IEnumerable<object> ids)
		{
            return ContentQuery.Content(ConvertIdsObjectToInts(ids));
		}

        /// <summary>
        /// Gets the contents corresponding to the identifiers.
        /// </summary>
        /// <param name="ids">The content identifiers.</param>
        /// <returns>The existing contents corresponding to the identifiers.</returns>
        /// <remarks>If an identifier does not match an existing content, it will be missing in the returned value.</remarks>
        public IEnumerable<IPublishedContent> Content(IEnumerable<string> ids)
		{
            return ContentQuery.Content(ConvertIdsObjectToInts(ids));
		}

        /// <summary>
        /// Gets the contents corresponding to the identifiers.
        /// </summary>
        /// <param name="ids">The content identifiers.</param>
        /// <returns>The existing contents corresponding to the identifiers.</returns>
        /// <remarks>If an identifier does not match an existing content, it will be missing in the returned value.</remarks>
        public IEnumerable<IPublishedContent> Content(IEnumerable<int> ids)
		{
            return ContentQuery.Content(ids);
		}

        public IEnumerable<IPublishedContent> ContentAtXPath(string xpath, params XPathVariable[] vars)
        {
            return ContentQuery.ContentAtXPath(xpath, vars);
        }

        public IEnumerable<IPublishedContent> ContentAtXPath(XPathExpression xpath, params XPathVariable[] vars)
        {
            return ContentQuery.ContentAtXPath(xpath, vars);
        }

        public IEnumerable<IPublishedContent> ContentAtRoot()
        {
            return ContentQuery.ContentAtRoot();
        }

        private bool ConvertIdObjectToInt(object id, out int intId)
        {
            var s = id as string;
            if (s != null)
            {
                return int.TryParse(s, out intId);
            }

            if (id is int)
            {
                intId = (int) id;
                return true;
            }

            throw new InvalidOperationException("The value of parameter 'id' must be either a string or an integer");
        }

        private IEnumerable<int> ConvertIdsObjectToInts(IEnumerable<object> ids)
        {
            var list = new List<int>();
            foreach (var id in ids)
            {
                int intId;
                if (ConvertIdObjectToInt(id, out intId))
                {
                    list.Add(intId);
                }
            }
            return list;
        }

		#endregion

		#region Media

		/// <summary>
		/// Overloaded method accepting an 'object' type
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		/// <remarks>
		/// We accept an object type because GetPropertyValue now returns an 'object', we still want to allow people to pass
		/// this result in to this method.
		/// This method will throw an exception if the value is not of type int or string.
		/// </remarks>
		public IPublishedContent Media(object id)
		{
            int intId;
            return ConvertIdObjectToInt(id, out intId) ? ContentQuery.Media(intId) : null;
		}

		public IPublishedContent Media(int id)
		{
            return ContentQuery.Media(id);
		}

		public IPublishedContent Media(string id)
		{
            int intId;
            return ConvertIdObjectToInt(id, out intId) ? ContentQuery.Media(intId) : null;
		}

        /// <summary>
        /// Gets the medias corresponding to the identifiers.
        /// </summary>
        /// <param name="ids">The media identifiers.</param>
        /// <returns>The existing medias corresponding to the identifiers.</returns>
        /// <remarks>If an identifier does not match an existing media, it will be missing in the returned value.</remarks>
        public IEnumerable<IPublishedContent> Media(params object[] ids)
		{
            return ContentQuery.Media(ConvertIdsObjectToInts(ids));
		}

        /// <summary>
        /// Gets the medias corresponding to the identifiers.
        /// </summary>
        /// <param name="ids">The media identifiers.</param>
        /// <returns>The existing medias corresponding to the identifiers.</returns>
        /// <remarks>If an identifier does not match an existing media, it will be missing in the returned value.</remarks>
        public IEnumerable<IPublishedContent> Media(params int[] ids)
		{
            return ContentQuery.Media(ids);
		}

        /// <summary>
        /// Gets the medias corresponding to the identifiers.
        /// </summary>
        /// <param name="ids">The media identifiers.</param>
        /// <returns>The existing medias corresponding to the identifiers.</returns>
        /// <remarks>If an identifier does not match an existing media, it will be missing in the returned value.</remarks>
        public IEnumerable<IPublishedContent> Media(params string[] ids)
		{
            return ContentQuery.Media(ConvertIdsObjectToInts(ids));
		}

        /// <summary>
        /// Gets the medias corresponding to the identifiers.
        /// </summary>
        /// <param name="ids">The media identifiers.</param>
        /// <returns>The existing medias corresponding to the identifiers.</returns>
        /// <remarks>If an identifier does not match an existing media, it will be missing in the returned value.</remarks>
        public IEnumerable<IPublishedContent> Media(IEnumerable<object> ids)
		{
            return ContentQuery.Media(ConvertIdsObjectToInts(ids));
		}

        /// <summary>
        /// Gets the medias corresponding to the identifiers.
        /// </summary>
        /// <param name="ids">The media identifiers.</param>
        /// <returns>The existing medias corresponding to the identifiers.</returns>
        /// <remarks>If an identifier does not match an existing media, it will be missing in the returned value.</remarks>
        public IEnumerable<IPublishedContent> Media(IEnumerable<int> ids)
		{
            return ContentQuery.Media(ids);
		}

        /// <summary>
        /// Gets the medias corresponding to the identifiers.
        /// </summary>
        /// <param name="ids">The media identifiers.</param>
        /// <returns>The existing medias corresponding to the identifiers.</returns>
        /// <remarks>If an identifier does not match an existing media, it will be missing in the returned value.</remarks>
        public IEnumerable<IPublishedContent> Media(IEnumerable<string> ids)
		{
            return ContentQuery.Media(ConvertIdsObjectToInts(ids));
		}

        public IEnumerable<IPublishedContent> MediaAtRoot()
        {
            return ContentQuery.MediaAtRoot();
        }

		#endregion

		#region Search

		/// <summary>
		/// Searches content
		/// </summary>
		/// <param name="term"></param>
		/// <param name="useWildCards"></param>
		/// <param name="searchProvider"></param>
		/// <returns></returns>
		public IEnumerable<IPublishedContent> Search(string term, bool useWildCards = true, string searchProvider = null)
		{
            return ContentQuery.Search(term, useWildCards, searchProvider);
		}

		/// <summary>
		/// Searhes content
		/// </summary>
		/// <param name="criteria"></param>
		/// <param name="searchProvider"></param>
		/// <returns></returns>
		public IEnumerable<IPublishedContent> Search(Examine.SearchCriteria.ISearchCriteria criteria, Examine.Providers.BaseSearchProvider searchProvider = null)
		{
            return ContentQuery.Search(criteria, searchProvider);
		}

		#endregion

		#region Strings

		/// <summary>
		/// Replaces text line breaks with html line breaks
		/// </summary>
		/// <param name="text">The text.</param>
		/// <returns>The text with text line breaks replaced with html linebreaks (<br/>)</returns>
		public string ReplaceLineBreaksForHtml(string text)
		{
            return StringUtilities.ReplaceLineBreaksForHtml(text);
		}

		/// <summary>
		/// Returns an MD5 hash of the string specified
		/// </summary>
		/// <param name="text">The text to create a hash from</param>
		/// <returns>Md5 has of the string</returns>
		public string CreateMd5Hash(string text)
		{
			return text.ToMd5();
		}

		/// <summary>
		/// Strips all html tags from a given string, all contents of the tags will remain.
		/// </summary>
		public HtmlString StripHtml(IHtmlString html, params string[] tags)
		{
			return StripHtml(html.ToHtmlString(), tags);
		}

		/// <summary>
		/// Strips all html tags from a given string, all contents of the tags will remain.
		/// </summary>
		public HtmlString StripHtml(string html, params string[] tags)
		{
            return StringUtilities.StripHtmlTags(html, tags);
		}

		/// <summary>
		/// Will take the first non-null value in the collection and return the value of it.
		/// </summary>
		public string Coalesce(params object[] args)
		{
            return StringUtilities.Coalesce(args);
		}

		/// <summary>
		/// Will take the first non-null value in the collection and return the value of it.
		/// </summary>
		public string Concatenate(params object[] args)
		{
            return StringUtilities.Concatenate(args);
		}

		/// <summary>
		/// Joins any number of int/string/objects into one string and seperates them with the string seperator parameter.
		/// </summary>
		public string Join(string separator, params object[] args)
		{
            return StringUtilities.Join(separator, args);
		}

		/// <summary>
		/// Truncates a string to a given length, can add a elipsis at the end (...). Method checks for open html tags, and makes sure to close them
		/// </summary>
		public IHtmlString Truncate(IHtmlString html, int length)
		{
			return Truncate(html.ToHtmlString(), length, true, false);
		}

		/// <summary>
		/// Truncates a string to a given length, can add a elipsis at the end (...). Method checks for open html tags, and makes sure to close them
		/// </summary>
		public IHtmlString Truncate(IHtmlString html, int length, bool addElipsis)
		{
			return Truncate(html.ToHtmlString(), length, addElipsis, false);
		}

		/// <summary>
		/// Truncates a string to a given length, can add a elipsis at the end (...). Method checks for open html tags, and makes sure to close them
		/// </summary>
		public IHtmlString Truncate(IHtmlString html, int length, bool addElipsis, bool treatTagsAsContent)
		{
			return Truncate(html.ToHtmlString(), length, addElipsis, treatTagsAsContent);
		}

		/// <summary>
		/// Truncates a string to a given length, can add a elipsis at the end (...). Method checks for open html tags, and makes sure to close them
		/// </summary>
		public IHtmlString Truncate(string html, int length)
		{
			return Truncate(html, length, true, false);
		}

		/// <summary>
		/// Truncates a string to a given length, can add a elipsis at the end (...). Method checks for open html tags, and makes sure to close them
		/// </summary>
		public IHtmlString Truncate(string html, int length, bool addElipsis)
		{
			return Truncate(html, length, addElipsis, false);
		}

		/// <summary>
		/// Truncates a string to a given length, can add a elipsis at the end (...). Method checks for open html tags, and makes sure to close them
		/// </summary>
		public IHtmlString Truncate(string html, int length, bool addElipsis, bool treatTagsAsContent)
		{
            return StringUtilities.Truncate(html, length, addElipsis, treatTagsAsContent);
		}


		#endregion

		#region If

		/// <summary>
		/// If the test is true, the string valueIfTrue will be returned, otherwise the valueIfFalse will be returned.
		/// </summary>
		public HtmlString If(bool test, string valueIfTrue, string valueIfFalse)
		{
			return test ? new HtmlString(valueIfTrue) : new HtmlString(valueIfFalse);
		}

		/// <summary>
		/// If the test is true, the string valueIfTrue will be returned, otherwise the valueIfFalse will be returned.
		/// </summary>
		public HtmlString If(bool test, string valueIfTrue)
		{
			return test ? new HtmlString(valueIfTrue) : new HtmlString(string.Empty);
		}

		#endregion

        #region Prevalues

        public string GetPreValueAsString(int id)
        {
            return DataTypeService.GetPreValueAsString(id);
        }

        #endregion

        #region canvasdesigner

        [Obsolete("Use EnableCanvasDesigner on the HtmlHelper extensions instead")]
        public IHtmlString EnableCanvasDesigner()
        {
            return EnableCanvasDesigner(string.Empty, string.Empty);
        }

        [Obsolete("Use EnableCanvasDesigner on the HtmlHelper extensions instead")]
        public IHtmlString EnableCanvasDesigner(string canvasdesignerConfigPath)
        {
            return EnableCanvasDesigner(canvasdesignerConfigPath, string.Empty);
        }

        [Obsolete("Use EnableCanvasDesigner on the HtmlHelper extensions instead")]
        public IHtmlString EnableCanvasDesigner(string canvasdesignerConfigPath, string canvasdesignerPalettesPath)
        {
            var html = CreateHtmlHelper("");
            var urlHelper = new UrlHelper(UmbracoContext.HttpContext.Request.RequestContext);
            return html.EnableCanvasDesigner(urlHelper, UmbracoContext, canvasdesignerConfigPath, canvasdesignerPalettesPath);
        }

        [Obsolete("This shouldn't need to be used but because the obsolete extension methods above don't have access to the current HtmlHelper, we need to create a fake one, unfortunately however this will not pertain the current views viewdata, tempdata or model state so should not be used")]
        private HtmlHelper CreateHtmlHelper(object model)
        {
            var cc = new ControllerContext
            {
                RequestContext = UmbracoContext.HttpContext.Request.RequestContext
            };
            var viewContext = new ViewContext(cc, new FakeView(), new ViewDataDictionary(model), new TempDataDictionary(), new StringWriter());
            var htmlHelper = new HtmlHelper(viewContext, new ViewPage());
            return htmlHelper;
        }

        [Obsolete("This shouldn't need to be used but because the obsolete extension methods above don't have access to the current HtmlHelper, we need to create a fake one, unfortunately however this will not pertain the current views viewdata, tempdata or model state so should not be used")]
        private class FakeView : IView
        {
            public void Render(ViewContext viewContext, TextWriter writer)
            {
            }
        }

        #endregion

        /// <summary>
        /// This is used in methods like BeginUmbracoForm and SurfaceAction to generate an encrypted string which gets submitted in a request for which
        /// Umbraco can decrypt during the routing process in order to delegate the request to a specific MVC Controller.
        /// </summary>
        /// <param name="controllerName"></param>
        /// <param name="controllerAction"></param>
        /// <param name="area"></param>
        /// <param name="additionalRouteVals"></param>
        /// <returns></returns>
        internal static string CreateEncryptedRouteString(string controllerName, string controllerAction, string area, object additionalRouteVals = null)
        {
            Mandate.ParameterNotNullOrEmpty(controllerName, "controllerName");
            Mandate.ParameterNotNullOrEmpty(controllerAction, "controllerAction");
            Mandate.ParameterNotNull(area, "area");

            //need to create a params string as Base64 to put into our hidden field to use during the routes
            var surfaceRouteParams = $"c={HttpUtility.UrlEncode(controllerName)}&a={HttpUtility.UrlEncode(controllerAction)}&ar={area}";

            var additionalRouteValsAsQuery = additionalRouteVals?.ToDictionary<object>().ToQueryString();

            if (additionalRouteValsAsQuery.IsNullOrWhiteSpace() == false)
                surfaceRouteParams += "&" + additionalRouteValsAsQuery;

            return surfaceRouteParams.EncryptWithMachineKey();
        }

	}
}
