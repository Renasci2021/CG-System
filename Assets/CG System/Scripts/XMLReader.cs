using System;
using System.Xml.Serialization;

namespace CG
{
    public record StoryLine(XMLReader.XMLLine Line)
    {
        public LineType LineType => (LineType)Enum.Parse(typeof(LineType), Line.TypeString);
        public TextBoxType TextBoxType => (TextBoxType)Enum.Parse(typeof(TextBoxType), Line.TextBoxTypeString);
        public string Character => Line.Character;
        public string Expression => Line.Expression;
        public ContinuationMode ContinuationMode => (ContinuationMode)Enum.Parse(typeof(ContinuationMode), Line.ContinuationModeString);
        public string ChineseText => Line.ChineseText;
        public string EnglishText => Line.EnglishText;
    }

    public class XMLReader
    {
        private XMLData _lineList;

        private int _lineIndex = 0;

        // TODO: Replace with Addressable Asset System
        private readonly string _folderPath = "Assets/CG System/Texts/";

        public StoryLine NextLine
        {
            get
            {
                if (_lineIndex < _lineList.Lines.Length)
                {
                    return new(_lineList.Lines[_lineIndex++]);
                }
                else
                {
                    return null;
                }
            }
        }

        public XMLReader(string chapterName)
        {
            // TODO: Replace with Addressable Reference
            LoadXML(_folderPath + chapterName + ".xml");
        }

        private void LoadXML(string filepath)
        {
            XmlSerializer serializer = new(typeof(XMLData));
            using System.IO.StreamReader streamReader = new(filepath);
            _lineList = (XMLData)serializer.Deserialize(streamReader);
        }

        [Serializable]
        public class XMLLine
        {
            [XmlElement("Type")]
            public string TypeString;

            [XmlElement("TextBoxType")]
            public string TextBoxTypeString;

            [XmlElement("Character")]
            public string Character;

            [XmlElement("Expression")]
            public string Expression;

            [XmlElement("ContinuationMode")]
            public string ContinuationModeString;

            [XmlElement("Chinese")]
            public string ChineseText;

            [XmlElement("English")]
            public string EnglishText;
        }

        [Serializable]
        public class XMLData
        {
            [XmlElement("Line")]
            public XMLLine[] Lines;
        }
    }
}

namespace System.Runtime.CompilerServices
{
    // * This attribute allows records to be used in Unity 2019.4 LTS
    internal class IsExternalInit { }
}
