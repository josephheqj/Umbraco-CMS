﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using Umbraco.Core.Logging;

namespace Umbraco.Core.Services.Implement
{
    // TODO: Convert all of this over to Niels K's localization framework one day

    public class LocalizedTextService :  ILocalizedTextService2
    {
        private readonly ILogger _logger;
        private readonly Lazy<LocalizedTextServiceFileSources> _fileSources;
        private readonly IDictionary<CultureInfo, IDictionary<string, IDictionary<string, string>>> _dictionarySource;
        private readonly IDictionary<CultureInfo, IDictionary<string, string>> _noAreaDictionarySource;
        private readonly char[] _splitter = new[] { '/' };
        /// <summary>
        /// Initializes with a file sources instance
        /// </summary>
        /// <param name="fileSources"></param>
        /// <param name="logger"></param>
        public LocalizedTextService(Lazy<LocalizedTextServiceFileSources> fileSources, ILogger logger)
        {
            if (logger == null) throw new ArgumentNullException("logger");
            _logger = logger;
            if (fileSources == null) throw new ArgumentNullException("fileSources");
            var dictionaries = FileSourcesToDictionarySources(fileSources.Value);
            _dictionarySource = dictionaries.WithArea;
            _noAreaDictionarySource = dictionaries.WithoutArea;
            _fileSources = fileSources;
        }

        private (IDictionary<CultureInfo, IDictionary<string, IDictionary<string, string>>> WithArea, IDictionary<CultureInfo, IDictionary<string, string>> WithoutArea) FileSourcesToDictionarySources(LocalizedTextServiceFileSources fileSources)
        {
            var xmlSources = fileSources.GetXmlSources();
            return XmlSourcesToDictionarySources(xmlSources);
        }
        private (IDictionary<CultureInfo, IDictionary<string, IDictionary<string, string>>> WithArea, IDictionary<CultureInfo, IDictionary<string, string>> WithoutArea) XmlSourcesToDictionarySources(IDictionary<CultureInfo, Lazy<XDocument>> source)
        {
            var cultureDictionary = new Dictionary<CultureInfo, IDictionary<string, IDictionary<string, string>>>();
            var cultureNoAreaDictionary = new Dictionary<CultureInfo, IDictionary<string, string>>();
            foreach (var xmlSource in source)
            {
                var areaAliaValue = GetAreaStoredTranslations(source, xmlSource.Key);
                cultureDictionary.Add(xmlSource.Key, areaAliaValue);
                var aliasValue = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
                foreach (var area in areaAliaValue)
                {
                    foreach (var alias in area.Value)
                    {
                        if (!aliasValue.ContainsKey(alias.Key))
                        {
                            aliasValue.Add(alias.Key, alias.Value);
                        }
                    }
                }
                cultureNoAreaDictionary.Add(xmlSource.Key, aliasValue);
            }
            return (cultureDictionary, cultureNoAreaDictionary);
        }
        /// <summary>
        /// Initializes with an XML source
        /// </summary>
        /// <param name="source"></param>
        /// <param name="logger"></param>
        public LocalizedTextService(IDictionary<CultureInfo, Lazy<XDocument>> source, ILogger logger)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (logger == null) throw new ArgumentNullException("logger");
            _logger = logger;
            var dictionaries = XmlSourcesToDictionarySources(source);
            _dictionarySource = dictionaries.WithArea;
            _noAreaDictionarySource = dictionaries.WithoutArea;
        }


        /// <summary>
        /// Initializes with a source of a dictionary of culture -> areas -> sub dictionary of keys/values
        /// </summary>
        /// <param name="source"></param>
        /// <param name="logger"></param>
        public LocalizedTextService(IDictionary<CultureInfo, IDictionary<string, IDictionary<string, string>>> source, ILogger logger)
        {
            _dictionarySource = source ?? throw new ArgumentNullException(nameof(source));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            var cultureNoAreaDictionary = new Dictionary<CultureInfo, IDictionary<string, string>>();
            foreach (var cultureDictionary in _dictionarySource)
            {
                var areaAliaValue = GetAreaStoredTranslations(source, cultureDictionary.Key);
                var aliasValue = new Dictionary<string, string>();
                foreach (var area in areaAliaValue)
                {
                    foreach (var alias in area.Value)
                    {
                        if (!aliasValue.ContainsKey(alias.Key))
                        {
                            aliasValue.Add(alias.Key, alias.Value);
                        }
                    }
                }
                cultureNoAreaDictionary.Add(cultureDictionary.Key, aliasValue);
            }
            _noAreaDictionarySource = cultureNoAreaDictionary;
        }

        public string Localize(string key, CultureInfo culture, IDictionary<string, string> tokens = null)
        {
            if (culture == null) throw new ArgumentNullException(nameof(culture));
            var keyParts = key.Split(_splitter, StringSplitOptions.RemoveEmptyEntries);
            var area = keyParts.Length > 1 ? keyParts[0] : null;
            var alias = keyParts.Length > 1 ? keyParts[1] : keyParts[0];
            return Localize(area, alias, culture, tokens);
        }
        public string Localize(string area, string alias, CultureInfo culture, IDictionary<string, string> tokens = null)
        {
            if (culture == null) throw new ArgumentNullException(nameof(culture));
                // TODO: Hack, see notes on ConvertToSupportedCultureWithRegionCode
                culture = ConvertToSupportedCultureWithRegionCode(culture);

                return GetFromDictionarySource(culture, area, alias, tokens);
        }

        /// <summary>
        /// Returns all key/values in storage for the given culture
        /// </summary>
        /// <returns></returns>
        public IDictionary<string, string> GetAllStoredValues(CultureInfo culture)
        {
            if (culture == null) throw new ArgumentNullException("culture");

            // TODO: Hack, see notes on ConvertToSupportedCultureWithRegionCode
            culture = ConvertToSupportedCultureWithRegionCode(culture);

            var result = new Dictionary<string, string>();


            if (_dictionarySource.ContainsKey(culture) == false)
            {
                _logger.Warn<LocalizedTextService>("The culture specified {Culture} was not found in any configured sources for this service", culture);
                return result;
            }

            //convert all areas + keys to a single key with a '/'
            foreach (var area in _dictionarySource[culture])
            {
                foreach (var key in area.Value)
                {
                    var dictionaryKey = string.Format("{0}/{1}", area.Key, key.Key);
                    //i don't think it's possible to have duplicates because we're dealing with a dictionary in the first place, but we'll double check here just in case.
                    if (result.ContainsKey(dictionaryKey) == false)
                    {
                        result.Add(dictionaryKey, key.Value);
                    }
                }
            }

            return result;
        }

        private Dictionary<string, IDictionary<string, string>> GetAreaStoredTranslations(IDictionary<CultureInfo, Lazy<XDocument>> xmlSource, CultureInfo cult)
        {
            var overallResult = new Dictionary<string, IDictionary<string, string>>(StringComparer.InvariantCultureIgnoreCase);
            var areas = xmlSource[cult].Value.XPathSelectElements("//area");
            foreach (var area in areas)
            {
                var result = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
                var keys = area.XPathSelectElements("./key");
                foreach (var key in keys)
                {
                    var dictionaryKey =
                        (string)key.Attribute("alias");
                    //there could be duplicates if the language file isn't formatted nicely - which is probably the case for quite a few lang files
                    if (result.ContainsKey(dictionaryKey) == false)
                        result.Add(dictionaryKey, key.Value);
                }
                overallResult.Add(area.Attribute("alias").Value, result);
            }
            return overallResult;
        }
        private Dictionary<string, IDictionary<string, string>> GetAreaStoredTranslations(IDictionary<CultureInfo, IDictionary<string, IDictionary<string, string>>> dictionarySource, CultureInfo cult)
        {
            var overallResult = new Dictionary<string, IDictionary<string, string>>(StringComparer.InvariantCultureIgnoreCase);
            var areaDict = dictionarySource[cult];

            foreach (var area in areaDict)
            {
                var result = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
                var keys = area.Value.Keys;
                foreach (var key in keys)
                {
                    //there could be duplicates if the language file isn't formatted nicely - which is probably the case for quite a few lang files
                    if (result.ContainsKey(key) == false)
                        result.Add(key, area.Value[key]);
                }
                overallResult.Add(area.Key, result);
            }
            return overallResult;
        }

        /// <summary>
        /// Returns a list of all currently supported cultures
        /// </summary>
        /// <returns></returns>
        public IEnumerable<CultureInfo> GetSupportedCultures()
        {
            return _dictionarySource.Keys;
        }

        /// <summary>
        /// Tries to resolve a full 4 letter culture from a 2 letter culture name
        /// </summary>
        /// <param name="currentCulture">
        /// The culture to determine if it is only a 2 letter culture, if so we'll try to convert it, otherwise it will just be returned
        /// </param>
        /// <returns></returns>
        /// <remarks>
        /// TODO: This is just a hack due to the way we store the language files, they should be stored with 4 letters since that
        /// is what they reference but they are stored with 2, further more our user's languages are stored with 2. So this attempts
        /// to resolve the full culture if possible.
        ///
        /// This only works when this service is constructed with the LocalizedTextServiceFileSources
        /// </remarks>
        public CultureInfo ConvertToSupportedCultureWithRegionCode(CultureInfo currentCulture)
        {
            if (currentCulture == null) throw new ArgumentNullException("currentCulture");

            if (_fileSources == null) return currentCulture;
            if (currentCulture.Name.Length > 2) return currentCulture;

            var attempt = _fileSources.Value.TryConvert2LetterCultureTo4Letter(currentCulture.TwoLetterISOLanguageName);
            return attempt ? attempt.Result : currentCulture;
        }

        private string GetFromDictionarySource(CultureInfo culture, string area, string key, IDictionary<string, string> tokens)
        {
            if (_dictionarySource.ContainsKey(culture) == false)
            {
                _logger.Warn<LocalizedTextService>("The culture specified {Culture} was not found in any configured sources for this service", culture);
                return "[" + key + "]";
            }


            string found = null;
            if (string.IsNullOrWhiteSpace(area))
            {
                _noAreaDictionarySource[culture].TryGetValue(key, out found);
            }
            else
            {
                if (_dictionarySource[culture].TryGetValue(area, out var areaDictionary))
                {
                    areaDictionary.TryGetValue(key, out found);
                }
            }


            if (found != null)
            {
                return ParseTokens(found, tokens);
            }

            //NOTE: Based on how legacy works, the default text does not contain the area, just the key
            return "[" + key + "]";
        }
        /// <summary>
        /// Parses the tokens in the value
        /// </summary>
        /// <param name="value"></param>
        /// <param name="tokens"></param>
        /// <returns></returns>
        /// <remarks>
        /// This is based on how the legacy ui localized text worked, each token was just a sequential value delimited with a % symbol.
        /// For example: hello %0%, you are %1% !
        ///
        /// Since we're going to continue using the same language files for now, the token system needs to remain the same. With our new service
        /// we support a dictionary which means in the future we can really have any sort of token system.
        /// Currently though, the token key's will need to be an integer and sequential - though we aren't going to throw exceptions if that is not the case.
        /// </remarks>
        internal static string ParseTokens(string value, IDictionary<string, string> tokens)
        {
            if (tokens == null || tokens.Any() == false)
            {
                return value;
            }

            foreach (var token in tokens)
            {
                value = value.Replace(string.Concat("%", token.Key, "%"), token.Value);
            }

            return value;
        }

    }
}
