﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Ocelot.Errors;
using Ocelot.Responses;
using Ocelot.Logging;

namespace Ocelot.Requester
{
    public class HttpClientHttpRequester : IHttpRequester
    {
        private readonly IOcelotLogger _logger;
 
        public HttpClientHttpRequester(IOcelotLoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<HttpClientHttpRequester>();
        }

        public async Task<Response<HttpResponseMessage>> GetResponse(Request.Request request)
        {
            HttpClientBuilder builder = new HttpClientBuilder();    

            using (var handler = new HttpClientHandler { CookieContainer = request.CookieContainer })
            {
                if (request.IsQos)
                {
                    builder.WithCircuitBreaker(request.Qos, _logger, handler);
                }           

                using (var httpClient = builder.Build(handler))
                {
                    try
                    {
                        var response = await httpClient.SendAsync(request.HttpRequestMessage);
                        return new OkResponse<HttpResponseMessage>(response);
                    }
                    catch (Polly.Timeout.TimeoutRejectedException exception)
                    {
                        return
                           new ErrorResponse<HttpResponseMessage>(new RequestTimedOutError(exception));
                    }
                    catch (Exception exception)
                    {
                        return new ErrorResponse<HttpResponseMessage>(new UnableToCompleteRequestError(exception));
                    }
                }
            }
        }
    }
}