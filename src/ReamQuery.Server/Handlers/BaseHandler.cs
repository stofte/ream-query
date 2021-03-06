namespace ReamQuery.Server.Handlers
{
    using System;
    using System.IO;
    using System.Text;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Newtonsoft.Json;
    using ReamQuery.Server.Converters;
    using ReamQuery.Server.Api;
    using NLog;

    public abstract class BaseHandler<TResult, TInput> where TResult : ResponseBase
    {
        private static Logger Logger = LogManager.GetCurrentClassLogger();

        private static readonly Encoding _encoding = new UTF8Encoding(false);

        protected RequestDelegate _next;

        public BaseHandler(RequestDelegate next) 
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            var sw = new Stopwatch();
            sw.Start();
            if (context.Request.Path.HasValue && Handle(context.Request.Path.Value))
            {
                try { 
                    Logger.Debug("{0}", context.Request.Path.Value);
                    TInput input = default(TInput);
                    if (context.Request.Method == "POST") 
                    {
                        input = ReadIn(context.Request);
                    }
                    var res = await Execute(input);
                    var duration = Math.Ceiling(sw.Elapsed.TotalMilliseconds);
                    context.Response.Headers.Add("X-Duration-Milliseconds", duration.ToString());
                    context.Response.Headers.Add("Content-Type", "application/json; charset=utf-8");
                    Logger.Info("{0} took {1} ms", context.Request.Path.Value, duration);
                    WriteTo(context.Response, res);
                    return;
                } catch (Exception exn) {
                    Logger.Error("Handler failed {1} => {0}", exn.Message, exn.InnerException);
                    Logger.Error("Stack {0}", exn.StackTrace);
                    return;
                }
            }
            await _next(context);
        }

        TInput ReadIn(HttpRequest request)
        {
            using (StreamReader reader = new StreamReader(request.Body))
            using (JsonTextReader jsonReader = new JsonTextReader(reader))
            {
                JsonSerializer ser = new JsonSerializer();
                return ser.Deserialize<TInput>(jsonReader);
            }
        }

        void WriteTo(HttpResponse response, object value)
        {
            using (var writer = new StreamWriter(response.Body, _encoding, 1024, true))
            using (var jsonWriter = new JsonTextWriter(writer))
            {
                jsonWriter.CloseOutput = false;
                var settings = new JsonSerializerSettings();
                settings.Converters.Add(new NumberConverter());
                settings.Converters.Add(new TupleConverter());
                var jsonSerializer = JsonSerializer.Create(settings);
                jsonSerializer.Serialize(jsonWriter, value);
            }
        }

        protected abstract bool Handle(string path);

        protected abstract Task<TResult> Execute(TInput input);
    }
}
