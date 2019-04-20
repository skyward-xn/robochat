using CodeKicker.BBCode;
using CodeKicker.BBCode.SyntaxTree;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Interactivity;
using System.Windows.Media;
using System.Windows.Controls;
using System.Text.RegularExpressions;
using System.ComponentModel;
using System.Windows.Media.Imaging;
using System.Web;
using RoboChat.Behaviors;

namespace RoboChat.Model
{
    public static class Parser
    {
        private static BBCodeParser _parser;
        private static BBCodeParser ParserInstance
        {
            get
            {
                if (_parser == null)
                {
                    var tags = new[] {
                        new BBTag( "b", "", "" ),
                        new BBTag( "i", "", "" ),
                        new BBTag( "u", "", "" ),
                        new BBTag( "color", "", "", new BBAttribute( "color", "" ) ),
                        new BBTag( "smile", "", "", new BBAttribute( "smile", "" ) ),
                        new BBTag( "img", "", "", new BBAttribute( "img", "" ) ),
                        new BBTag( "link", "", "", new BBAttribute( "link", "" ) ),
                        new BBTag( "url", "", "" ),
                        new BBTag( "br", "", "" ),
                        new BBTag( "quote", "", "", new BBAttribute( "quote", "" ) )
                    };

                    _parser = new BBCodeParser(ErrorMode.ErrorFree, "", tags);
                }

                return _parser;
            }
        }

        public static void Parse(Span span, string messageText, ref List<Image> images)
        {
            var rootNode = ParserInstance.ParseSyntaxTree(messageText);
            ProcessNode(span, rootNode, ref images);
        }

        private static void ProcessNode(Span parent, SyntaxTreeNode node, ref List<Image> images)
        {
            foreach (SyntaxTreeNode child in node.SubNodes)
            {
                if (child is TextNode)
                {
                    string text = child.ToText();
                    string newText = ReplaceSmileys(text);
                    newText = ReplaceLinks(newText);
                    if (newText != text)
                    {
                        Span span = new Span();
                        parent.Inlines.Add(span);

                        var rootNode = ParserInstance.ParseSyntaxTree(newText);
                        ProcessNode(span, rootNode, ref images);
                    }
                    else
                    {
                        parent.Inlines.Add(new Run(text));
                    }
                    continue;
                }

                string tagName = ((TagNode)child).Tag.Name.ToLower();
                switch (tagName)
                {
                    case "br":
                        parent.Inlines.Add(new LineBreak());
                        break;

                    case "url":
                        // Попытаемся отобразить ссылку
                        try
                        {
                            string hyperlinkText = child.ToText();
                            if (string.IsNullOrEmpty(hyperlinkText))
                                break;

                            Hyperlink hyperlink = new Hyperlink();
                            hyperlink.Inlines.Add(new Run(hyperlinkText));
                            hyperlink.NavigateUri = new Uri(hyperlinkText);
                            Interaction.GetBehaviors(hyperlink).Add(new HyperlinkBehavior());
                            parent.Inlines.Add(hyperlink);
                        }
                        catch
                        {
                            // Молча продолжаем
                        }
                        break;

                    case "link":
                        // Попытаемся отобразить ссылку
                        try
                        {
                            string hyperlinkLink = GetAttr((TagNode)child, "");
                            Hyperlink hyperlink = new Hyperlink();
                            ProcessNode(hyperlink, child, ref images);
                            hyperlink.NavigateUri = new Uri(hyperlinkLink);
                            Interaction.GetBehaviors(hyperlink).Add(new HyperlinkBehavior());
                            parent.Inlines.Add(hyperlink);
                        }
                        catch
                        {
                            // Молча продолжаем
                        }
                        break;

                    case "smile":
                        // Попытаемся отобразить смайл
                        try
                        {
                            string imageText = child.ToText();
                            if (!string.IsNullOrEmpty(imageText))
                            {
                                Image image = new Image();
                                image.Stretch = Stretch.None;

                                // Зарезервируем место под изображения, но потратим время на дополнительную предварительную загрузку
                                var bitmap = BitmapFrame.Create(new Uri(Path.Combine(Settings.SmileysLocation, imageText)),
                                    BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                                image.Width = bitmap.Width;
                                image.Height = bitmap.Height;

                                XamlAnimatedGif.AnimationBehavior.SetAutoStart(image, Settings.Instance.EnableAnimation);
                                XamlAnimatedGif.AnimationBehavior.SetSourceUri(image,
                                    new Uri(Path.Combine(Settings.SmileysLocation, imageText)));

                                parent.Inlines.Add(image);
                                images.Add(image);
                            }
                        }
                        catch
                        {
                            // Молча продолжаем
                        }
                        break;

                    case "img":
                        // Попытаемся отобразить картинку
                        try
                        {
                            string imageText = child.ToText();
                            if (!string.IsNullOrEmpty(imageText))
                            {
                                // Добавим обработку кастомных относительных адресов
                                // В будущем желательно вынести в плагин
                                imageText = HttpUtility.UrlDecode(imageText);
                                imageText = imageText.Replace("robochat://", "file:///" + Settings.Directory.Replace('\\', '/'));

                                Image image = new Image();
                                image.StretchDirection = StretchDirection.DownOnly;
                                image.Source = BitmapFrame.Create(new Uri(imageText, UriKind.RelativeOrAbsolute),
                                    BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                                parent.Inlines.Add(image);
                                images.Add(image);
                            }
                        }
                        catch
                        {
                            // Молча продолжаем
                        }
                        break;

                    case "b":
                    case "i":
                    case "u":
                        var inlineSpan = ProcessTag(tagName);
                        ProcessNode(inlineSpan, child, ref images);
                        parent.Inlines.Add(inlineSpan);
                        break;

                    case "color":
                        var colorSpan = new Span();
                        ProcessNode(colorSpan, child, ref images);

                        // Попытаемся преобразовать цвет
                        try
                        {
                            string attrText = GetAttr((TagNode)child, "");
                            var color = (Color)ColorConverter.ConvertFromString(attrText);
                            var brush = new SolidColorBrush(color);
                            colorSpan.Foreground = brush;
                        }
                        catch
                        {
                            // Молча продолжаем
                        }

                        parent.Inlines.Add(colorSpan);
                        break;

                    case "quote":
                        var quotedSpan = new Span();
                        ProcessNode(quotedSpan, child, ref images);

                        string quotedInfo = GetAttr((TagNode)child, "");
                        if (quotedInfo == null)
                            break;

                        // Блок Figure может идти только со второй строчки
                        parent.Inlines.Add(new Run(quotedInfo + ":") { FontSize = Settings.Instance.FontSize - 2 });

                        parent.Inlines.Add(new Figure(new Paragraph(quotedSpan))
                        {
                            BorderBrush = new SolidColorBrush(Colors.Black),
                            BorderThickness = new Thickness(2, 0, 0, 0),
                            HorizontalAnchor = FigureHorizontalAnchor.ContentLeft,
                            Padding = new Thickness(5, 2, 0, 5),
                            Margin = new Thickness(0),
                            WrapDirection = WrapDirection.Left
                        });
                        parent.Inlines.Add(new LineBreak());
                        break;
                }
            }
        }

        private static string GetAttr(TagNode child, string attrName)
        {
            try
            {
                string tagName = child.Tag.Name.ToLower();

                var attr = ParserInstance.Tags.First(p => p.Name == tagName).FindAttribute("");
                return child.AttributeValues[attr].Trim('"');
            }
            catch
            {
                return null;
            }
        }

        private static Span ProcessTag(string tagName)
        {
            switch (tagName)
            {
                case "b":
                    return new Bold();
                case "i":
                    return new Italic();
                case "u":
                    return new Underline();
                default:
                    return new Span();
            }
        }

        private static string ReplaceSmileys(string text)
        {
            if (Settings.Instance.EnableSmileys)
            {
                foreach (var smiley in Settings.SmileysReplacer)
                    text = text.Replace(smiley.Key, GetTag(Tags.Smile, smiley.Value));
            }

            return text;
        }

        private static string ReplaceLinks(string text)
        {
            // Автоматически будем заменять только HTTP, HTTPS и FTP
            var words = from w in Regex.Split(text, "\\s")
                        where Uri.IsWellFormedUriString(w, UriKind.Absolute)
                        let url = new Uri(w, UriKind.Absolute)
                        where (url.Scheme == Uri.UriSchemeHttp || url.Scheme == Uri.UriSchemeHttps ||
                        url.Scheme == Uri.UriSchemeFtp)
                        // Идем по убыванию длины, чтобы избежать замены части адреса
                        // на встреченный ранее более короткий адрес
                        orderby w.Length descending
                        select w;

            foreach (var w in words)
                text = text.Replace(w, GetTag(Tags.Url, w));

            return text;
        }

        public static string GetTag(Tags tag, string param = "", string attr = "")
        {
            switch (tag)
            {
                case Tags.Bold:
                    return string.Format("[b]{0}[/b]", param);

                case Tags.Italic:
                    return string.Format("[i]{0}[/i]", param);

                case Tags.Underline:
                    return string.Format("[u]{0}[/u]", param);

                case Tags.Smile:
                    return string.Format("[smile]{0}[/smile]", param);

                case Tags.Image:
                    return string.Format("[img]{0}[/img]", param);

                case Tags.Url:
                    // Если планируется отобразить пустой тег, то проверим буфер обмена
                    // и если среди разбитых по пробельным символам кусков будут урлы, то возьмем первый
                    if (string.IsNullOrEmpty(param) && System.Windows.Clipboard.ContainsText())
                    {
                        // Автоматически будем подставлять все, что похоже на ссылку
                        var urls = from w in Regex.Split(System.Windows.Clipboard.GetText(), "\\s")
                                   where Uri.IsWellFormedUriString(w, UriKind.Absolute)
                                   select w;

                        string url = urls.FirstOrDefault();
                        if (url != null)
                            param = url;
                    }

                    return string.Format("[url]{0}[/url]", param);

                case Tags.Newline:
                    return "[br][/br]";

                case Tags.Color:
                    if (!string.IsNullOrEmpty(attr))
                        return string.Format("[color=\"{1}\"]{0}[/color]", param, attr);

                    return param;

                case Tags.Quote:
                    return string.Format("[quote=\"{1}\"]{0}[/quote]", param, attr);

                default:
                    throw new NotSupportedException("Unsupported tag");
            }
        }

        public static string GetTag(string tagName, string param = "", string attr = "")
        {
            var enumConverter = new EnumConverter(typeof(Tags));
            var tag = (Tags)enumConverter.ConvertFromString(tagName);
            return GetTag(tag, param, attr);
        }
    }
}
