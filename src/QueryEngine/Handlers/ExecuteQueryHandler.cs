namespace QueryEngine.Handlers
{
    using System;
    using Microsoft.AspNetCore.Http;
    using QueryEngine.Services;
    using QueryEngine.Models;
    using System.Threading.Tasks;

    public class ExecuteQueryHandler : BaseHandler<QueryResult, QueryInput>
    {
        QueryService _service;

        public ExecuteQueryHandler(RequestDelegate next, QueryService service) : base(next) 
        {
            _service = service; 
        }

        protected override bool Handle(string path)
        {
            return path.Contains("/executequery");
        }

        protected override async Task<QueryResult> Execute(QueryInput input)
        {
            var res = _service.ExecuteQuery(input);
            return await Task.FromResult(new QueryResult
            {
                Id = Guid.NewGuid(),
                Created = DateTime.Now,
                Results = res
            });
        }
    }
}