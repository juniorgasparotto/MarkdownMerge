﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Html2Markdown.Replacement;

namespace Html2Markdown
{
	/// <summary>
	/// An Html to Markdown converter.
	/// </summary>
	public class Converter
	{
		private readonly IList<IReplacer> _replacers = new List<IReplacer>
		{
			new PatternReplacer
			{
				Pattern = @"</?(strong|b)>",
				Replacement = @"**"
			},
			new PatternReplacer
			{
				Pattern = @"</?(em|i)>",
				Replacement = @"*"
			},
			new PatternReplacer
			{
				Pattern = @"</h[1-6]>",
				Replacement = Environment.NewLine + Environment.NewLine
			},
			new PatternReplacer
			{
				Pattern = @"<h1[^>]*>",
				Replacement = Environment.NewLine + Environment.NewLine + "# "
			},
			new PatternReplacer
			{
				Pattern = @"<h2[^>]*>",
				Replacement = Environment.NewLine + Environment.NewLine + "## "
			},
			new PatternReplacer
			{
				Pattern = @"<h3[^>]*>",
				Replacement = Environment.NewLine + Environment.NewLine + "### "
			},
			new PatternReplacer
			{
				Pattern = @"<h4[^>]*>",
				Replacement = Environment.NewLine + Environment.NewLine + "#### "
			},
			new PatternReplacer
			{
				Pattern = @"<h5[^>]*>",
				Replacement = Environment.NewLine + Environment.NewLine + "##### "
			},
			new PatternReplacer
			{
				Pattern = @"<h6[^>]*>",
				Replacement = Environment.NewLine + Environment.NewLine + "###### "
			},
			new PatternReplacer
			{
				Pattern = @"<hr[^>]*>",
				Replacement = Environment.NewLine + Environment.NewLine + "* * *" + Environment.NewLine
			},
			new PatternReplacer
			{
				Pattern = @"<!DOCTYPE[^>]*>",
				Replacement = ""
			},
			new PatternReplacer
			{
				Pattern = @"</?html[^>]*>",
				Replacement = ""
			},
			new PatternReplacer
			{
				Pattern = @"</?head[^>]*>",
				Replacement = ""
			},
			new PatternReplacer
			{
				Pattern = @"</?body[^>]*>",
				Replacement = ""
			},
			new PatternReplacer
			{
				Pattern = @"<title[^>]*>.*?</title>",
				Replacement = ""
			},
			new PatternReplacer
			{
				Pattern = @"<meta[^>]*>",
				Replacement = ""
			},
			new PatternReplacer
			{
				Pattern = @"<link[^>]*>",
				Replacement = ""
			},
			new PatternReplacer
			{
				Pattern = @"<!--[^-]+-->",
				Replacement = ""
			},
			new CustomReplacer
			{
				CustomAction = HtmlParser.ReplaceImg
			},
			new CustomReplacer
			{
				CustomAction = HtmlParser.ReplaceLists
			},
			new CustomReplacer
			{
				CustomAction = HtmlParser.ReplaceAnchor
			},
			new CustomReplacer
			{
				CustomAction = HtmlParser.ReplaceCode
			},
			new CustomReplacer
			{
				CustomAction = HtmlParser.ReplacePre
			},
			new CustomReplacer
			{
				CustomAction = HtmlParser.ReplaceParagraph
			},
			new PatternReplacer
			{
				Pattern = @"<br[^>]*>",
				Replacement = @"  " + Environment.NewLine
			},
			new CustomReplacer
			{
				CustomAction = HtmlParser.ReplaceBlockquote
			},
			new CustomReplacer
			{
				CustomAction = HtmlParser.ReplaceEntites
			}
		};

		/// <summary>
		/// Converts Html contained in a file to a Markdown string
		/// </summary>
		/// <param name="path">The path to the file which is being converted</param>
		/// <returns>A Markdown representation of the passed in Html</returns>
		public string ConvertFile(string path)
		{
			using (var reader = new StreamReader(path))
			{
				var html = reader.ReadToEnd();
				html = StandardiseWhitespace(html);
				return Convert(html);
			}
		}

		private static string StandardiseWhitespace(string html)
		{
			return Regex.Replace(html, @"([^\r])\n", "$1\r\n");
		}

		/// <summary>
		/// Converts an Html string to a Markdown string
		/// </summary>
		/// <param name="html">The Html string you wish to convert</param>
		/// <returns>A Markdown representation of the passed in Html</returns>
		public string Convert(string html)
		{
			return CleanWhiteSpace(_replacers.Aggregate(html, (current, element) => element.Replace(current)));
		}

		private static string CleanWhiteSpace(string markdown)
		{
            var cleaned = Regex.Replace(markdown, @"\r", "");
            cleaned = Regex.Replace(cleaned, @"\n", "\r\n");
            cleaned = Regex.Replace(cleaned, @"\r\n\s+\r\n", "\r\n\r\n");
            cleaned = Regex.Replace(cleaned, @"(\r\n){3,}", "\r\n\r\n");
			cleaned = Regex.Replace(cleaned, @"(> \r\n){2,}", "> \r\n");
            cleaned = Regex.Replace(cleaned, @"^(\r\n)+", "");
			cleaned = Regex.Replace(cleaned, @"(\r\n)+$", "");

            // add break line to code blocks when not exists, add 2 if not exists
            // [add here] ```code
            var countCodeDelimiter = 0;
			cleaned = Regex.Replace(cleaned, @"```", f =>
            {
                var retValue = f.Value;

                if (countCodeDelimiter % 2 == 0)
                { 
                    var lastChar = cleaned.ElementAtOrDefault(f.Index - 1);
                    var lastChar2 = cleaned.ElementAtOrDefault(f.Index - 2);
                    var lastChar3 = cleaned.ElementAtOrDefault(f.Index - 3);

                    if (lastChar != '\n')
                        retValue = "\r\n\r\n" + f.Value;

                    if (lastChar == '\n' && lastChar2 == '\r' && lastChar3 != '\n')
                        retValue = "\r\n" + f.Value;
                }

                countCodeDelimiter++;
                return retValue;
            });

			return cleaned;
		}
	}
}
