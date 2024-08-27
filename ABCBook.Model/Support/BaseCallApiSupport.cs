using Microsoft.AspNetCore.Http;
using System.Net;
using System.Text;
using System.Text.Json;

namespace ABCBook.Model.Support
{
    public abstract class BaseCallApiSupport
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly JsonSerializerOptions JsonSerializerOptions;
        private readonly MvcSettingsParse _mvcSettingsParse;
        private readonly string _area;

        public BaseCallApiSupport(IHttpClientFactory httpClientFactory,
            IHttpContextAccessor httpContextAccessor,
            MvcSettingsParse mvcSettingsParse,
            string area,
            JsonSerializerOptions jsonSerializerOptions)
        {
            _httpClientFactory = httpClientFactory;
            _httpContextAccessor = httpContextAccessor;
            _mvcSettingsParse = mvcSettingsParse;
            _area = area;
            JsonSerializerOptions = jsonSerializerOptions;
        }


        /// <summary>
        /// Gọi API lên server BE
        /// </summary>
        /// <typeparam name="TDataResult">Kiểu dữ liệu của Data mà BE trả về</typeparam>
        /// <param name="PathAPI">Đường dẫn gọi API không bao gồm domain và ký tự / cuối cùng</param>
        /// <param name="BodyContent">Dữ liệu truyền đi</param>
        /// <param name="method">Phương thức truyền</param>
        /// <returns></returns>
        /// <exception cref="CallApiManagerException"></exception>
        public async Task<ApiResultFE<TDataResult?>> API<TDataResult>(string PathAPI, object? BodyContent, HttpMethod method, Func<byte[], APIResult<TDataResult>>? convert = null)
        {
            HttpClient client = _httpClientFactory.CreateClient("ABCBook");

            HttpRequestMessage request = new(method, PathAPI);
            //string? userAgentRequest = _httpContextAccessor.HttpContext.Request.Headers
            //    .Where(x => x.Key.ToLower() == "user-agent")
            //    .FirstOrDefault().Value;
            ////Truyền chuỗi User-Agent của Client
            //if (!string.IsNullOrWhiteSpace(userAgentRequest))
            //{
            //    string userAgent = userAgentRequest;
            //    request.Headers.Add("User-Agent", userAgent);
            //}
            ////Truyền chuỗi Authentication của Manager nếu có
            //if (_httpContextAccessor.HttpContext.Request.Cookies.Any(x => x.Key == _area))
            //{
            //    string Authentication = _httpContextAccessor.HttpContext.Request.Cookies
            //        .Where(x => x.Key == _area)
            //        .FirstOrDefault()
            //        .Value.ToString();
            //    request.Headers.Add("Authorization", Authentication);
            //}
            ////Truyền IP client cho API server
            //request.Headers.Add(_mvcSettingsParse.MvcSettings.AppSettings.APIHeaderIP, _httpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString());


            if (BodyContent != null)
            {
                string body = JsonSerializer.Serialize(BodyContent, JsonSerializerOptions);
                StringContent content = new(body, Encoding.UTF8, "application/json");
                request.Content = content;
            }

            ApiResultFE<TDataResult> apiResultFE = new();
            apiResultFE.httpResponseMessage = await client.SendAsync(request);

            switch ((int)apiResultFE.httpResponseMessage.StatusCode)
            {
                case (int)HttpStatusCode.OK:
                    {
                        break;
                    }
                case (int)HttpStatusCode.BadRequest:
                    {
                        break;
                    }
                case 401:
                    {
                        throw new Exception("401");
                    }
                default:
                    {
                        throw new Exception("500");
                    }
            }

            byte[] bodyResponse = await apiResultFE.httpResponseMessage.Content.ReadAsByteArrayAsync();
            if (convert == null)
            {

                apiResultFE.apiResult = JsonSerializer.Deserialize<APIResult<TDataResult?>>(Encoding.UTF8.GetString(bodyResponse), JsonSerializerOptions);

                if (apiResultFE.apiResult == null)
                {
                    apiResultFE.apiResult = new();
                }
            }
            else
            {
                apiResultFE.apiResult = convert(bodyResponse);
            }


            return apiResultFE;
        }


    }
}
