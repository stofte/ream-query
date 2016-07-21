namespace ReamQuery.Test
{
    using System;
    using System.Net.Http;
    using ReamQuery.Api;
    using ReamQuery.Core.Api;
    using Newtonsoft.Json;
    using Xunit;
    using System.Linq;
    using System.Collections.Generic;

    public class ExecuteCodeEndpoint : E2EBase
    {
        protected override string EndpointAddress { get { return  "/executecode"; } }
        
        [Fact]
        public async void Handles_Endless_Queries()
        {
            var id = Guid.NewGuid();
            var code = @"

    var x = 42;
    while (true) {
        System.Threading.Thread.Sleep(1000);
        x.Dump();
    }
";
            var request = new CodeRequest { Text = code, Id = id };
            var json = JsonConvert.SerializeObject(request);
            var res = await _client
                .PostAsync(EndpointAddress, new StringContent(json))
                ;

            // very sensitive?
            var timeout = IsContinousIntegration() ? 30 : 5;
            var msgs = GetMessages(timeout);
            var jsonRes = await res.Content.ReadAsStringAsync();
            var output = JsonConvert.DeserializeObject<CodeResponse>(jsonRes);
            Assert.Equal(StatusCode.Ok, output.Code);

            var expected = JsonConvert.SerializeObject(new Message
            {
                Session = id,
                Type = ItemType.SingleAtomic,
                Values = new object[]
                {
                    new Column { Name = "int", Type = "System.Int32" },
                    42
                }
            });
            Console.WriteLine("Handles_Endless_Queries msgs.Count: {0}", msgs.Count());
            Assert.True(1 < msgs.Count(), "More then 1 msg expected");
            Assert.True(msgs.Count() < 20, "Less than 20 msgs expected");
            Assert.Contains(expected, msgs);
        }

        [Theory, MemberData("Execute_Code_Samples")]
        public async void Returns_Expected_Data_For_Code_Sample(Guid id, string code, string[] expectedMsgs)
        {
            var request = new CodeRequest { Text = code, Id = id };
            var json = JsonConvert.SerializeObject(request);
            var res = await _client
                .PostAsync(EndpointAddress, new StringContent(json))
                ;
                
            var msgs = GetMessages();
            var jsonRes = await res.Content.ReadAsStringAsync();
            var output = JsonConvert.DeserializeObject<CodeResponse>(jsonRes);
            Assert.Equal(StatusCode.Ok, output.Code);
            foreach(var expected in expectedMsgs)
            {
                Assert.Single(msgs, expected);
            }
        }

        protected static IEnumerable<object> Execute_Code_Samples()
        {
            var id1 = Guid.NewGuid();
            return new object[][]
            {
                new object[]
                {
                    id1,
                    @"
                    var x = 10;
                    x + 1
                    ",
                    new string[]
                    {
                        JsonConvert.SerializeObject(new Message
                        {
                            Session = id1,
                            Type = ItemType.SingleAtomic,
                            Values = new object[]
                            {
                                new Column { Name = "int", Type = "System.Int32" },
                                11
                            }
                        })
                    }    
                }
            };
        }
    }
}
