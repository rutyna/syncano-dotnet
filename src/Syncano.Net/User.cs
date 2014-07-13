﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Syncano.Net
{
    public class User
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("nick")]
        public string Nick { get; set; }

        [JsonProperty("avatar")]
        public Avatar Avatar { get; set; }
    }
}
