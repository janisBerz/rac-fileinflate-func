using System;
using AzUnzipEverything;
using Xunit;

namespace FileInflate.Tests
{
    public class HelperTests
    {
        [Fact]
        public void SetLocalPathToTempFolder()
        {
            
            var result = Helper.SetLocalPath("SomeFileName.zip");
            Assert.NotNull(result);

        }
    }
}
