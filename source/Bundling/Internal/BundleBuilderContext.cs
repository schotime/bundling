﻿using System.Collections.Generic;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Karambolo.AspNetCore.Bundling.Internal
{
    public class BundleBuilderContext : IBundleBuilderContext
    {
        public IBundlingContext BundlingContext { get; set; }
        public PathString AppBasePath { get; set; }
        public IDictionary<string, StringValues> Params { get; set; }
        public IBundleModel Bundle { get; set; }
        public CancellationToken CancellationToken { get; set; }

        /// <remarks>
        /// Not null when change detection is enabled, otherwise null.
        /// </remarks>
        public ISet<IChangeSource> ChangeSources { get; set; }

        public string Result { get; set; }
    }
}
