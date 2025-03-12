using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HtmlAgilityPack;

namespace OSPAutoSearch_AutoLogin
{
    //HtmlAgilityPack LIB는 조회된 HTML코드를 Parsing한 정보를 가지고 있기때문에
    //모든속성의 값을 확인할 수 있다.
    public class clsHTMLParser : HtmlDocument
    {
        public delegate string FnSubString(string strText);

        public clsHTMLParser() { }

        public void setHTML(string strHTML)
        {
            LoadHtml(strHTML);
        }

        public bool setHTML(System.Windows.Forms.HtmlDocument doc)
        {
            try
            {
                LoadHtml(doc.Body.OuterHtml);
            }
            catch
            {
                return false;
            }
            
            return true;
        }

        public bool setHTMLEdge(string html)
        {
            try
            {
                LoadHtml(html);
            }
            catch
            {
                return false;
            }

            return true;
        }

        public string getInnerText(string strTag, HtmlNode rootNode = null)
        {
            if (strTag == null) return "";

            HtmlNode node = getNode3(strTag, rootNode);
            if (node != null && node.InnerText != null)
                return clsWebDocument.Trim(node.InnerText);

            return "";
        }

        public string getInnerText(HtmlNode node)
        {
            if (node == null) return "";

            foreach (HtmlNode childNode in node.ChildNodes)
            {
                if (childNode.NodeType == HtmlNodeType.Text)
                {
                    if (childNode.InnerText != null)
                    {
                        string strTemp = clsWebDocument.Trim(childNode.InnerText);
                        if (strTemp.Length > 0)
                            return strTemp;
                    }
                }
            }
            
            return "";
        }

        public string getInnerText(string strTag, string strAttribute, string strValue, HtmlNode rootNode = null)
        {
            if (strTag == null || strAttribute == null || strValue == null) return "";

            HtmlNode node = getNode(strTag, strAttribute, strValue, "", rootNode);
            if (node != null && node.InnerText != null)
                return clsWebDocument.Trim(node.InnerText);

            return "";
        }

        public string getInnerText2(string strTag, string strAttribute, string strValue, HtmlNode rootNode = null)
        {
            if (strTag == null || strAttribute == null || strValue == null) return "";

            HtmlNode node = getNode2(strTag, strAttribute, strValue, "", rootNode);
            if (node != null && node.InnerText != null)
                return clsWebDocument.Trim(node.InnerText);

            return "";
        }

        public string getInnerText(string strTag, string[] arrAttribute, string[] arrValue)
        {
            if (strTag == null || arrAttribute == null || arrValue == null) return "";

            HtmlNode node = getNode(strTag, arrAttribute, arrValue);
            if (node != null && node.InnerText != null)
                return clsWebDocument.Trim(node.InnerText);

            return "";
        }

        public string getInnerText2(string strTag, string[] arrAttribute, string[] arrValue)
        {
            if (strTag == null || arrAttribute == null || arrValue == null) return "";

            HtmlNode node = getNode2(strTag, arrAttribute, arrValue);
            if (node != null && node.InnerText != null)
                return clsWebDocument.Trim(node.InnerText);

            return "";
        }

        public void getInnerTextList(string strTag, string strAttribute, string[] arrValue, ref List<string> listResult)
        {
            if (strTag == null || strAttribute == null || arrValue == null) return;

            List<HtmlNode> listNode = new List<HtmlNode>();
            getNodes(strTag, strAttribute, arrValue, ref listNode);
            foreach (HtmlNode node in listNode)
            {
                if (node != null && node.InnerText != null)
                    listResult.Add(clsWebDocument.Trim(node.InnerText));
            }
        }

        public void getInnerTextList2(string strTag, string strAttribute, string[] arrValue, ref List<string> listResult, HtmlNode rootNode = null)
        {
            if (strTag == null || strAttribute == null || arrValue == null) return;

            List<HtmlNode> listNode = new List<HtmlNode>();
            getNodes2(strTag, strAttribute, arrValue, ref listNode, rootNode);
            foreach (HtmlNode node in listNode)
            {   
                if (node != null && node.InnerText != null)
                    listResult.Add(clsWebDocument.Trim(node.InnerText));
            }
        }
		
		// 태그명 만으로 텍스트 가져오기
        public void getInnerTextList4(string strTag, ref List<string> listResult, HtmlNode rootNode = null)
        {
            if (strTag == null) return;

            List<HtmlNode> listNode = new List<HtmlNode>();
            getNodes(strTag, ref listNode, rootNode);
            foreach (HtmlNode node in listNode)
            {   
                if (node != null && node.InnerText != null)
                    listResult.Add(clsWebDocument.Trim(node.InnerText));
            }
        }

        public void getInnerTextList(string strTag, string[] arrAttribute, string[] arrValue, ref List<string> listResult)
        {
            if (strTag == null || arrAttribute == null || arrValue == null) return;

            List<HtmlNode> listNode = new List<HtmlNode>();
            getNodes(strTag, arrAttribute, arrValue, ref listNode);
            foreach (HtmlNode node in listNode)
            {
                if (node != null && node.InnerText != null)
                    listResult.Add(clsWebDocument.Trim(node.InnerText));
            }
        }

        public void getInnerTextList2(string strTag, string[] arrAttribute, string[] arrValue, ref List<string> listResult)
        {
            if (strTag == null || arrAttribute == null || arrValue == null) return;

            List<HtmlNode> listNode = new List<HtmlNode>();
            getNodes2(strTag, arrAttribute, arrValue, ref listNode);
            foreach (HtmlNode node in listNode)
            {
                if (node != null && node.InnerText != null)
                    listResult.Add(clsWebDocument.Trim(node.InnerText));
            }
        }

        // 태그명, InnerText를 비교해서 node를 검색한다. contain
        public HtmlNode getNodeContains(string strTag, string strInnerText)
        {
            if (strTag == null || strInnerText == null) return null;

            HtmlNodeCollection nodelist = DocumentNode.SelectNodes("//" + strTag);
            if (nodelist == null) return null;

            foreach (HtmlNode node in nodelist)
            {
                if (node.InnerText != null && clsWebDocument.Trim(node.InnerText).Contains(strInnerText))
                {
                    return node;
                }
            }

            return null;
        }

        // 태그명, InnerText를 비교해서 node를 검색한다.
        public HtmlNode getNode(string strTag, string strInnerText)
        {
            if (strTag == null || strInnerText == null) return null;

            HtmlNodeCollection nodelist = DocumentNode.SelectNodes("//" + strTag);
            if (nodelist == null) return null;

            foreach (HtmlNode node in nodelist)
            {
                if (node.InnerText != null && String.Compare(clsWebDocument.Trim(node.InnerText), strInnerText, true) == 0)
                {
                    return node;
                }
            }

            return null;
        }

        // 태그명, 속성의 값을 비교해서 대상 노드를 찾는다. (InnerText까지 비교가능)
        public HtmlNode getNode(string strTag, string strAttribute, string strValue, string strInnerText = "", HtmlNode rootNode = null)
        {
            if (strTag == null || strAttribute == null || strValue == null) return null;

            HtmlNodeCollection nodelist = null;
            if (rootNode != null)
                nodelist = DocumentNode.SelectNodes(rootNode.XPath + "//" + strTag);
            else
                nodelist = DocumentNode.SelectNodes("//" + strTag);

            if (nodelist == null) return null;

            foreach (HtmlNode node in nodelist)
            {   
                HtmlAttribute attr = node.Attributes[strAttribute];
                if (attr != null)
                {
                    if (String.Compare(attr.Value, strValue, true) == 0)
                    {
                        if (strInnerText.Length > 0)
                        {
                            if (node.InnerText != null && String.Compare(clsWebDocument.Trim(node.InnerText), strInnerText, true) == 0)
                            {
                                return node;
                            }
                        }
                        else
                        {
                            return node;
                        }
                    }
                }
            }

            return null;
        }

        // 태그명, 속성의 값의 포함여부를 확인해서 대상 노드를 찾는다. (InnerText까지 비교가능)
        public HtmlNode getNode2(string strTag, string strAttribute, string strValue, string strInnerText = "", HtmlNode rootNode = null)
        {
            if (strTag == null || strAttribute == null || strValue == null) return null;

            HtmlNodeCollection nodelist = null;
            if (rootNode != null)
                nodelist = DocumentNode.SelectNodes(rootNode.XPath + "//" + strTag);
            else
                nodelist = DocumentNode.SelectNodes("//" + strTag);

            if (nodelist == null) return null;

            foreach (HtmlNode node in nodelist)
            {
                HtmlAttribute attr = node.Attributes[strAttribute];
                if (attr != null)
                {
                    if (attr.Value.Contains(strValue) == true)
                    {
                        if (strInnerText.Length > 0)
                        {
                            if (node.InnerText != null && String.Compare(clsWebDocument.Trim(node.InnerText), strInnerText, true) == 0)
                            {
                                return node;
                            }
                        }
                        else
                        {
                            return node;
                        }
                    }
                }
            }

            return null;
        }

        // 태그명, 속성의 값을 비교해서 대상 노드를 찾는다. (InnerText까지 비교가능)
        public HtmlNode getNode(string strTag, string[] arrAttribute, string[] arrValue, string strInnerText = "")
        {
            if (strTag == null || arrAttribute == null || arrValue == null) return null;

            HtmlNodeCollection nodelist = DocumentNode.SelectNodes("//" + strTag);
            if (nodelist == null) return null;

            foreach (HtmlNode node in nodelist)
            {
                bool isSearch = false;
                for (int i = 0; i < arrAttribute.Length; i++)
                {
                    HtmlAttribute attr = node.Attributes[arrAttribute[i]];
                    if (attr != null)
                    {
                        if (String.Compare(attr.Value, arrValue[i], true) == 0)
                        {
                            isSearch = true;
                        }
                        else
                        {
                            isSearch = false;
                            break;
                        }
                    }
                    else
                    {
                        isSearch = false;
                        break;
                    }
                }

                if (isSearch == true)
                {
                    if (strInnerText.Length > 0)
                    {
                        if (String.Compare(clsWebDocument.Trim(node.InnerText), strInnerText, true) == 0)
                            return node;
                    }
                    else
                    {
                        return node;
                    }
                }
            }

            return null;
        }

        // 태그명, 속성의 값을 비교해서 대상 노드를 찾는다. (InnerText까지 비교가능)
        public HtmlNode getNode2(string strTag, string[] arrAttribute, string[] arrValue, string strInnerText = "")
        {
            if (strTag == null || arrAttribute == null || arrValue == null) return null;

            HtmlNodeCollection nodelist = DocumentNode.SelectNodes("//" + strTag);
            if (nodelist == null) return null;

            foreach (HtmlNode node in nodelist)
            {
                bool isSearch = false;
                for (int i = 0; i < arrAttribute.Length; i++)
                {
                    HtmlAttribute attr = node.Attributes[arrAttribute[i]];
                    if (attr != null)
                    {
                        if (attr.Value.Contains(arrValue[i]) == true)
                        {
                            isSearch = true;
                        }
                        else
                        {
                            isSearch = false;
                            break;
                        }
                    }
                    else
                    {
                        isSearch = false;
                        break;
                    }
                }

                if (isSearch == true)
                {
                    if (strInnerText.Length > 0)
                    {
                        if (String.Compare(clsWebDocument.Trim(node.InnerText), strInnerText, true) == 0)
                            return node;
                    }
                    else
                    {
                        return node;
                    }
                }
            }

            return null;
        }

        // 태그명을 비교해서 대상 노드를 찾는다.
        public HtmlNode getNode3(string strTag, HtmlNode rootNode = null)
        {
            if (strTag == null) return null;

            HtmlNodeCollection nodelist = null;
            if (rootNode != null)
                nodelist = DocumentNode.SelectNodes(rootNode.XPath + "//" + strTag);
            else
                nodelist = DocumentNode.SelectNodes("//" + strTag);

            if (nodelist.Count > 0) return nodelist[0];

            
            return null;
        }

        // 태그명을 비교해서 대상 노드를 찾는다.
        public void getNodes(string strTag, ref List<HtmlNode> listNode, HtmlNode rootNode = null)
        {
            if (strTag == null) return;

            HtmlNodeCollection nodelist = null;
            if (rootNode != null)
                nodelist = DocumentNode.SelectNodes(rootNode.XPath + "//" + strTag);
            else
                nodelist = DocumentNode.SelectNodes("//" + strTag);

            if (nodelist == null) return;

            foreach (HtmlNode node in nodelist)
            {
                listNode.Add(node);
            }
        }

        // 태그값, InnerText만으로 노드를 찾는다.
        public void getNodes(string strTag, string strInnerText, ref List<HtmlNode> listNode)
        {
            if (strTag == null || strInnerText == null) return;

            HtmlNodeCollection nodelist = DocumentNode.SelectNodes("//" + strTag);
            if (nodelist == null) return;

            foreach (HtmlNode node in nodelist)
            {
                if (node.InnerText != null && String.Compare(clsWebDocument.Trim(node.InnerText), strInnerText, true) == 0)
                {
                    listNode.Add(node);
                }
            }
        }

        // 태그명, 속성의 값을 비교해서 대상 노드를 찾는다.
        public void getNodesNull(string strTag, string strAttribute, ref List<HtmlNode> listNode, HtmlNode rootNode = null)
        {
            if (strTag == null || strAttribute == null) return;

            HtmlNodeCollection nodelist = null;
            if (rootNode != null)
                nodelist = DocumentNode.SelectNodes(rootNode.XPath + "//" + strTag);
            else
                nodelist = DocumentNode.SelectNodes("//" + strTag);

            if (nodelist == null) return;

            foreach (HtmlNode node in nodelist)
            {
                HtmlAttribute attr = node.Attributes[strAttribute];
                if (attr == null)
                {
                    listNode.Add(node);
                }
            }
        }

        // 태그명, 속성의 값을 비교해서 대상 노드를 찾는다.
        public void getNodes(string strTag, string strAttribute, string[] arrValue, ref List<HtmlNode> listNode, HtmlNode rootNode = null)
        {
            if (strTag == null || strAttribute == null || arrValue == null) return;

            HtmlNodeCollection nodelist = null;
            if (rootNode != null)
                nodelist = DocumentNode.SelectNodes(rootNode.XPath + "//" + strTag);
            else
                nodelist = DocumentNode.SelectNodes("//" + strTag);

            if (nodelist == null) return;

            foreach (HtmlNode node in nodelist)
            {
                HtmlAttribute attr = node.Attributes[strAttribute];
                if (attr != null)
                {
                    for (int i = 0; i < arrValue.Length; i++)
                    {
                        if (String.Compare(attr.Value, arrValue[i], true) == 0)
                        {
                            listNode.Add(node);
                        }
                    }
                }
            }
        }

        // 태그명, 속성의 값의 포함여부를 확인해서 대상 노드를 찾는다.
        public void getNodes2(string strTag, string strAttribute, string[] arrValue, ref List<HtmlNode> listNode, HtmlNode rootNode = null)
        {
            if (strTag == null || strAttribute == null || arrValue == null) return;

            HtmlNodeCollection nodelist = null;
            if (rootNode != null)
                nodelist = DocumentNode.SelectNodes(rootNode.XPath + "//" + strTag);
            else
                nodelist = DocumentNode.SelectNodes("//" + strTag);

            if (nodelist == null) return;

            foreach (HtmlNode node in nodelist)
            {
                HtmlAttribute attr = node.Attributes[strAttribute];
                if (attr != null)
                {
                    for (int i = 0; i < arrValue.Length; i++)
                    {
                        if (attr.Value.Contains(arrValue[i]) == true)
                        {
                            listNode.Add(node);
                        }
                    }
                }
            }
        }

        public void getNodes(string strTag, string[] arrAttribute, string[] arrValue, ref List<HtmlNode> listNode, HtmlNode rootNode = null)
        {
            if (strTag == null || arrAttribute == null || arrValue == null) return;

            HtmlNodeCollection nodelist = null;
            if (rootNode != null)
                nodelist = DocumentNode.SelectNodes(rootNode.XPath + "//" + strTag);
            else
                nodelist = DocumentNode.SelectNodes("//" + strTag);

            if (nodelist == null) return;

            foreach (HtmlNode node in nodelist)
            {
                List<string> listAttrValue = new List<string>();
                for (int i = 0; i < arrAttribute.Length; i++)
                {
                    HtmlAttribute attr = node.Attributes[arrAttribute[i]];
                    if (attr != null)
                    {
                        listAttrValue.Add(attr.Value);
                    }
                }

                if (listAttrValue.Count >= arrAttribute.Length
                     && listAttrValue.Count >= arrValue.Length)
                {
                    bool isMatching = true;
                    for (int i = 0; i < arrAttribute.Length; i++)
                    {
                        if (String.Compare(listAttrValue[i], arrValue[i], true) != 0)
                        {
                            isMatching = false;
                            break;
                        }
                    }

                    if(isMatching == true)
                        listNode.Add(node);
                }
            }
        }

        public void getNodes2(string strTag, string[] arrAttribute, string[] arrValue, ref List<HtmlNode> listNode, HtmlNode rootNode = null)
        {
            if (strTag == null || arrAttribute == null || arrValue == null) return;

            HtmlNodeCollection nodelist = null;
            if (rootNode != null)
                nodelist = DocumentNode.SelectNodes(rootNode.XPath + "//" + strTag);
            else
                nodelist = DocumentNode.SelectNodes("//" + strTag);

            if (nodelist == null) return;

            foreach (HtmlNode node in nodelist)
            {
                List<string> listAttrValue = new List<string>();
                for (int i = 0; i < arrAttribute.Length; i++)
                {
                    HtmlAttribute attr = node.Attributes[arrAttribute[i]];
                    if (attr != null)
                    {
                        listAttrValue.Add(attr.Value);
                    }
                }

                if (listAttrValue.Count >= arrAttribute.Length
                     && listAttrValue.Count >= arrValue.Length)
                {
                    bool isMatching = true;
                    for (int i = 0; i < arrAttribute.Length; i++)
                    {
                        if (listAttrValue[i].Contains(arrValue[i]) == false)
                        {
                            isMatching = false;
                            break;
                        }
                    }

                    if(isMatching == true)
                        listNode.Add(node);
                }
            }
        }

        public bool isNode(string strTag, string strInnerText)
        {
            HtmlNodeCollection nodelist = DocumentNode.SelectNodes("//" + strTag);
            if (nodelist == null) return false;

            foreach (HtmlNode node in nodelist)
            {
                if (node.InnerText != null && String.Compare(clsWebDocument.Trim(node.InnerText), strInnerText, true) == 0)
                {
                    return true;
                }
            }

            return false;
        }

        public bool isNode2(string strTag, string strInnerText)
        {
            HtmlNodeCollection nodelist = DocumentNode.SelectNodes("//" + strTag);
            if (nodelist == null) return false;

            foreach (HtmlNode node in nodelist)
            {
                if (node.InnerText != null && clsWebDocument.Trim(node.InnerText).Contains(strInnerText) == true)
                {
                    return true;
                }
            }

            return false;
        }

        public bool isNode(string strTag, string strAttribute, string strValue, string strInnerText = "")
        {
            HtmlNodeCollection nodelist = DocumentNode.SelectNodes("//" + strTag);
            if (nodelist == null) return false;

            foreach (HtmlNode node in nodelist)
            {
                HtmlAttribute attr = node.Attributes[strAttribute];
                if (attr != null)
                {
                    if (String.Compare(attr.Value, strValue, true) == 0)
                    {
                        if (strInnerText.Length > 0)
                        {
                            if (node.InnerText != null && String.Compare(clsWebDocument.Trim(node.InnerText), strInnerText, true) == 0)
                            {
                                return true;
                            }
                        }
                        else
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public bool isNode2(string strTag, string strAttribute, string strValue, string strInnerText = "")
        {
            HtmlNodeCollection nodelist = DocumentNode.SelectNodes("//" + strTag);
            if (nodelist == null) return false;

            foreach (HtmlNode node in nodelist)
            {
                HtmlAttribute attr = node.Attributes[strAttribute];
                if (attr != null)
                {
                    if (attr.Value.Contains(strValue) == true)
                    {
                        if (strInnerText.Length > 0)
                        {
                            if (node.InnerText != null && String.Compare(clsWebDocument.Trim(node.InnerText), strInnerText, true) == 0)
                            {
                                return true;
                            }
                        }
                        else
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public bool isNode(string strTag, string[] arrAttribute, string[] arrValue, string strInnerText = "")
        {
            HtmlNodeCollection nodelist = DocumentNode.SelectNodes("//" + strTag);
            if (nodelist == null) return false;

            foreach (HtmlNode node in nodelist)
            {
                bool isSearch = false;
                for (int i = 0; i < arrAttribute.Length; i++)
                {
                    HtmlAttribute attr = node.Attributes[arrAttribute[i]];
                    if (attr != null)
                    {
                        if (String.Compare(attr.Value, arrValue[i], true) == 0)
                        {
                            isSearch = true;
                        }
                        else
                        {
                            isSearch = false;
                            break;
                        }
                    }
                    else
                    {
                        isSearch = false;
                        break;
                    }
                }

                if (isSearch == true)
                {
                    if (strInnerText.Length > 0)
                    {
                        if (String.Compare(clsWebDocument.Trim(node.InnerText), strInnerText, true) == 0)
                            return true;
                    }
                    else
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public bool isNode2(string strTag, string[] arrAttribute, string[] arrValue, string strInnerText = "")
        {
            HtmlNodeCollection nodelist = DocumentNode.SelectNodes("//" + strTag);
            if (nodelist == null) return false;

            foreach (HtmlNode node in nodelist)
            {
                bool isSearch = false;
                for (int i = 0; i < arrAttribute.Length; i++)
                {
                    HtmlAttribute attr = node.Attributes[arrAttribute[i]];
                    if (attr != null)
                    {
                        if (attr.Value.Contains(arrValue[i]) == true)
                        {
                            isSearch = true;
                        }
                        else
                        {
                            isSearch = false;
                            break;
                        }
                    }
                    else
                    {
                        isSearch = false;
                        break;
                    }
                }

                if (isSearch == true)
                {
                    if (strInnerText.Length > 0)
                    {
                        if (String.Compare(clsWebDocument.Trim(node.InnerText), strInnerText, true) == 0)
                            return true;
                    }
                    else
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        // 지정노드에서 nParentIndex값만큼 상위노드를 검색한다.
        public HtmlNode getParentNode(HtmlNode node, int nParentIndex)
        {
            if (node == null) return null;

            HtmlNode tempNode = node;
            for (int i = 0; i < nParentIndex; i++)
            {
                if (tempNode.ParentNode != null)
                {
                    tempNode = tempNode.ParentNode;
                }
                else
                {
                    return null;
                }
            }

            return tempNode;
        }

        // 지정노드에서 strParentTagName인 상위노드가 검색될때가지 검색한다.
        public HtmlNode getParentNode(HtmlNode node, string strParentTagName)
        {
            if (node == null) return null;
            if (strParentTagName == null) return null;

            HtmlNode tempNode = node;
            while (tempNode != null)
            {
                tempNode = tempNode.ParentNode;

                if (tempNode != null)
                {
                    if (String.Compare(tempNode.OriginalName, strParentTagName, true) == 0)
                    {
                        break;
                    }
                }
            }

            return tempNode;
        }

        // 하위Node들중 검색태그명과 일치하고 일치하는 Node중에서 ChildIndex순번의 node검색
        public HtmlNode getChildNode(HtmlNode node, string strTag, int nChildIndex)
        {
            if (node == null) return null;
            if (strTag == null) return null;

            HtmlNodeCollection elc = node.ChildNodes;
            if (elc == null) return null;

            int i = 0;
            foreach (HtmlNode tempNode in elc)
            {
                if (String.Compare(tempNode.OriginalName, strTag, true) == 0)
                {
                    i++;
                    if (i >= nChildIndex)
                    {
                        return tempNode;
                    }
                }
            }

            return null;
        }

        // 하위Node들중 태그명, 속성의 값이 일치하는 Node검색
        public HtmlNode getChildNode(HtmlNode node, string strTag, string strAttribute, string strValue, string strInnerText = "")
        {
            if (node == null) return null;
            if (strTag == null || strAttribute == null || strValue == null) return null;

            HtmlNodeCollection elc = node.ChildNodes;
            if (elc == null) return null;

            foreach (HtmlNode tempNode in elc)
            {
                if (String.Compare(tempNode.OriginalName, strTag, true) == 0)
                {
                    if (strAttribute.Length > 0)
                    {
                        HtmlAttribute attr = tempNode.Attributes[strAttribute];
                        if (attr != null)
                        {
                            if (String.Compare(attr.Value, strValue, true) == 0)
                            {
                                if (strInnerText.Length > 0)
                                {
                                    if (tempNode.InnerText != null)
                                    {
                                        if (String.Compare(tempNode.InnerText, strInnerText, true) == 0)
                                        {
                                            return tempNode;
                                        }
                                    }
                                }
                                else
                                {
                                    return tempNode;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (strInnerText.Length > 0)
                        {
                            if (tempNode.InnerText != null)
                            {
                                if (String.Compare(tempNode.InnerText, strInnerText, true) == 0)
                                {
                                    return tempNode;
                                }
                            }
                        }
                        else
                        {
                            return tempNode;
                        }
                    }
                }
            }

            return null;
        }

        // 하위Node들중 태그명, 속성의 값을 포함하는 Node검색
        public HtmlNode getChildNode2(HtmlNode node, string strTag, string strAttribute, string strValue, string strInnerText = "")
        {
            if (node == null) return null;
            if (strTag == null || strAttribute == null || strValue == null) return null;

            HtmlNodeCollection elc = node.ChildNodes;
            if (elc == null) return null;

            foreach (HtmlNode tempNode in elc)
            {
                if (String.Compare(tempNode.OriginalName, strTag, true) == 0)
                {
                    if (strAttribute.Length > 0)
                    {
                        HtmlAttribute attr = tempNode.Attributes[strAttribute];
                        if (attr != null)
                        {
                            if (attr.Value.Contains(strValue) == true)
                            {
                                if (strInnerText.Length > 0)
                                {
                                    if (tempNode.InnerText != null)
                                    {
                                        if (String.Compare(tempNode.InnerText, strInnerText, true) == 0)
                                        {
                                            return tempNode;
                                        }
                                    }
                                }
                                else
                                {
                                    return tempNode;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (strInnerText.Length > 0)
                        {
                            if (tempNode.InnerText != null)
                            {
                                if (String.Compare(tempNode.InnerText, strInnerText, true) == 0)
                                {
                                    return tempNode;
                                }
                            }
                        }
                        else
                        {
                            return tempNode;
                        }
                    }
                }
            }

            return null;          
        }

        public void getBoardList(HtmlNode node, ref List<string> listResult, int nMaxElement = 20, bool bSkipEmpty = false)
        {      
            if (node == null || node.NodeType != HtmlNodeType.Element) return;

            int nCount = 0;

            HtmlNodeCollection childnodelist = node.ChildNodes;
            foreach (HtmlNode childnode in childnodelist)
            {
                if (childnode.NodeType == HtmlNodeType.Element)
                {
                    string strTemp = clsWebDocument.Trim(childnode.InnerText);

                    if (bSkipEmpty && strTemp.Length == 0) { continue; }

                    listResult.Add(strTemp);

                    ++nCount;
                    if (nCount >= nMaxElement) return;
                }
            }
        }

        public string getValueInAttribute(HtmlNode node, string strAttribute)
        {
            if (node == null || strAttribute == null) return "";

            HtmlAttribute attr = node.Attributes[strAttribute];
            if (attr != null)
            {
                return attr.Value;
            }

            return "";
        }

        public string getValueInAttribute2(string strTag, string strAttribute, string strValue, HtmlAgilityPack.HtmlNode rootNode = null)
        {
            if (strTag == null || strAttribute == null || strValue == null) return "";

            HtmlNodeCollection nodelist = null;
            if (rootNode != null)
                nodelist = DocumentNode.SelectNodes(rootNode.XPath + "//" + strTag);
            else
                nodelist = DocumentNode.SelectNodes("//" + strTag);

            if (nodelist == null) return "";

            foreach (HtmlNode node in nodelist)
            {
                HtmlAttribute attr = node.Attributes[strAttribute];
                if (attr != null)
                {
                    if (attr.Value.Contains(strValue) == true)
                    {
                        return attr.Value;
                    }
                }
            }

            return "";
        }

        public void getValueInAttribute(string strTag, string strFindAttribute, string strFindValue, string strReturnAttribute, ref List<string> listResult, FnSubString fn = null)
        {
            if (strTag == null || strFindAttribute == null || strFindValue == null || strReturnAttribute == null) return;

            HtmlNodeCollection nodelist = DocumentNode.SelectNodes("//" + strTag);
            foreach (HtmlNode node in nodelist)
            {
                if (node.NodeType == HtmlNodeType.Element)
                {
                    HtmlAttribute attr = node.Attributes[strFindAttribute];
                    if (attr != null)
                    {
                        if (String.Compare(attr.Value, strFindValue, true) == 0)
                        {
                            HtmlAttribute attrReturn = node.Attributes[strReturnAttribute];
                            if (attrReturn != null)
                            {
                                if (fn != null)
                                {
                                    listResult.Add(fn(attrReturn.Value));
                                }
                                else
                                {
                                    listResult.Add(attrReturn.Value);
                                }
                            }
                        }
                    }
                }
            }
        }

        public void getValueInAttribute(HtmlNode node, string strFindAttribute, string strFindValue, string strReturnAttribute, ref List<string> listResult, FnSubString fn = null)
        {
            if (node == null || strFindAttribute == null || strFindValue == null || strReturnAttribute == null) return;

            HtmlNodeCollection childnodelist = node.ChildNodes;
            foreach (HtmlNode childnode in childnodelist)
            {
                if (childnode.NodeType == HtmlNodeType.Element)
                {
                    HtmlAttribute attr = childnode.Attributes[strFindAttribute];
                    if (attr != null)
                    {
                        if (String.Compare(attr.Value, strFindValue, true) == 0)
                        {
                            HtmlAttribute attrReturn = childnode.Attributes[strReturnAttribute];
                            if (attrReturn != null)
                            {
                                if (fn != null)
                                {
                                    listResult.Add(fn(attrReturn.Value));
                                }
                                else
                                {
                                    listResult.Add(attrReturn.Value);
                                }
                            }
                        }
                    }
                }
            }   
        }

        public void getValueInAttribute(string strTag, string strAttribute, string[] arrValue, ref List<string> listResult, FnSubString fn = null)
        {
            if (strTag == null || strAttribute == null || arrValue == null) return;

            HtmlNodeCollection nodelist = DocumentNode.SelectNodes("//" + strTag);
            if (nodelist == null) return;

            foreach (HtmlNode node in nodelist)
            {
                HtmlAttribute attr = node.Attributes[strAttribute];
                if (attr != null)
                {
                    for (int i = 0; i < arrValue.Length; i++)
                    {
                        if (String.Compare(attr.Value, arrValue[i], true) == 0)
                        {
                            if (fn != null)
                            {
                                listResult.Add(fn(attr.Value));
                            }
                            else
                            {
                                listResult.Add(attr.Value);
                            }

                            break;
                        }
                    }                    
                }
            }

            return;
        }

        public void getValueInAttribute2(string strTag, string strAttribute, string[] arrValue, ref List<string> listResult, FnSubString fn = null, HtmlNode rootNode = null)
        {
            if (strTag == null || strAttribute == null || arrValue == null) return;


            HtmlNodeCollection nodelist = null;
            if (rootNode != null)
                nodelist = DocumentNode.SelectNodes(rootNode.XPath + "//" + strTag);
            else
                nodelist = DocumentNode.SelectNodes("//" + strTag);
            //HtmlNodeCollection nodelist = DocumentNode.SelectNodes("//" + strTag);

            if (nodelist == null) return;

            foreach (HtmlNode node in nodelist)
            {
                HtmlAttribute attr = node.Attributes[strAttribute];
                if (attr != null)
                {
                    for (int i = 0; i < arrValue.Length; i++)
                    {
                        if (attr.Value.Contains(arrValue[i]) == true)
                        {
                            if (fn != null)
                            {
                                listResult.Add(fn(attr.Value));
                            }
                            else
                            {
                                listResult.Add(attr.Value);
                            }

                            break;
                        }
                    }
                }
            }

            return;
        }

        public void getValueInAttribute3(string strTag, string strAttribute, string[] arrValue, ref List<string> listResult, string []arrIf = null, FnSubString fn = null, HtmlNode rootNode = null)
        {
            if (strTag == null || strAttribute == null || arrValue == null) return;


            HtmlNodeCollection nodelist = null;
            if (rootNode != null)
                nodelist = DocumentNode.SelectNodes(rootNode.XPath + "//" + strTag);
            else
                nodelist = DocumentNode.SelectNodes("//" + strTag);
            //HtmlNodeCollection nodelist = DocumentNode.SelectNodes("//" + strTag);

            if (nodelist == null) return;

            foreach (HtmlNode node in nodelist)
            {
                HtmlAttribute attr = node.Attributes[strAttribute];
                if (attr != null)
                {
                    for (int i = 0; i < arrValue.Length; i++)
                    {
                        if (attr.Value.Contains(arrValue[i]) == true)
                        {
                            if (arrIf != null)
                            {
                                HtmlAttribute attrIF = node.Attributes[arrIf[0]];
                                if (!attrIF.Value.Contains(arrIf[1]))
                                    continue;
                            }

                            if (fn != null)
                            {
                                listResult.Add(fn(attr.Value));
                            }
                            else
                            {
                                listResult.Add(attr.Value);
                            }

                            break;
                        }
                    }
                }
            }

            return;
        }

        //인자로 넘오언 노드의 자식중에 Element가 있는지 여부를 확인한다.
        public bool isEmptyElement(HtmlNode node, string strTag = "")
        {
            foreach (HtmlNode childNode in node.ChildNodes)
            {
                if (childNode.NodeType == HtmlNodeType.Element)
                {
                    if (strTag.Length > 0)
                    {
                        if (clsUtil.isCompare(childNode.OriginalName, strTag) == true)
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }              
                }
            }

            return true;
        }
    }
}
