using System;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using ImageMagick;
using Newtonsoft.Json;

using MeepLib;
using MeepLib.Messages;

namespace MeepMagick.Messages
{
    /// <summary>
    /// An ImageMagick Image
    /// </summary>
    /// <remarks>Most of the fields here aren't serialisable, so there isn't
    /// much value trading them over a process barrier. You should consider
    /// sending the DerivedFrom instead, and running it back through the Info
    /// module on the other side.</remarks>
    [DataContract]
    public class ImageMessage : Message
    {
        [XmlIgnore, JsonIgnore]
        public MagickImage Image { get; set; }

        /// <summary>
        /// Perceptual hash of image
        /// </summary>
        /// <value>The PHash.</value>
        /// <remarks>Use this to find likely duplicate images.</remarks>
        [XmlIgnore, JsonIgnore]
        public PerceptualHash PHash { get; set; }

        [DataMember, MaxLength(64), Index(IsUnique = false)]
        public string PHashString
        {
            get 
            {
                return PHash?.ToString();
            }
            set
            {
                PHash = new PerceptualHash(value);
            }
        }
    }
}
