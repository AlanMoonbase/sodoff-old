using System.Xml.Serialization;

namespace sodoff.Schema
{
    [XmlRoot(ElementName = "TrackInfo", IsNullable = true, Namespace = "")]
    [Serializable]
    public class TrackInfo
    {
        [XmlElement(ElementName = "id", IsNullable = true)]
        public int? UserTrackID;

        // Token: 0x040006F9 RID: 1785
        [XmlElement(ElementName = "utcid")]
        public int UserTrackCategoryID;

        // Token: 0x040006FA RID: 1786
        [XmlElement(ElementName = "uid")]
        public Guid UserID;

        // Token: 0x040006FB RID: 1787
        [XmlElement(ElementName = "nm")]
        public string Name;

        // Token: 0x040006FC RID: 1788
        [XmlElement(ElementName = "iu")]
        public string ImageURL;

        // Token: 0x040006FD RID: 1789
        [XmlElement(ElementName = "is")]
        public bool IsShared;

        // Token: 0x040006FE RID: 1790
        [XmlElement(ElementName = "sl", IsNullable = true)]
        public int? Slot;

        // Token: 0x040006FF RID: 1791
        [XmlElement(ElementName = "cd", IsNullable = true)]
        public DateTime? CreatedDate;

        // Token: 0x04000700 RID: 1792
        [XmlElement(ElementName = "md", IsNullable = true)]
        public DateTime? ModifiedDate;
    }
}
