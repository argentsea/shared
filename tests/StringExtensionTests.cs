using Xunit;
using System.Collections.Generic;
using ArgentSea;
using FluentAssertions;
using System.Text;

namespace ArgentSea.Test
{
	public class StringExtensionTests
	{

		private static string Emoji
		{
			get
			{
				//return char.ConvertFromUtf32(System.Convert.ToInt32(0xF09f9883));
				return char.ConvertFromUtf32(System.Convert.ToInt32(0x1F603));
				//return System.Text.Encoding.Unicode.GetString(new byte[] { 0xF0, 0x9F, 0x98, 0x83 });
			}
		}
		[Fact]
		public void TestNormalStringCleaning()
		{
			var test1 = @"This is a test";
			test1.CleanInput().Should().Be("This is a test");
		}
		[Fact]
		public void TestWhitespaceStringCleaning()
		{
			var test1 = @"  This is a test   
";
			test1.CleanInput().Should().Be("This is a test");
		}
		[Fact]
		public void TestNornalEmojiStringCleaning()
		{
			var test1 = @"  This is a test" + StringExtensionTests.Emoji;
			test1.CleanInput().Should().Be("This is a test");
		}
		[Fact]
		public void TestTwoLineEmojiStringCleaning()
		{
			var test1 = "  " + StringExtensionTests.Emoji +  @"This is a test   
Another test  .";
			test1.CleanInput().Should().Be("This is a test   Another test  .");
		}
		[Fact]
		public void TestEmojiTwoLineEndStringCleaning()
		{
			var test1 = @"  This is a test   
Another test "  + StringExtensionTests.Emoji;
			test1.CleanInput().Should().Be("This is a test   Another test");
		}
		[Fact]
		public void TestEmojiLineStringCleaning()
		{
			var test1 = "  This is a test" + StringExtensionTests.Emoji + "Another test";
			test1.CleanInput().Should().Be("This is a testAnother test");
		}


        [Theory]
        [InlineData("t")]
		[InlineData("te")]
		[InlineData("tes")]
		[InlineData("test")]
		[InlineData("testin")]
        [InlineData("testing")]
        [InlineData("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!!©°´±")]
		public void TestEncodeDecode(string validateThis)
		{
            var testBytes = Encoding.UTF8.GetBytes(validateThis);
			var encoded = StringExtensions.EncodeToString(ref testBytes);
			var decoded = StringExtensions.Decode(encoded);
			Encoding.UTF8.GetString(decoded).Should().Be(validateThis);
            var encoded2 = StringExtensions.EncodeToUtf8(ref testBytes);
            var decoded2 = StringExtensions.Decode(encoded2);
			Encoding.UTF8.GetString(decoded2).Should().Be(validateThis);
        }
    }
}