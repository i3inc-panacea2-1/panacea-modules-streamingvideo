using Panacea.Models;
using Panacea.Multilinguality;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using static System.Net.Mime.MediaTypeNames;

namespace Panacea.Modules.StreamingVideo.Models
{
    [DataContract]
    public class VideoStreamItem : ServerItem
    {
        [DataMember(Name = "url")]
        public string Url { get; set; }

        [IsTranslatable]
        [DataMember(Name = "description")]
        public string Description
        {
            get => GetTranslation();
            set => SetTranslation(value);
        }
    }
}
