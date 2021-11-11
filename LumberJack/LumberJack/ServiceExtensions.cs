using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BITS.Logger
{
    public static class ServiceExtensions
    {
        /// <summary>
        /// Sets up LumberJack
        /// </summary>
        /// <param name="services"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        public static IServiceCollection AddLumberJack(this IServiceCollection services, IConfiguration config)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (config == null) throw new ArgumentNullException(nameof(config));
            
            services.AddHttpContextAccessor();

            services.AddSingleton<ILumberJack>(s => new LumberJack(config
                , (IHttpContextAccessor)s.GetService(typeof(IHttpContextAccessor))
                ));


            return services;
        }
    }
}
