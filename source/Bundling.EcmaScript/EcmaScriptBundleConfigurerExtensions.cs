﻿using System;
using Karambolo.AspNetCore.Bundling.EcmaScript;
using Karambolo.AspNetCore.Bundling.Internal.Helpers;
using Karambolo.AspNetCore.Bundling.Js;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder
{
    public static class EcmaScriptBundleConfigurerExtensions
    {
        public static JsBundleConfigurer EnableEs6ModuleBundling(this JsBundleConfigurer configurer, Action<ModuleBundlerOptions> configure = null)
        {
            if (configurer == null)
                throw new ArgumentNullException(nameof(configurer));

            configurer.Bundle.Transforms = configurer.Bundle.Transforms.Modify(
                l =>
                {
                    l.RemoveAll(t => t is ModuleBundlingTransform);

                    var index = l.FindIndex(t => t is JsMinifyTransform);
                    if (index < 0)
                        index = l.Count;

                    IModuleBundlerFactory moduleBundlerFactory = configurer.AppServices.GetRequiredService<IModuleBundlerFactory>();

                    var options = new ModuleBundlerOptions
                    {
                        DevelopmentMode = configurer.AppServices.GetService<IHostingEnvironment>()?.IsDevelopment() ?? false
                    };

                    configure?.Invoke(options);

                    l.Insert(index, new ModuleBundlingTransform(moduleBundlerFactory, options));
                });

            return configurer;
        }
    }
}
