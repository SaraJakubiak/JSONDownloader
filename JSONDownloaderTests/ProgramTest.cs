using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.IO.Abstractions.TestingHelpers;
using JSONDownloader;
using Moq;
using Moq.Protected;
using Xunit;


namespace JSONDownloaderTests
{

    public class ProgramTest
    {

        Mock<HttpMessageHandler> mockHttpMessageHandler = new Mock<HttpMessageHandler>();


        [Fact]
        public void TestGenerateJSONInfo() {

            string strUrls = "http://xyz.com/a.json;https://xyz.com/b.json;c d;;";

            var jsonInfo = Program.GenerateJSONInfo(strUrls, "");

            Assert.Equal(2, jsonInfo.Count);
            Assert.True(jsonInfo.ContainsKey("a.json"));
            Assert.True(jsonInfo.ContainsKey("b.json"));
        }

        [Fact]
        public async Task TestDownloadAsyncGoodResponse()
        {

            string content = "test string";
            mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(content)
                })
                .Verifiable();

            HttpClient client = new HttpClient(mockHttpMessageHandler.Object);
            MockFileSystem mockFileSystem = new MockFileSystem();
            
            string url = "http://xyz.com/a.json";
            string path = "a.json";

            await Program.DownloadAsync(url, path, client, mockFileSystem);

            var jsonFile = mockFileSystem.GetFile(path);
            Assert.NotNull(jsonFile);
            Assert.Equal(content, jsonFile.TextContents);
        }


        [Fact]
        public async Task TestDownloadAsyncBadResponseNoDownload()
        {

            mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.NotFound,
                })
                .Verifiable();

            HttpClient client = new HttpClient(mockHttpMessageHandler.Object);
            MockFileSystem mockFileSystem = new MockFileSystem();
            
            string url = "http://xyz.com/a.json";
            string path = "a.json";

            await Program.DownloadAsync(url, path, client, mockFileSystem);

            var jsonFile = mockFileSystem.GetFile(path);
            Assert.Null(jsonFile);
        }


        [Theory]
        [InlineData("http://xyz.com/")]
        [InlineData("https://xyz.com/")]
        [InlineData("https://xyz.com/ab")]
        [InlineData("https://xyz.com/ab/")]
        [InlineData("https://xyz.com/ab/c.json")]
        public void TestIsValidURLValid(string url)
        {
            Assert.True(Program.IsValidURL(url));
        }


        [Theory]
        [InlineData("xyz.com")]
        [InlineData("file://xyz/ab/c.json")]
        [InlineData("xyz")]
        [InlineData("xyz ab.com")]
        [InlineData("xyz.com/ab//b")]
        [InlineData("")]
        public void TestIsValidURLInvalid(string url)
        {
            Assert.False(Program.IsValidURL(url));
        }


        [Fact]
        public void TestGetNameURLWithJSON()
        {
            string name = Program.GetName("http://xyz.com/a.json");
            Assert.Equal("a", name);
        }


        [Fact]
        public void TestGetNameURLNoJSON()
        {
            string name = Program.GetName("http://xyz.com/a/b");
            Assert.Equal("xyz.com.a.b", name);
        }


        [Fact]
        public void TestGetNameIllegalChars() {
        // TODO: Illegal chars are OS dependant but Unix is much
        //       less restrictive than Windows. In fact, the main 
        //       illegal Unix character '/' is replaced before removal
        //       of illegal characters in the method.
        //       Hence, this test should focus on Windows which I have not
        //       had a chance to test on.
        }


        [Fact]
        public void TestGetFullPath() {

            // slash direction dependent so skip Windows
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                Assert.Equal("y/x.json", Program.GetFullPath("x", "y"));
                Assert.Equal("y/x.json", Program.GetFullPath("x.", "y"));
                Assert.Equal("y/x1.json", Program.GetFullPath("x", "y", "1"));
                Assert.Equal("y/x11.json", Program.GetFullPath("x.", "y", "11"));
            }
        }

    }
}
