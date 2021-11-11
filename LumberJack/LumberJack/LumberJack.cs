using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;

namespace BITS.Logger
{

    public interface ILumberJack
    {
        void LogDiagnostic(string message, Dictionary<string, object> diagnosticInfo = null);
        void LogUsage(string message, Dictionary<string, object> additionalInfo = null);
        void LogError(Exception ex, string message = null);
        void LogPerformance(long elapsedTime, Dictionary<string, object> performanceInfo = null);
        void GetLocationForApiCall(Dictionary<string, object> dict, out string location);
        void GetUserData(Dictionary<string, object> data, out int userId, out string userName);
        Dictionary<string, object> GetBitsLoggingData(out int userId, out string userName, out string location);
    }


    public class LumberJack : ILumberJack
    {
        private readonly IHttpContextAccessor _context;
        private readonly LoggerOptions _appSettings;
        private readonly ILogger _perfLogger;
        private readonly ILogger _usageLogger;
        private readonly ILogger _errorLogger;
        private readonly ILogger _diagnosticLogger;

        private string _messageTemplate = "{Timestamp}|{Message}|{Location}|{Product}|" +
                                          "{CustomException}|{ElapsedMilliseconds}|{Exception}|{Hostname}|" +
                                          "{PartId}|{UserName}|{CorrelationId}|{AdditionalInfo}|{EnvironmentName}\n";


        public LumberJack(IConfiguration config, IHttpContextAccessor context)
        {
            _context = context;
            _appSettings = config.GetSection("BITS.Logger").Get<LoggerOptions>();

            _perfLogger = new LoggerConfiguration().Enrich.FromLogContext()
                //.WriteTo.EventCollector(_appSettings.EventCollector,_appSettings.EventCollectorToken)
                .WriteTo.File(new JsonFormatter(), @$"{_appSettings.LogLocation}\perf.json", shared: true, rollingInterval: RollingInterval.Day)
                .CreateLogger();

            _usageLogger = new LoggerConfiguration().Enrich.FromLogContext()
                //.WriteTo.EventCollector(_appSettings.EventCollector, _appSettings.EventCollectorToken)
                .WriteTo.File(new JsonFormatter(), @$"{_appSettings.LogLocation}\usage.json", shared: true, rollingInterval: RollingInterval.Day)
                .CreateLogger();

            _errorLogger = new LoggerConfiguration().Enrich.FromLogContext()
                //.WriteTo.EventCollector(_appSettings.EventCollector, _appSettings.EventCollectorToken)
                .WriteTo.File(new JsonFormatter(), @$"{_appSettings.LogLocation}\error.json", shared: true, rollingInterval: RollingInterval.Day)
                .CreateLogger();

            _diagnosticLogger = new LoggerConfiguration().Enrich.FromLogContext()
                //.WriteTo.EventCollector(_appSettings.EventCollector, _appSettings.EventCollectorToken)
                .WriteTo.File(new JsonFormatter(), @$"{_appSettings.LogLocation}\diag.json", shared: true, rollingInterval: RollingInterval.Day)
                .CreateLogger();

            Serilog.Debugging.SelfLog.Enable(msg => Debug.WriteLine(msg));
        }
        
        public void WritePerf(LumberJackDetail infoToLog)
        {
            _perfLogger.Write(LogEventLevel.Information, _messageTemplate
                    ,
                    infoToLog.Timestamp, infoToLog.Message,
                    infoToLog.Location,
                    infoToLog.Product, infoToLog.LumberJackException,
                    infoToLog.ElapsedMilliseconds, infoToLog.Exception?.ToBetterString(),
                    infoToLog.Hostname, infoToLog.PartId,
                    infoToLog.UserName, infoToLog.CorrelationId,
                    infoToLog.AdditionalInfo,
                    _appSettings.EnvironmentName
                    );
        }
        public void WriteUsage(LumberJackDetail infoToLog)
        {
            _usageLogger.Write(LogEventLevel.Information,
                _messageTemplate,
                    infoToLog.Timestamp, infoToLog.Message,
                    infoToLog.Location,
                    infoToLog.Product, infoToLog.LumberJackException,
                    infoToLog.ElapsedMilliseconds, infoToLog.Exception?.ToBetterString(),
                    infoToLog.Hostname, infoToLog.PartId,
                    infoToLog.UserName, infoToLog.CorrelationId,
                    infoToLog.AdditionalInfo,
                _appSettings.EnvironmentName
                    );
        }


        public void Write(LumberJackDetail infoToLog)
        {
            switch (infoToLog.LogType)
            {
                case "Error":
                {
                    if (infoToLog.Exception != null)
                    {
                        var procName = FindProcName(infoToLog.Exception);
                        infoToLog.Location = string.IsNullOrEmpty(procName) ? infoToLog.Location : procName;
                        infoToLog.Message = GetMessageFromException(infoToLog.Exception);

                        infoToLog.AdditionalInfo ??= new Dictionary<string, object>();


                        if (infoToLog.Exception.Data.Count > 0)
                        {
                            foreach (DictionaryEntry dictionaryEntry in infoToLog.Exception.Data)
                            {
                                infoToLog.AdditionalInfo.Add(dictionaryEntry.Key.ToString() ?? string.Empty, dictionaryEntry.Value);
                            }
                        }

                    }

                    break;
                }
                case "Diagnostic" when !_appSettings.EnableDiagnostics:
                    return;
            }


            _usageLogger.Write(LogEventLevel.Information,
                _messageTemplate,
                infoToLog.Timestamp, infoToLog.Message,
                infoToLog.Location,
                infoToLog.Product, infoToLog.LumberJackException,
                infoToLog.ElapsedMilliseconds, infoToLog.Exception?.ToBetterString(),
                infoToLog.Hostname, infoToLog.PartId,
                infoToLog.UserName, infoToLog.CorrelationId,
                infoToLog.AdditionalInfo,
                _appSettings.EnvironmentName
            );
        }


        public void WriteError(LumberJackDetail infoToLog)
        {
            if (infoToLog.Exception != null)
            {
                var procName = FindProcName(infoToLog.Exception);
                infoToLog.Location = string.IsNullOrEmpty(procName) ? infoToLog.Location : procName;
                infoToLog.Message = GetMessageFromException(infoToLog.Exception);

                infoToLog.AdditionalInfo ??= new Dictionary<string, object>();


                if (infoToLog.Exception.Data.Count > 0)
                {
                    foreach (DictionaryEntry dictionaryEntry in infoToLog.Exception.Data)
                    {
                        infoToLog.AdditionalInfo.Add(dictionaryEntry.Key.ToString() ?? string.Empty, dictionaryEntry.Value);
                    }
                }

            }

            _errorLogger.Write(LogEventLevel.Error,
                _messageTemplate,
                    infoToLog.Timestamp, infoToLog.Message,
                    infoToLog.Location,
                    infoToLog.Product, infoToLog.LumberJackException,
                    infoToLog.ElapsedMilliseconds, infoToLog.Exception?.ToBetterString(),
                    infoToLog.Hostname, infoToLog.PartId,
                    infoToLog.UserName, infoToLog.CorrelationId,
                    infoToLog.AdditionalInfo,
                _appSettings.EnvironmentName
                    );
        }
        public void WriteDiagnostic(LumberJackDetail infoToLog)
        {
            var writeDiagnostics = _appSettings.EnableDiagnostics;
            if (!writeDiagnostics) return;


            _diagnosticLogger.Write(LogEventLevel.Information,
                _messageTemplate,
                    infoToLog.Timestamp, infoToLog.Message,
                    infoToLog.Location,
                    infoToLog.Product, infoToLog.LumberJackException,
                    infoToLog.ElapsedMilliseconds, infoToLog.Exception?.ToBetterString(),
                    infoToLog.Hostname, infoToLog.PartId,
                    infoToLog.UserName, infoToLog.CorrelationId,
                    infoToLog.AdditionalInfo,
                _appSettings.EnvironmentName
                    );
        }

        public string GetMessageFromException(Exception ex)
        {
            return ex.InnerException != null ? GetMessageFromException(ex.InnerException) : ex.Message;
        }

        private string FindProcName(Exception ex)
        {

            if (!string.IsNullOrEmpty((string)ex.Data["Procedure"]))
            {
                return (string)ex.Data["Procedure"];
            }

            return ex.InnerException != null ? FindProcName(ex.InnerException) : null;
        }


        public void LogUsage(string message, Dictionary<string, object> additionalInfo = null)
        {
            var webInfo = GetBitsLoggingData(out var userId, out var userName, out var location);

            if (additionalInfo != null)
            {
                foreach (var key in additionalInfo.Keys)
                    webInfo.Add($"Info-{key}", additionalInfo[key]);
            }

            var usageInfo = new LumberJackDetail
            {
                Product = Assembly.GetExecutingAssembly().GetName().Name,
                LogType = "Usage",
                Location = location,
                Timestamp = DateTime.Now,
                PartId = userId,
                UserName = userName,
                Hostname = Environment.MachineName,
                CorrelationId = Activity.Current.Id,
                Message = message,
                AdditionalInfo = webInfo
            };

            WriteUsage(usageInfo);
        }
        
        public void LogPerformance(long elapsedTime,Dictionary<string, object> performanceInfo = null)
        {
            var webInfo = GetBitsLoggingData(out var userId, out var userName, out var location);
            if (performanceInfo != null)
            {
                foreach (var key in performanceInfo.Keys)
                    webInfo.Add(key, performanceInfo[key]);
            }

            var perfInfo = new LumberJackDetail
            {
                Product = Assembly.GetExecutingAssembly().GetName().Name,
                LogType = "Performance",
                Location = location,
                Timestamp = DateTime.Now,
                PartId = userId,
                UserName = userName,
                Hostname = Environment.MachineName,
                AdditionalInfo = webInfo,
                ElapsedMilliseconds = elapsedTime
            };

            WritePerf(perfInfo);
        }


        public void LogDiagnostic(string message, Dictionary<string, object> diagnosticInfo = null)
        {

            if (!_appSettings.EnableDiagnostics)  
                return;

            var webInfo = GetBitsLoggingData(out var userId, out var userName, out var location);
            if (diagnosticInfo != null)
            {
                foreach (var key in diagnosticInfo.Keys)
                    webInfo.Add(key, diagnosticInfo[key]);
            }

            var diagInfo = new LumberJackDetail
            {
                Product = Assembly.GetExecutingAssembly().GetName().Name,
                LogType = "Diagnostic",
                Location = location,
                Timestamp = DateTime.Now,
                PartId = userId,
                UserName = userName,
                Hostname = Environment.MachineName,
                CorrelationId = Activity.Current.Id,
                Message = message,
                AdditionalInfo = webInfo
            };

            WriteDiagnostic(diagInfo);
        }

        public void LogError(Exception ex,string message = null)
        {
            var webInfo = GetBitsLoggingData(out var userId, out var userName, out var location);

            var errorInformation = new LumberJackDetail
            {
                Product = Assembly.GetExecutingAssembly().GetName().Name,
                LogType = "Error",
                Location = location,
                Timestamp = DateTime.Now,
                PartId = userId,
                UserName = userName,
                Hostname = Environment.MachineName,
                CorrelationId = Activity.Current.Id,
                Message = message ?? "An error occurred during processing.",
                Exception = ex,
                AdditionalInfo = webInfo
            };

            WriteError(errorInformation);
        }

        public Dictionary<string, object> GetBitsLoggingData(out int userId,
            out string userName, out string location)
        {
            var data = new Dictionary<string, object>();
            GetUserData(data, out userId, out userName);
            GetLocationForApiCall(data, out  location);

            return data;
        }

        public void GetUserData(Dictionary<string, object> data,
            out int userId,
            out string userName)
        {
            var user = _context.HttpContext.User.Identities.FirstOrDefault();
            int.TryParse(user.Claims.FirstOrDefault(c => c.Type == "bits_participant_partid")?.Value, out  userId);

            var surname = user.Claims.FirstOrDefault(c => c.Type.ToLower().Contains("surname"))?.Value.ToUpper();
            var givenName = user.Claims.FirstOrDefault(c => c.Type.ToLower().Contains("givenname"))?.Value.ToUpper();

            userName = $"{givenName}.{surname}";
            if (!_appSettings.AddClaims) return;
            if (user.Claims == null) return;
            var i = 1; // i included in dictionary key to ensure uniqueness
            foreach (var claim in user.Claims)
            {
                // example dictionary key: UserClaim-4-role 
                data.Add($"UserClaim-{i++}-{claim.Type}",
                    claim.Value);
            }
        }

        public void GetLocationForApiCall(
            Dictionary<string, object> dict, out string location)
        {
            Endpoint endpointBeingHit = _context.HttpContext.Features.Get<IEndpointFeature>()?.Endpoint;
            ControllerActionDescriptor actionDescriptor = endpointBeingHit?.Metadata?.GetMetadata<ControllerActionDescriptor>();
            var routeTemplate = actionDescriptor?.AttributeRouteInfo.Template;

            var request = _context.HttpContext.Request;

            var method = request.Method;
            dict.Add("UserAgent", request.Headers["User-Agent"]);
            foreach (var key in request.RouteValues.Keys)
            {
                var value = request.RouteValues[key].ToString();
                dict.Add($"Route-{key}", value);

            }

            location = $"{method} {routeTemplate}";

            var qs = HttpUtility.ParseQueryString(request.QueryString.ToUriComponent());
            var i = 0;
            foreach (string key in qs.Keys)
            {
                var newKey = $"queryString-{i++}-{key}";
                if (!dict.ContainsKey(newKey))
                    dict.Add(newKey, qs[key]);
            }

            var referrer = request.GetTypedHeaders().Referer;
            if (referrer == null) return;
            var source = referrer.OriginalString;
            if (source.ToLower().Contains("swagger"))
                source = "Swagger";
            if (!dict.ContainsKey("Referrer"))
                dict.Add("Referrer", source);
        }
        public void GetSessionData(Dictionary<string, object> data)
        {
            if (_context.HttpContext.Session == null) return;
            foreach (var key in _context.HttpContext.Session.Keys)
            {
                var keyName = key.ToString();
                if (_context.HttpContext.Session.Keys.FirstOrDefault(k => k == keyName) != null)
                {
                    data.Add($"Session-{keyName}",
                        _context.HttpContext.Session.Keys.FirstOrDefault(k => k == keyName)?.ToString());
                }
            }
            data.Add("SessionId", _context.HttpContext.Session.Id);
        }
    }
}
