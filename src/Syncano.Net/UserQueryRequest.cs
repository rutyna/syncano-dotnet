﻿using System.Collections.Generic;
using Syncano.Net.Data;

namespace Syncano.Net
{
    public class UserQueryRequest
    {
        public UserQueryRequest()
        {
            State = DataObjectState.All;
        }

        public string ProjectId { get; set; }

        public string CollectionId { get; set; }

        public string CollectionKey { get; set; }

        public DataObjectState State { get; set; }

        public string Folder { get; set; }

        public List<string> Folders { get; set; }

        public DataObjectContentFilter? Filter { get; set; }


    }
}