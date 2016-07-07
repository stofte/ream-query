namespace ReamQuery.Test
{
    using Xunit;
    using System.Linq;
    using ReamQuery.Models;
    using ReamQuery.Api;
    using System.Net.Http;
    using Newtonsoft.Json;
    using Microsoft.CodeAnalysis;

    public class ExecuteQueryEndpoint : E2EBase
    {
        protected override string EndpointAddress { get { return  "/executequery"; } }

        [Theory, MemberData("WorldDatabase")]
        public async void Returns_Expected_Data_For_Database(string connectionString, DatabaseProviderType dbType)
        {
            var request = new QueryRequest 
            {
                ServerType = dbType,
                ConnectionString = connectionString,
                Namespace = "ns",
                Text = "City.Take(10)"
            };
            var json = JsonConvert.SerializeObject(request);
            var res = await _client
                .PostAsync(EndpointAddress, new StringContent(json))
                ;
            
            var jsonRes = await res.Content.ReadAsStringAsync();
            var output = JsonConvert.DeserializeObject<QueryResponse>(jsonRes);
            Assert.Equal(10, output.Results.Single().Values.Count());
        }

        [Theory, MemberData("WorldDatabase")]
        public async void Handles_Malformed_Source_Code_In_Request(string connectionString, DatabaseProviderType dbType)
        {
            var queryStr = @"

    NOT_DEFINED";
            var request = new QueryRequest 
            {
                ServerType = dbType,
                ConnectionString = connectionString,
                Namespace = "ns",
                Text = queryStr
            };
            var json = JsonConvert.SerializeObject(request);
            
            var res = await _client
                .PostAsync(EndpointAddress, new StringContent(json))
                ;
            
            var jsonRes = await res.Content.ReadAsStringAsync();
            var output = JsonConvert.DeserializeObject<QueryResponse>(jsonRes);

            Assert.Equal(StatusCode.CompilationError, output.Code);
            Assert.Equal(2, output.Diagnostics.Where(x => x.Severity == DiagnosticSeverity.Error).First().Line);
            Assert.Equal(4, output.Diagnostics.Where(x => x.Severity == DiagnosticSeverity.Error).First().Column);
            Assert.Null(output.Results);
        }

        [Theory, MemberData("WorldDatabase")]
        public async void Executes_Linq_Style_Statements(string connectionString, DatabaseProviderType dbType)
        {
            var request = new QueryRequest 
            {
                ServerType = dbType,
                ConnectionString = connectionString,
                Namespace = "ns",
                Text = @"
from c in City 
where c.Name.StartsWith(""Ca"") 
select c
"
            };
            var json = JsonConvert.SerializeObject(request);
            var res = await _client
                .PostAsync(EndpointAddress, new StringContent(json))
                ;
            
            var jsonRes = await res.Content.ReadAsStringAsync();
            var output = JsonConvert.DeserializeObject<QueryResponse>(jsonRes);
            Assert.All(output.Results.Single().Values, (val) => val.ToString().StartsWith("Ca"));
        }

        [Theory, MemberData("InvalidConnectionStrings")]
        public async void Returns_Expected_StatusCode_For_Invalid_ConnectionString(string connectionString, DatabaseProviderType dbType, Api.StatusCode expectedCode)
        {
            var request = new QueryRequest 
            {
                ServerType = dbType,
                ConnectionString = connectionString,
                Namespace = "ns",
                Text = ""
            };
            var json = JsonConvert.SerializeObject(request);
            var res = await _client
                .PostAsync(EndpointAddress, new StringContent(json))
                ;
            
            var jsonRes = await res.Content.ReadAsStringAsync();
            var output = JsonConvert.DeserializeObject<QueryResponse>(jsonRes);

            Assert.Equal(expectedCode, output.Code);
        }
    }
}
