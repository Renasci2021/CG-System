using System;
using System.Xml.Serialization;

namespace CG
{
    internal class XMLReader
    {
        private XMLLineList _lineList;

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
            XmlSerializer serializer = new(typeof(XMLLineList));
            using System.IO.StreamReader streamReader = new(filepath);
            _lineList = (XMLLineList)serializer.Deserialize(streamReader);
        }

        [Serializable]
        public class XMLLine
        {
            [XmlElement("Type")]
            private readonly string TypeString;

            [XmlIgnore]
            public LineType Type => Enum.TryParse(TypeString, true, out LineType type) ? type : LineType.Narration;

            [XmlElement("TextBoxType")]
            private readonly string TextBoxTypeString;

            [XmlIgnore]
            public TextBoxType TextBoxType => Enum.TryParse(TextBoxTypeString, true, out TextBoxType type) ? type : TextBoxType.Normal;

            [XmlElement("Character")]
            public readonly string Character;

            [XmlElement("Expression")]
            public readonly string Expression;

            [XmlElement("Effect")]
            private readonly string EffectString;

            [XmlIgnore]
            public EffectType Effect => Enum.TryParse(EffectString, true, out EffectType effect) ? effect : EffectType.None;

            [XmlElement("Interval")]
            private readonly string IntervalString;

            [XmlIgnore]
            public float Interval => float.TryParse(IntervalString, out float interval) ? interval : -1f;

            [XmlElement("Chinese")]
            public readonly string ChineseText;

            [XmlElement("English")]
            public readonly string EnglishText;
        }

        [Serializable]
        private class XMLLineList
        {
            [XmlElement("Line")]
            public XMLLine[] Lines;
        }
    }
}
