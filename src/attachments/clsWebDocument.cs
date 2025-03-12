using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using mshtml;
using System.Text.RegularExpressions;

namespace OSPAutoSearch_AutoLogin
{
    public class clsWebDocument
    {
        /*
         * Document에서 Node삭제방법
        var document = webMain.Document.DomDocument as HTMLDocument;

        if (document != null)
        {
            var childNode = document.getElementById("div_modals") as IHTMLDOMNode;

            if (childNode != null)
            {
                var parentNode = childNode.parentNode;

                parentNode.removeChild(childNode);
            }
        }
         * */

        //Element에서 값이 확인가능한 속성 => ID, NAME, TITLE, SRC, HREF, VALUE

        // 태그명, 속성의 값으로 대상 Element를 찾는다.
        public static bool setInnerText3(HtmlDocument doc, string strTag, string strValue, string strInnerText, bool isFocus = true)
        {
            if (doc == null) return false;
            if (strTag == null || strValue == null || strInnerText == null) return false;

            HtmlElementCollection elc = doc.GetElementsByTagName(strTag);
            if (elc == null) return false;

            foreach (HtmlElement el in elc)
            {
                if (el.OuterHtml.Contains(strValue))
                {
                    if (isFocus == true)
                    {
                        el.Focus();
                    }

                    el.InnerText = strInnerText;
                    return true;
                }
            }

            return false;
        }

        // 태그명, 속성의 값의 포함여부를 확인해서 대상 Element를 찾는다. (InnerText까지 비교가능)
        public static HtmlElement getElement3(HtmlDocument doc, string strTag, string strValue, string strInnerText = "")
        {
            if (doc == null) return null;
            if (strTag == null || strValue == null) return null;

            HtmlElementCollection elc = doc.GetElementsByTagName(strTag);
            if (elc == null) return null;

            foreach (HtmlElement el in elc)
            {
                string sTmp = el.OuterHtml;

                if (clsUtil.StringContain(sTmp, strValue) == true)
                {
                    if (strInnerText.Length > 0)
                    {
                        if (el.InnerText != null && String.Compare(Trim(el.InnerText), strInnerText, true) == 0)
                        {
                            return el;
                        }
                    }
                    else
                    {
                        return el;
                    }
                }
            }

            return null;
        }

        // 태그명, 속성의 값으로 대상 Element를 찾는다.
        public static bool setInnerText(HtmlDocument doc, string strTag, string strAttribute, string strValue, string strInnerText, bool isFocus = true)
        {
            if (doc == null) return false;
            if (strTag == null || strAttribute == null || strValue == null || strInnerText == null) return false;

            HtmlElementCollection elc = doc.GetElementsByTagName(strTag);
            if (elc == null) return false;

            foreach (HtmlElement el in elc)
            {
                if (String.Compare(el.GetAttribute(strAttribute), strValue, true) == 0)
                {
                    if (isFocus == true)
                    {
                        el.Focus();
                    }
                    el.InnerText = strInnerText;
                    return true;
                }
            }
            return false;
        }

        // 태그명, 속성의 값으로 대상 Element를 찾는다.
        public static bool setInnerText2(HtmlDocument doc, string strTag, string strAttribute, string strValue, string strInnerText, bool isFocus = true)
        {
            if (doc == null) return false;
            if (strTag == null || strAttribute == null || strValue == null || strInnerText == null) return false;

            HtmlElementCollection elc = doc.GetElementsByTagName(strTag);
            if (elc == null) return false;

            foreach (HtmlElement el in elc)
            {
                if (clsUtil.StringContain(el.GetAttribute(strAttribute), strValue) == true)
                {
                    if (isFocus == true)
                    {
                        el.Focus();
                    }

                    el.InnerText = strInnerText;
                    return true;
                }
            }
            return false;
        }
        //nNum번째꺼를 수정한다..
        public static bool setInnerText3(HtmlDocument doc, string strTag, string strAttribute, string strValue, string strInnerText,int nNum, bool isFocus = true)
        {
            if (doc == null) return false;
            if (strTag == null || strAttribute == null || strValue == null || strInnerText == null) return false;

            HtmlElementCollection elc = doc.GetElementsByTagName(strTag);
            if (elc == null) return false;
            int nCount = 0;
            foreach (HtmlElement el in elc)
            {
                if (String.Compare(el.GetAttribute(strAttribute), strValue, true) == 0)
                {
                    if (isFocus == true)
                    {
                        el.Focus();
                    }

                    nCount++;
                    if (nCount == nNum)
                    {
                        el.InnerText = strInnerText;
                        return true;
                    }                    
                }
            }
            return false;
        }

        // 태그명, 속성의 값으로 대상 Element를 찾는다.
        public static bool InvokeMember(HtmlDocument doc, string strTag, string strAttribute, string strValue, string strInnerText, string strFunction, bool isContains = true)
        {
            if (doc == null) return false;
            if (strTag == null || strAttribute == null || strValue == null || strFunction == null || strInnerText == null) return false;

            HtmlElementCollection elc = doc.GetElementsByTagName(strTag);
            if (elc == null) return false;

            foreach (HtmlElement el in elc)
            {
                if (isContains == true)
                {
                    if (clsUtil.StringContain(el.GetAttribute(strAttribute), strValue) == true)
                    {
                        if (strInnerText.Length > 0)
                        {
                            if (el.InnerText != null && String.Compare(Trim(el.InnerText), strInnerText, true) == 0)
                            {
                                el.InvokeMember(strFunction);
                                return true;
                            }
                        }
                        else
                        {
                            el.InvokeMember(strFunction);
                            return true;
                        }
                    }
                }
                else
                {
                    if (String.Compare(el.GetAttribute(strAttribute), strValue, true) == 0)
                    {
                        if (strInnerText.Length > 0)
                        {
                            if (el.InnerText != null && String.Compare(Trim(el.InnerText), strInnerText, true) == 0)
                            {
                                el.InvokeMember(strFunction);
                                return true;
                            }
                        }
                        else
                        {
                            el.InvokeMember(strFunction);
                            return true;
                        }
                    }
                }                
            }

            return false;
        }

        // 태그명, 속성의 값으로 대상 Element를 찾는다.
        public static bool InvokeMember(HtmlDocument doc, string strTag, string[] arrAttribute, string[] arrValue, string strFunction)
        {
            if (doc == null) return false;
            if (strTag == null || arrAttribute == null || arrValue == null || strFunction == null) return false;
            if (arrAttribute.Length != arrValue.Length) return false;

            HtmlElementCollection elc = doc.GetElementsByTagName(strTag);
            if (elc == null) return false;

            foreach (HtmlElement el in elc)
            {
                bool isSearch = false;
                for (int i = 0; i < arrAttribute.Length; i++)
                {
                    if (clsUtil.StringContain(el.GetAttribute(arrAttribute[i]), arrValue[i]) == true)
                    {
                        isSearch = true;
                    }
                    else
                    {
                        isSearch = false;
                        break;
                    }
                }

                if (isSearch == true)
                {
                    el.InvokeMember(strFunction);
                    return true;
                }
            }

            return false;
        }
        // 태그명, 속성의 값으로 대상 Element를 찾는다.
        public static bool InvokeMember2(HtmlDocument doc, string strTag, string strAttribute, string strValue, string strInnerText, string strFunction,int nNum, bool isContains = true)
        {
            if (doc == null) return false;
            if (strTag == null || strAttribute == null || strValue == null || strFunction == null || strInnerText == null) return false;

            HtmlElementCollection elc = doc.GetElementsByTagName(strTag);
            if (elc == null) return false;
            int nCount = 0;
            foreach (HtmlElement el in elc)
            {
                if (isContains == true)
                {
                    if (clsUtil.StringContain(el.GetAttribute(strAttribute), strValue) == true)
                    {
                        if (strInnerText.Length > 0)
                        {
                            if (el.InnerText != null && String.Compare(Trim(el.InnerText), strInnerText, true) == 0)
                            {
                                el.InvokeMember(strFunction);
                                return true;
                            }
                        }
                        else
                        {
                            nCount++;
                            if (nCount == nNum)
                            {
                                el.InvokeMember(strFunction);
                                return true;
                            }
                        }
                    }
                }
                else
                {
                    if (String.Compare(el.GetAttribute(strAttribute), strValue, true) == 0)
                    {
                        if (strInnerText.Length > 0)
                        {
                            if (el.InnerText != null && String.Compare(Trim(el.InnerText), strInnerText, true) == 0)
                            {
                                el.InvokeMember(strFunction);
                                return true;
                            }
                        }
                        else
                        {
                            el.InvokeMember(strFunction);
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        // 태그명, InnerText를 비교해서 대상 Element를 찾는다.
        public static HtmlElement getElement(HtmlDocument doc, string strTag, string strInnerText)
        {
            if (doc == null) return null;
            if (strTag == null || strInnerText == null) return null;

            HtmlElementCollection elc = doc.GetElementsByTagName(strTag);
            if (elc == null) return null;

            foreach (HtmlElement el in elc)
            {
                if (el.InnerText != null && String.Compare(Trim(el.InnerText), strInnerText, true) == 0)
                {
                    return el;
                }
            }

            return null;
        }

        // 태그명, 속성의 값을 비교해서 대상 Element를 찾는다. (InnerText까지 비교가능)
        public static HtmlElement getElement(HtmlDocument doc, string strTag, string strAttribute, string strValue, string strInnerText = "")
        {
            if (doc == null) return null;
            if (strTag == null || strAttribute == null || strValue == null) return null;

            HtmlElementCollection elc = doc.GetElementsByTagName(strTag);
            if (elc == null) return null;

            foreach (HtmlElement el in elc)
            {
                if (String.Compare(el.GetAttribute(strAttribute), strValue, true) == 0)
                {
                    if (strInnerText.Length > 0)
                    {
                        if (el.InnerText != null && String.Compare(Trim(el.InnerText), strInnerText, true) == 0)
                        {
                            return el;
                        }
                    }
                    else
                    {
                        return el;
                    }
                }
            }

            return null;
        }

        // 태그명, 속성의 값의 포함여부를 확인해서 대상 Element를 찾는다. (InnerText까지 비교가능)
        public static HtmlElement getElement2(HtmlDocument doc, string strTag, string strAttribute, string strValue, string strInnerText = "")
        {
            if (doc == null) return null;
            if (strTag == null || strAttribute == null || strValue == null) return null;

            HtmlElementCollection elc = doc.GetElementsByTagName(strTag);
            if (elc == null) return null;

            foreach (HtmlElement el in elc)
            {
                string sTmp = strAttribute == "style" ? el.Style : el.GetAttribute(strAttribute);

                if (clsUtil.StringContain(sTmp, strValue) == true)
                {
                    if (strInnerText.Length > 0)
                    {
                        if (el.InnerText != null && String.Compare(Trim(el.InnerText), strInnerText, true) == 0)
                        {
                            return el;
                        }
                    }
                    else
                    {
                        return el;
                    }
                }
            }

            return null;
        }

        // 태그명, 속성의 값을 비교해서 대상 Element를 찾는다. (InnerText까지 비교가능)
        public static HtmlElement getElement2(HtmlDocument doc, string strTag, string[] arrAttribute, string[] arrValue)
        {
            if (doc == null) return null;
            if (strTag == null || arrAttribute == null || arrValue == null) return null;

            HtmlElementCollection elc = doc.GetElementsByTagName(strTag);
            if (elc == null) return null;

            foreach (HtmlElement el in elc)
            {
                bool isSearch = false;
                for (int i = 0; i < arrAttribute.Length; i++)
                {
                    if (clsUtil.StringContain(el.GetAttribute(arrAttribute[i]), arrValue[i]) == true)
                    {
                        isSearch = true;
                    }
                    else
                    {
                        isSearch = false;
                        break;
                    }
                }

                if (isSearch == true)
                {
                    return el;
                }               
            }

            return null;
        }

        // Element의 속성값과 비교한다.
        public static bool isElement(HtmlElement el, string strTag, string strAttribute, string strValue)
        {
            if (el == null) return false;
            if (strTag == null || strAttribute == null || strValue == null) return false;

            if (String.Compare(el.TagName, strTag, true) == 0)
            {
                if (String.Compare(el.GetAttribute(strAttribute), strValue, true) == 0)
                {
                    return true;
                }
            }

            return false;
        }

        // Element의 속성값의 포함여부를 확인한다.
        public static bool isElement2(HtmlElement el, string strTag, string strAttribute, string strValue)
        {
            if (el == null) return false;
            if (strTag == null || strAttribute == null || strValue == null) return false;

            if (String.Compare(el.TagName, strTag, true) == 0)
            {
                if (clsUtil.StringContain(el.GetAttribute(strAttribute), strValue) == true)
                {
                    return true;
                }
            }

            return false;
        }

        // 지정태그에서 nParentIndex값만큼 상위태그를 검색한다.
        public static HtmlElement getParentElement(HtmlElement el, int nParentIndex)
        {
            if (el == null) return null;

            HtmlElement tempEl = el;
            for (int i = 0; i < nParentIndex; i++)
            {
                if (tempEl.Parent != null)
                {
                    tempEl = tempEl.Parent;
                }
                else
                {
                    return null;
                }
            }

            return tempEl;
        }

        // 지정태그에서 strParentTagName인 상위태그가 검색될때가지 검색한다.
        public static HtmlElement getParentElement(HtmlElement el, string strParentTagName)
        {
            if (el == null) return null;
            if (strParentTagName == null) return null;

            HtmlElement tempEl = el;
            while (tempEl != null)
            {
                tempEl = tempEl.Parent;

                if (tempEl != null)
                {
                    if (String.Compare(tempEl.TagName, strParentTagName, true) == 0)
                    {
                        break;
                    }
                }
            }

            return tempEl;
        }

        // 하위Element들중 InnerText가 동일한 Element를 검색
        public static HtmlElement getChildElement(HtmlElement el, string strInnerText)
        {
            if (el == null) return null;
            if (strInnerText == null) return null;

            HtmlElementCollection elc = el.Children;
            if (elc == null) return null;

            foreach (HtmlElement tempEl in elc)
            {
                if (String.Compare(Trim(tempEl.InnerText), strInnerText, true) == 0)
                {
                    return tempEl;
                }
            }

            return null;
        }

        // 하위Element들중 검색태그명과 일치하고 일치하는 Element중에서 몇번째 Element인지 검색
        public static HtmlElement getChildElement(HtmlElement el, string strTag, int nChildIndex)
        {
            if (el == null) return null;
            if (strTag == null) return null;

            HtmlElementCollection elc = el.Children;
            if (elc == null) return null;
            
            int i = 0;
            foreach (HtmlElement tempEl in elc)
            {
                if (String.Compare(tempEl.TagName, strTag, true) == 0)
                {
                    i++;
                    if (i >= nChildIndex)
                    {
                        return tempEl;
                    }
                }
            }

            return null;
        }

        // 하위Element들중 태그명, 속성의 값이 일치하는 Element검색
        public static HtmlElement getChildElement(HtmlElement el, string strTag, string strAttribute, string strValue)
        {
            if (el == null) return null;
            if (strTag == null || strAttribute == null || strValue == null) return null;

            HtmlElementCollection elc = el.Children;
            if (elc == null) return null;

            foreach (HtmlElement tempEl in elc)
            {
                if (isElement(tempEl, strTag, strAttribute, strValue) == true)
                {
                    return tempEl;
                }
            }

            return null;
        }

        // 하위Element들중 태그명, 속성의 값을 포함하는 Element검색
        public static HtmlElement getChildElement2(HtmlElement el, string strTag, string strAttribute, string strValue)
        {
            if (el == null) return null;
            if (strTag == null || strAttribute == null || strValue == null) return null;

            HtmlElementCollection elc = el.Children;
            if (elc == null) return null;

            foreach (HtmlElement tempEl in elc)
            {
                if (isElement2(tempEl, strTag, strAttribute, strValue) == true)
                {
                    return tempEl;
                }
            }

            return null;
        }

        public static bool setNoPopup(HtmlDocument doc)
        {
            if (doc == null) return false;

            try
            {
                HtmlElement headEI = doc.GetElementsByTagName("head")[0];
                if (headEI.InnerHtml.Contains("window.alert = function () { }; window.close = function () { }") == true)
                {
                    return true;
                }
                
                HtmlElement scriptEl = doc.CreateElement("script");
                IHTMLScriptElement domEl = (IHTMLScriptElement)scriptEl.DomElement;
                domEl.text = @"window.alert = function () { }; window.close = function () { };";
                headEI.AppendChild(scriptEl);
            }
            catch
            {
                return false;
            }

            return true;            
        }

        public static string Trim(HtmlAgilityPack.HtmlNode node)
        {
            if (node == null) return "";

            return Trim(node.InnerText);
        }

        public static string Trim(string strInnerText)
        {
            if (strInnerText == null) return "";

            string strTemp = strInnerText.Trim();            
            strTemp = strTemp.Replace("&nbsp;", " ");
            strTemp = strTemp.Replace("&lt;", "<");
            strTemp = strTemp.Replace("&gt;", ">");
            strTemp = strTemp.Replace("&amp;", "&");
            strTemp = strTemp.Replace("&quot;", "");
            strTemp = strTemp.Replace("\t", "");
            strTemp = strTemp.Replace("\n", "");
            strTemp = strTemp.Replace("\r", "");
            strTemp = Regex.Replace(strTemp, "<!--[^>](.*?)-->", "");   //주석제거
            strTemp = strTemp.Trim();

            return strTemp;
        }

        public static bool IsCookieContain(HtmlDocument doc, string strValue)
        {
            if (doc != null && doc.Cookie != null)
            {
                return clsUtil.StringContain(doc.Cookie, strValue);
            }
            return false;
        }

        public static bool RemoveElement(WebBrowser web, string strTag, string strAttribute, string strValue, string strInnerText = "")
        {
            if (strTag == null || strAttribute == null || strValue == null) return false;

            try
            {
                var domDoc = web.Document.DomDocument as HTMLDocument;
                if (domDoc != null)
                {
                    mshtml.IHTMLElementCollection elc = domDoc.getElementsByTagName(strTag);
                    if (elc == null) return false;

                    IHTMLDOMNode removeEl = null;
                    foreach (mshtml.IHTMLElement el in elc)
                    {
                        if (String.Compare(el.getAttribute(strAttribute), strValue, true) == 0)
                        {
                            if (strInnerText.Length > 0)
                            {
                                if (el.innerText != null && String.Compare(Trim(el.innerText), strInnerText, true) == 0)
                                {
                                    removeEl = el as IHTMLDOMNode;
                                    break;
                                }
                            }
                            else
                            {
                                removeEl = el as IHTMLDOMNode;
                                break;
                            }
                        }
                    }

                    if (removeEl != null)
                    {
                        mshtml.IHTMLDOMNode parentEl = removeEl.parentNode as IHTMLDOMNode;
                        parentEl.removeChild(removeEl);
                    }                    
                }
            }
            catch
            {
                return false;	
            }

            return true;
        }
    }
}
