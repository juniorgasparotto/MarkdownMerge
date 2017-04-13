﻿using MarkdownMerge.Translation;
using MarkdownMerge.Xml.Tags;
using System.Collections.Generic;
using HtmlAgilityPack;
using Html2Markdown.Replacement;
using Html2Markdown;
using Markdig;
using System.Linq;
using System;
using MarkdownMerge.Helpers;

namespace MarkdownMerge.Xml
{
    public class Version
    {
        private HtmlDocument XContent { get; set; }
        private Dictionary<string, HtmlNode> NodesSpecials { get; } = new Dictionary<string, HtmlNode>();

        public Page Page { get; private set; }
        public Language Language { get; private set; }
        public string OriginalMarkdown { get; private set; }
        public List<NodeBase> Nodes { get; } = new List<NodeBase>();

        public Version(Page page, Language lang, string markdown)
        {
            Page = page;
            Page.Versions.Add(this);

            Language = lang;
            OriginalMarkdown = markdown;
            Compile();
        }

        public void Compile()
        {
            string html, markdown;

            // remove as tags especiais que são agrupadores: "<no-translate>", "<custom-translate",
            // pois a conversão para markdown não resolve strings markdown dentro de tags
            // e também para que o translate não precise traduzir esses elementos que por padrão,
            // não podem ser traduzidos.
            markdown = RemoveSpecialGroupsElements(OriginalMarkdown);

            // converte para markdown sem as tags que agrupadoras
            html = MarkdownToHtml(markdown);

            // traduz o HTML
            html = TranslateHtml(html, Language.Name);

            // cria o objeto HTML que será base daqui para frente
            XContent = HtmlParser.GetHtmlDocument(html);

            // volta as tags especiais que são agrupadores, pois já passou a faze 
            // de conversão para markdown e tradução.
            BackSpecialGroupElements();

            // troca todos os custom-translate pelo conteúdo dessa versão
            ReplaceCustomLanguage();

            // troca todos os no-translate pelo seu próprio conteúdo
            ReplaceNoTranslate();

            // compila as tags especiais.
            CompileSpecialElements();
        }

        private string RemoveSpecialGroupsElements(string html)
        {
            var htmlObj = HtmlParser.GetHtmlDocument(html);

            // remove spaces in no-translatin itens
            var noTranslations = htmlObj.DocumentNode.SelectNodes("//" + ElementNamesConstants.NoTranslation);
            if (noTranslations != null)
            {
                foreach (var no in noTranslations)
                {
                    var guid = Guid.NewGuid().ToString();
                    NodesSpecials.Add(guid, no);
                    HtmlParser.ReplaceNode(no, $"<p idreplace=\"{guid}\" />");
                }
            }

            // add notranslate attr in custom translations
            var customTransaltions = htmlObj.DocumentNode.SelectNodes("//" + ElementNamesConstants.CustomTranslation);
            if (customTransaltions != null)
            {
                if (customTransaltions != null)
                { 
                    foreach (var custom in customTransaltions)
                    {
                        var guid = Guid.NewGuid().ToString();
                        NodesSpecials.Add(guid, custom);
                        HtmlParser.ReplaceNode(custom, $"<p idreplace=\"{guid}\" />");
                    }
                }
            }

            return htmlObj.DocumentNode.OuterHtml;
        }

        private void BackSpecialGroupElements()
        {
            var replaces = XContent.DocumentNode.SelectNodes("//p[@idreplace]");
            if (replaces != null)
            {
                foreach (var replace in replaces)
                {
                    var node = NodesSpecials[replace.Attributes["idreplace"].Value];
                    HtmlParser.ReplaceNode(replace, node.OuterHtml);
                }
            }
        }

        private void ReplaceCustomLanguage()
        {
            var elements = XContent.DocumentNode.SelectNodes("//" + ElementNamesConstants.CustomTranslation);

            if (elements == null)
                return;

            foreach (var element in elements)
            {
                if (this == Page.GetDefaultVersion())
                {
                    var original = element.Element(ElementNamesConstants.CustomTranslationDefault);
                    string html = ConvertMarkdownNodeToHtml(original);
                    HtmlParser.ReplaceNode(element, html);
                }
                else
                {
                    foreach (var lang in element.Elements(ElementNamesConstants.CustomTranslationLanguage))
                    {
                        if (lang.Attributes["name"].Value == this.Language.Name)
                        {
                            string html = ConvertMarkdownNodeToHtml(lang);
                            HtmlParser.ReplaceNode(element, html);
                            break;
                        }
                    }
                }
            }
        }

        private void ReplaceNoTranslate()
        {
            var elements = XContent.DocumentNode.SelectNodes("//" + ElementNamesConstants.NoTranslation);
            if (elements == null)
                return;

            foreach (var element in elements)
            {
                string html = ConvertMarkdownNodeToHtml(element);
                HtmlParser.ReplaceNode(element, html);
            }
        }
        
        #region Compile - Step1

        private void CompileSpecialElements()
        {
            CompileElements(XContent.DocumentNode.SelectNodes("//" + ElementNamesConstants.HeaderSet)?.ToArray());
            CompileElements(XContent.DocumentNode.SelectNodes("//" + ElementNamesConstants.AnchorSet)?.ToArray());
            CompileElements(XContent.DocumentNode.SelectNodes("//" + ElementNamesConstants.AnchorGet)?.ToArray());
            CompileElements(XContent.DocumentNode.SelectNodes("//" + ElementNamesConstants.TableOfContents)?.ToArray());
        }

        
        private void CompileElements(IEnumerable<HtmlNode> elements)
        {
            if (elements == null)
                return;

            foreach (var element in elements)
            {
                switch (element.Name)
                {
                    case ElementNamesConstants.AnchorSet:
                        new AnchorSet(this, element);
                        break;
                    case ElementNamesConstants.AnchorGet:
                        new AnchorGet(this, element);
                        break;
                    case ElementNamesConstants.HeaderSet:
                        new HeaderSet(this, element);
                        break;
                    case ElementNamesConstants.TableOfContents:
                        new TableOfContents(this, element);
                        break;
                }
            }
        }

        #endregion

        #region Generate final markdown - Step2

        public string GetMarkdown()
        {
            foreach (var node in Nodes)
                node.ReplaceToMarkdown();

            var content = HtmlToMarkdown(this.XContent.DocumentNode.OuterHtml);
            return content;
        }

        #endregion

        #region Aux

        private string HtmlToMarkdown(string html)
        {
            var converter = new Converter();
            return converter.Convert(html);
        }

        private string MarkdownToHtml(string markdown)
        {
            return Markdown.ToHtml(markdown);
        }

        private string TranslateHtml(string html, string toLang)
        {
            return html;
            var fromLang = Page.GetDefaultVersion().Language.Name;
            if (fromLang != toLang)
                return Translator.Translate(html, fromLang, toLang);
            return html;
        }

        private static string ConvertMarkdownNodeToHtml(HtmlNode element)
        {
            var markdown = StringHelper.TrimAllLines(element.InnerHtml);
            var html = Markdown.ToHtml(markdown);
            return html;
        }

        #endregion
    }
}
