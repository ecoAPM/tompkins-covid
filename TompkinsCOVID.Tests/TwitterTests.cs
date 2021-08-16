using System.Collections.Generic;
using System.Threading.Tasks;
using AngleSharp.Dom;
using NSubstitute;
using Tweetinvi;
using Tweetinvi.Models;
using Tweetinvi.Parameters;
using Xunit;

namespace TompkinsCOVID.Tests
{
    public class TwitterTests
    {
        [Fact]
        public async Task CanGetMostRecentTweetDate()
        {
            //arrange
            var tweet = Substitute.For<ITweet>();
            tweet.Text.Returns("7/1/2021\ntest data");
            var client = Substitute.For<ITwitterClient>();
            client.Timelines.GetUserTimelineAsync(Arg.Any<IGetUserTimelineParameters>()).Returns(new[] { tweet });
            var twitter = new Twitter(client);

            //act
            var date = await twitter.GetLatestPostedDate();

            //assert
            Assert.Equal("7/1/2021", date?.ToShortDateString());
        }

        [Fact]
        public async Task TweetSendsInfoToClient()
        {
            //arrange
            var client = Substitute.For<ITwitterClient>();
            var twitter = new Twitter(client);
            var cells = Substitute.For<IList<IElement>>();
            var data = Substitute.For<IElement>();
            data.TextContent.Returns("7/1/2021");
            cells[0].Returns(data);
            var record = new Record(cells);

            //act
            await twitter.Tweet(record);
            
            //assert
            await client.Received().Tweets.PublishTweetAsync(Arg.Any<string>());
        }
    }
}