using System;
using System.Xml.Serialization;

namespace CG
{
    public class XMLReader
    {
        private XMLData _lineList;

        private int _lineIndex = 0;

        // TODO: Replace with Addressable Asset System
        private readonly string _folderPath = "Assets/CG System/Texts/";

        public XMLLine NextLine => _lineIndex < _lineList.Lines.Length ? _lineList.Lines[_lineIndex++] : null;

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
            // TODO: Write a StoryLine class and convert XMLLine to StoryLine
        }

        [Serializable]
        public class XMLLine
        {
            [XmlElement("Type")]
            public string TypeString;

            [XmlIgnore]
            public LineType LineType => Enum.TryParse(TypeString, true, out LineType type) ? type : throw new ArgumentException("Invalid Line Type");

            [XmlElement("TextBoxType")]
            public string TextBoxTypeString;

            [XmlIgnore]
            public TextBoxType TextBoxType => Enum.TryParse(TextBoxTypeString, true, out TextBoxType type) ? type : throw new ArgumentException("Invalid TextBox Type");

            [XmlElement("Character")]
            public string Character;

            [XmlElement("Expression")]
            public string Expression;

            [XmlElement("Effect")]
            public string EffectString;

            [XmlIgnore]
            public EffectType Effect => Enum.TryParse(EffectString, true, out EffectType effect) ? effect : EffectType.None;

            [XmlElement("Interval")]
            public string IntervalString;

            [XmlIgnore]
            public float Interval => float.TryParse(IntervalString, out float interval) ? interval : -1f;

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
