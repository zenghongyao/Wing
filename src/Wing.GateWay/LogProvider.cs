﻿using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Wing.Converter;
using Wing.EventBus;
using Wing.Gateway.Config;
using Wing.Persistence.Gateway;
using Wing.ServiceProvider;

namespace Wing.Gateway
{
    public class LogProvider : ILogProvider
    {
        private readonly IJson _json;
        private readonly ILogger<LogProvider> _logger;
        private readonly IConfiguration _configuration;

        public LogProvider(IJson json,
            ILogger<LogProvider> logger,
            IConfiguration configuration)
        {
            _json = json;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task Add(ServiceContext serviceContext)
        {
            var config = _configuration.GetSection("Gateway:Log").Get<LogConfig>();
            if (!config.IsEnabled)
            {
                return;
            }

            var httpContext = serviceContext.HttpContext;
            var request = httpContext.Request;
            Log log = null;
            try
            {
                var now = DateTime.Now;
                log = new Log
                {
                    Id = Guid.NewGuid().ToString(),
                    ClientIp = Tools.RemoteIp,
                    DownstreamUrl = serviceContext.DownstreamPath,
                    Policy = serviceContext.Policy == null ? string.Empty : _json.Serialize(serviceContext.Policy),
                    RequestTime = serviceContext.RequestTime,
                    RequestMethod = serviceContext.IsWebSocket ? "WebSocket" : request.Method,
                    RequestUrl = request.GetDisplayUrl(),
                    ResponseTime = now,
                    ServiceName = serviceContext.ServiceName,
                    StatusCode = serviceContext.StatusCode,
                    ResponseValue = serviceContext.ResponseValue,
                    GateWayServerIp = App.CurrentServiceUrl,
                    ServiceAddress = serviceContext.ServiceAddress,
                    UsedMillSeconds = Convert.ToInt64((now - serviceContext.RequestTime).TotalMilliseconds),
                    Exception = serviceContext.Exception
                };

                if (request.Headers != null && request.Headers.ContainsKey("AuthKey"))
                {
                    log.AuthKey = request.Headers["AuthKey"].ToString();
                }

                if (App.GetService<IAuthenticationService>() != null)
                {
                    try
                    {
                        log.Token = await httpContext.GetTokenAsync(JwtBearerDefaults.AuthenticationScheme, OpenIdConnectParameterNames.AccessToken);
                    }
                    catch
                    {
                    }
                }

                if (!string.IsNullOrEmpty(serviceContext.RequestValue))
                {
                    log.RequestValue = serviceContext.RequestValue;
                }
                else if (request.Body != null)
                {
                    using (var reader = new StreamReader(request.Body))
                    {
                        log.RequestValue = await reader.ReadToEndAsync();
                    }
                }

                if (config.UseEventBus)
                {
                    App.GetRequiredService<IEventBus>().Publish(log);
                }
                else
                {
                    var result = await App.GetRequiredService<ILogService>().Add(log);
                    if (result <= 0)
                    {
                        _logger.LogInformation($"数据库保存失败，请求日志：{_json.Serialize(log)}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发生异常,请求日志：{0}", log != null ? _json.Serialize(log) : string.Empty);
            }
        }
    }
}
