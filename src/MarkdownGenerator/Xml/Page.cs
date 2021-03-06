﻿using MarkdownGenerator.Xml.Tags;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SysCommand.ConsoleApp.Helpers;
using MarkdownGenerator.Xml.Extensions;
using HtmlAgilityPack;
using Html2Markdown.Replacement;
using System;

namespace MarkdownGenerator.Xml
{
    public partial class Page
    {
        public List<Version> Versions { get; } = new List<Version>();
        public Documentation Documentation { get; private set; }

        public Page(Documentation doc, XElement xpage)
        {
            this.Documentation = doc;
            this.ParseVersions(xpage);
        }

        private void ParseVersions(XElement xpage)
        {
            var content = XContent.GetContent(xpage);

            var languages = xpage.Elements("languages");
            if (languages != null)
            {
                var langs = languages.First().Elements("language").ToArray();
                if (langs.Length > 1 && Documentation.Translator == null)
                    throw new Exception("If you use more than one 'languages pages versions' is necessary input the parameter: --translator-key [value generated by azure]");

                foreach (var xlang in langs)
                {
                    var lang = new Language()
                    {
                        Name = xlang.Attribute("name").Value,
                        UrlBase = xlang.Attribute("url-base")?.Value,
                        Output = xlang.Attribute("output").Value,
                        IsDefault = xlang.Attribute("default")?.Value == "true"
                    };

                    new Version(this, lang, content);
                }
            }
        }

        public Version GetDefaultVersion()
        {
            return this.Versions.First(f => f.Language.IsDefault);
        }

        public void Save()
        {
            foreach (var version in Versions)
                FileHelper.SaveContentToFile(version.GetMarkdown(), version.Language.Output);
        }
    }
}
