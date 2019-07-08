using Panacea.ContentControls;
using Panacea.Core;
using Panacea.Models;
using Panacea.Modules.StreamingVideo.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Panacea.Modules.StreamingVideo
{
    public class StreamingVideoProvider : HospitalServerLazyItemProvider<VideoStreamItem>
    {
        public StreamingVideoProvider(PanaceaServices core)
            : base(core, 
                  "streaming_video_collection/get_categories_only/",
                  "streaming_video_collection/get_category_limited/{0}/{1}/{2}/",
                  "streaming_video_collection/find/{0}/{1}/{2}/{3}/", 
                  10)
        {
        }
    }
}
